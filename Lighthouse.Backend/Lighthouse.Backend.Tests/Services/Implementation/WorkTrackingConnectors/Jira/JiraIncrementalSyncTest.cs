using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The whole-query path carries Jira's <c>updated</c> onto <see cref="WorkItemBase.LastChangedRemote"/>.
    /// Without it a full cycle stores nothing for the next one to compare against, so every later cycle
    /// downloads everything and the incremental sync never engages. Cloud and Data Center are the same
    /// connector class, so both deployments are exercised.
    ///
    /// The rest of the fixture covers the two-phase fetch for Jira Cloud: the identity sweep that reports
    /// which records moved, and the download of those records and no others. The acceptance suite fakes
    /// <c>IWorkTrackingConnector</c> by policy and therefore cannot see any of this - these tests are the
    /// only evidence the Jira side actually works.
    ///
    /// The Data Center block at the foot of the fixture is the same contract over the other transport. Data
    /// Center has no page token and no <c>search/jql</c> endpoint, so its sweep is an offset walk over
    /// <c>rest/api/latest/search</c> - a different set of requests answering the same two questions.
    /// </summary>
    [TestFixture]
    public class JiraIncrementalSyncTest
    {
        private const string Cloud = "Cloud";
        private const string DataCenter = "Server";

        private static readonly string[] BothPages = ["PROJ-1", "PROJ-2"];
        private static readonly string[] TheOnlyRecord = ["PROJ-1"];
        private static readonly string[] FourRecordsOverTwoPages = ["PROJ-1", "PROJ-2", "PROJ-3", "PROJ-4"];

        private const string StampWithNoZone = "2026-08-05T14:30:00.000";
        private static readonly DateTime TheInstantThatStampNames = new(2026, 8, 5, 14, 30, 0, DateTimeKind.Utc);

        private const string StampTwoHoursAheadOfUtc = "2026-08-05T14:30:00.000+0200";
        private static readonly DateTime TheInstantThatOffsetStampNames = new(2026, 8, 5, 12, 30, 0, DateTimeKind.Utc);

        private const string CloudSearchPath = "rest/api/3/search/jql";
        private const string DataCenterSearchPath = "/rest/api/latest/search";
        private const string SweepFieldList = "key,updated";
        private const string EveryField = "*all";
        private const string TheSecondPage = "page-2";

        private const string OrderingKeyword = "ORDER BY";
        private const string TheDeterministicOrdering = OrderingKeyword + " key ASC";
        private const string TheKeyedQuery = "key = \"PROJ-1\" OR key = \"PROJ-3\"";

        private const string AQueryTheOperatorOrdered = "project = PROJ ORDER BY created DESC";
        private const string TheSameQueryWithoutItsOrdering = "(project = PROJ) AND";

        private const string AnOrderingAndNothingElse = "ORDER BY rank ASC";
        private const string TheTypeFilterOnItsOwn = "(issuetype = \"Story\")";

        private static readonly string[] TheDataCenterSearchPathOnly = [DataCenterSearchPath];
        private static readonly string[] TheTwoKeysAskedFor = ["PROJ-1", "PROJ-3"];

        [TestCase(Cloud)]
        [TestCase(DataCenter)]
        public async Task GetWorkItemsForTeam_RemembersWhenTheIssueLastChangedRemotely(string deploymentType)
        {
            var workItem = await TheSingleWorkItemFetchedFrom(deploymentType, updated: StampTwoHoursAheadOfUtc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.LastChangedRemote, Is.EqualTo(TheInstantThatOffsetStampNames));
                Assert.That(workItem.LastChangedRemote?.Kind, Is.EqualTo(DateTimeKind.Utc),
                    "An instant has no time zone - storing it in anything but UTC makes the next cycle's comparison wrong.");
            }
        }

        [TestCase(Cloud)]
        [TestCase(DataCenter)]
        public async Task GetWorkItemsForTeam_LeavesTheStampEmptyWhenTheTrackerDoesNotReportOne(string deploymentType)
        {
            var workItem = await TheSingleWorkItemFetchedFrom(deploymentType, updated: null);

            Assert.That(workItem.LastChangedRemote, Is.Null,
                "No stamp means 'never swept', so the next update downloads everything. "
                + "A sentinel date would claim knowledge the tracker never gave.");
        }

        [TestCase(Cloud)]
        [TestCase(DataCenter)]
        public async Task GetWorkItemsForTeam_LeavesTheStampEmptyWhenTheTrackerReportsSomethingUnreadable(string deploymentType)
        {
            var workItem = await TheSingleWorkItemFetchedFrom(deploymentType, updated: "not a date");

            Assert.That(workItem.LastChangedRemote, Is.Null,
                "An unreadable stamp is no knowledge at all - downloading everything is the safe answer.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_ReadsAStampWithNoZoneAsUtcRatherThanAsTheHostsLocalTime()
        {
            var workItem = await TheSingleWorkItemFetchedFrom(Cloud, updated: StampWithNoZone);

            Assert.That(workItem.LastChangedRemote, Is.EqualTo(TheInstantThatStampNames),
                "A stamp the tracker gave no zone for is an instant, and which instant it is may not depend on where "
                + "Lighthouse happens to run. Reading it as host-local time makes the same payload mean a different "
                + "instant on every deployment, and the per-record comparison then reports every record as moved.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_LeavesOutAnOrderingTheTeamsOwnQueryCarries()
        {
            var jira = new JiraStub(Cloud);
            var subject = CreateSubject(jira.Handler);
            var team = CreateTeam();
            team.DataRetrievalValue = AQueryTheOperatorOrdered;

            await subject.GetWorkItemsForTeam(team);

            var jql = QueryValue(jira.SearchRequests.Last(), "jql");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jql, Does.Not.Contain(OrderingKeyword),
                    "The team's own query is wrapped in brackets and the type, state and cutoff filters are hung off "
                    + "it, so an ordering the operator wrote lands in the middle of an expression. Jira rejects that "
                    + $"as invalid JQL and the team then fetches nothing at all. Asked for: {jql}");
                Assert.That(jql, Does.StartWith(TheSameQueryWithoutItsOrdering),
                    "What is left has to still be the operator's filter, with its brackets closed - cutting the "
                    + $"ordering off the finished string would leave '(project = PROJ' open. Asked for: {jql}");
            }
        }

        [Test]
        public async Task GetFeaturesForProject_LeavesOutAnOrderingThePortfoliosOwnQueryCarries()
        {
            var jira = new JiraStub(Cloud);
            var subject = CreateSubject(jira.Handler);
            var portfolio = CreatePortfolio(CreateTeam());
            portfolio.DataRetrievalValue = AQueryTheOperatorOrdered;

            await subject.GetFeaturesForProject(portfolio);

            var jql = QueryValue(jira.SearchRequests.Last(), "jql");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jql, Does.Not.Contain(OrderingKeyword),
                    "A portfolio's Features are fetched through the same wrapping, so an operator who orders their "
                    + $"Feature query loses the whole portfolio the same way. Asked for: {jql}");
                Assert.That(jql, Does.StartWith(TheSameQueryWithoutItsOrdering),
                    $"What is left has to still be the operator's filter, with its brackets closed. Asked for: {jql}");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_LeavesNoEmptyFilterWhenTheTeamsQueryIsNothingButAnOrdering()
        {
            var jql = await TheQueryJiraIsAskedFor(AnOrderingAndNothingElse);

            AssertJiraCanParseTheWholeFilter(jql);
        }

        [Test]
        public async Task GetFeaturesForProject_LeavesNoEmptyFilterWhenThePortfoliosQueryIsNothingButAnOrdering()
        {
            var jira = new JiraStub(Cloud);
            var subject = CreateSubject(jira.Handler);
            var portfolio = CreatePortfolio(CreateTeam());
            portfolio.DataRetrievalValue = AnOrderingAndNothingElse;

            await subject.GetFeaturesForProject(portfolio);

            AssertJiraCanParseTheWholeFilter(QueryValue(jira.SearchRequests.Last(), "jql"));
        }

        [TestCase("summary ~ \"reorder by priority\"")]
        [TestCase("description ~ \"preorder by hand\"")]
        [TestCase("text ~ \"border by\"")]
        [TestCase("summary ~ \"ORDER BY created\"")]
        [TestCase("summary ~ 'ORDER BY created'")]
        [TestCase("description ~ \"please ORDER BY priority\"")]
        [TestCase("project = PROJ AND status != Done")]
        public async Task GetWorkItemsForTeam_AsksForTheTeamsQueryWholeWhenNothingInItOrdersTheResult(string theTeamsOwnQuery)
        {
            var jql = await TheQueryJiraIsAskedFor(theTeamsOwnQuery);

            Assert.That(jql, Does.StartWith($"({theTeamsOwnQuery}) AND"),
                "Only a trailing ordering clause may come off an operator's query. The words 'order by' sitting "
                + "inside a longer word, or inside a value the operator searches for, order nothing - cutting there "
                + "leaves a truncated filter that Jira still accepts and that quietly selects the wrong work items. "
                + $"Asked for: {jql}");
        }

        [TestCase("project = PROJ order by created desc", "(project = PROJ) AND")]
        [TestCase("project = PROJ   ORDER BY   created DESC", "(project = PROJ) AND")]
        [TestCase("summary ~ \"reorder by priority\" ORDER BY created DESC", "(summary ~ \"reorder by priority\") AND")]
        [TestCase("summary ~ \"ORDER BY created\" ORDER BY created DESC", "(summary ~ \"ORDER BY created\") AND")]
        public async Task GetWorkItemsForTeam_LeavesOutTheOrderingTheTeamsQueryEndsWith(string theTeamsOwnQuery, string whatIsLeftOfIt)
        {
            var jql = await TheQueryJiraIsAskedFor(theTeamsOwnQuery);

            Assert.That(jql, Does.StartWith(whatIsLeftOfIt),
                "The ordering an operator wrote at the end of their query has to come off however they spelled it, "
                + "and what comes off is that clause and nothing before it. Asked for: " + jql);
        }

        [Test]
        public async Task GetWorkItemsForTeam_AsksForTheTeamsQueryWholeWhenAValueItSearchesForIsNeverClosed()
        {
            var jql = await TheQueryJiraIsAskedFor("summary ~ \"never closed ORDER BY created");

            Assert.That(jql, Does.StartWith("(summary ~ \"never closed ORDER BY created) AND"),
                "A quote with nothing closing it runs to the end of the query, so every word after it is still part "
                + "of the value the operator is searching for and orders nothing. Treating the quote as if it closed "
                + "where it opened puts the search for the ordering clause inside that value, where it finds those "
                + $"words and cuts the query in half. Asked for: {jql}");
        }

        [Test]
        public async Task GetWorkItemsForTeam_AsksForTheTeamsQueryWholeWhenItEndsOnTheOrderingKeywordAndNothingElse()
        {
            var jql = await TheQueryJiraIsAskedFor("project = PROJ ORDER BY");

            Assert.That(jql, Does.StartWith("(project = PROJ ORDER BY) AND"),
                "An ordering clause names what to order by, and this one names nothing, so there is nothing to take "
                + "off. The part worth holding is that looking at what follows the keyword stops at the end of the "
                + "query: here the keyword is the end, and reading one character further throws and takes the whole "
                + $"fetch for that team with it. Asked for: {jql}");
        }

        [Test]
        public void SweepWorkItemsForTeam_RefusesToSweepAnInstanceLighthouseHasNeverReached()
        {
            var jira = new JiraStub(Cloud);
            var subject = CreateSubject(jira.Handler);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(async () => await subject.SweepWorkItemsForTeam(CreateTeam()), Throws.TypeOf<NotSupportedException>(),
                    "The two deployments page their search results differently and each one's endpoint answers 404 on "
                    + "the other, so which one to walk has to be settled before the first request goes out. Guessing "
                    + "wrong returns nothing, 'nothing' reads as 'the query holds no records', and removal is 'stored "
                    + "minus swept' - every stored record for that team would go.");
                Assert.That(jira.Requests, Is.Empty,
                    "Refusing after the round trip would still have cost the round trip.");
            }
        }

        [Test]
        public void SupportsIncrementalSync_IsFalseBeforeLighthouseHasEverReachedTheInstance()
        {
            var jira = new JiraStub(Cloud);
            var subject = CreateSubject(jira.Handler);
            var team = CreateTeam();

            Assert.That(subject.SupportsIncrementalSync(team.WorkTrackingSystemConnection), Is.False,
                "The port member cannot block on a network round trip, so an undiscovered deployment answers "
                + "'no' - which downloads everything rather than guessing.");
        }

        [Test]
        public async Task SupportsIncrementalSync_IsTrueForCloudOnceTheDeploymentIsKnown()
        {
            var (subject, team, _) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);

            Assert.That(subject.SupportsIncrementalSync(team.WorkTrackingSystemConnection), Is.True);
        }

        [Test]
        public async Task SweepWorkItemsForTeam_AsksTheVeryQuestionTheWholeQueryAsks()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            var fullFetchQuery = QueryValue(jira.SearchRequests.Single(), "jql");

            await subject.SweepWorkItemsForTeam(team);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(fullFetchQuery),
                "Removal is 'stored minus swept'. A sweep that enumerates anything other than the exact "
                + "query the full fetch enumerates deletes whatever the two disagree about.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_AsksOnlyForIdentityAndTheChangeStamp()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);

            await subject.SweepWorkItemsForTeam(team);

            var sweep = jira.SearchRequests.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(QueryValue(sweep, "fields"), Is.EqualTo(SweepFieldList),
                    "Downloading *all during the sweep would cost exactly what the two-phase fetch exists to save.");
                Assert.That(sweep.Query, Does.Not.Contain("expand=changelog"),
                    "The changelog is the single most expensive part of a Jira issue and phase 1 never reads it.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_WalksEveryPage()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            jira.QueueSweepPage(SweepIssue("PROJ-1", "2026-08-01T10:00:00.000+0000"), nextPageToken: TheSecondPage);
            jira.QueueSweepPage(SweepIssue("PROJ-2", "2026-08-02T10:00:00.000+0000"), nextPageToken: null);

            var stamps = await subject.SweepWorkItemsForTeam(team);

            Assert.That(stamps.Select(stamp => stamp.ReferenceId), Is.EqualTo(BothPages),
                "A sweep that stops at page one under-reports the query, and 'stored minus swept' deletes everything it missed.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_ReadsAStampWithNoZoneAsUtcRatherThanAsTheHostsLocalTime()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            jira.QueueSweepPage(SweepIssue("PROJ-1", StampWithNoZone), nextPageToken: null);

            var stamps = await subject.SweepWorkItemsForTeam(team);

            Assert.That(stamps.Single().ChangedAt, Is.EqualTo(TheInstantThatStampNames),
                "The sweep reads the stamp exactly the way the full fetch reads it. A zone the sweep assumes and the "
                + "full fetch does not is the one disagreement the per-record comparison cannot absorb: every "
                + "record would look moved forever.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_ReportsTheChangeStampTheFullFetchWouldStore()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud, StampTwoHoursAheadOfUtc);
            jira.QueueSweepPage(SweepIssue("PROJ-1", StampTwoHoursAheadOfUtc), nextPageToken: null);

            var stamps = await subject.SweepWorkItemsForTeam(team);
            var stored = (await subject.GetWorkItemsForTeam(team)).Single();

            Assert.That(stamps.Single().ChangedAt, Is.EqualTo(stored.LastChangedRemote),
                "The swept stamp is compared against the stored one item by item. Two different parses of the "
                + "same string would make every item look moved forever.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_StillReportsARecordTheTrackerGaveNoStampFor()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            jira.QueueSweepPage(SweepIssue("PROJ-1", updated: null), nextPageToken: null);

            var stamps = await subject.SweepWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stamps.Select(stamp => stamp.ReferenceId), Is.EqualTo(TheOnlyRecord),
                    "Dropping it from the sweep would put it in 'stored minus swept' and delete a live item.");
                Assert.That(stamps.Single().ChangedAt, Is.Default,
                    "No stamp can never equal a stored stamp, so the record is re-downloaded rather than assumed unchanged.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_RefusesToReportAHalfWalkedQuery()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            jira.QueueSweepPage(SweepIssue("PROJ-1", "2026-08-01T10:00:00.000+0000"), nextPageToken: TheSecondPage);
            jira.FailTheSearchAfterTheNextOne();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(async () => await subject.SweepWorkItemsForTeam(team), Throws.TypeOf<InvalidOperationException>(),
                    "Returning the first page as if it were the whole query is the one answer removal cannot survive: "
                    + "every record on the pages that never arrived would be deleted. Throwing falls back to a full fetch. "
                    + "The rejected page is what has to be reported - a refusal to sweep this deployment at all would "
                    + "satisfy a looser assertion without a single page ever being walked.");
                Assert.That(TheTokensTheSweepAskedFor(jira), Does.Contain(TheSecondPage),
                    "The half-walk is the whole point: the token the first page handed back has to have been asked for "
                    + "before the rejection, or the refusal being asserted is some earlier refusal that never "
                    + "enumerated anything.");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeamByReferenceId_NamesOnlyTheKeysItWasAskedFor()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);

            await subject.GetWorkItemsForTeam(team, TheTwoKeysAskedFor);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(TheKeyedQuery),
                "Phase 2 downloads what moved and nothing else - re-applying the team filter would let the cutoff "
                + "date silently drop an item the sweep just reported as changed.");
        }

        [Test]
        public async Task GetWorkItemsForTeamByReferenceId_AsksTheTrackerNothingWhenNoKeysAreNamed()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            var requestsBefore = jira.SearchRequests.Count();

            var workItems = await subject.GetWorkItemsForTeam(team, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Empty);
                Assert.That(jira.SearchRequests.Count(), Is.EqualTo(requestsBefore),
                    "An empty key list as JQL is an empty JQL, which Jira answers with the whole project.");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeamByReferenceId_SplitsALargeSetAcrossSeveralRequests()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            var requestsBefore = jira.SearchRequests.Count();
            var manyKeys = Enumerable.Range(1, 250).Select(number => $"PROJ-{number}").ToList();

            await subject.GetWorkItemsForTeam(team, manyKeys);

            Assert.That(jira.SearchRequests.Count() - requestsBefore, Is.EqualTo(2),
                "250 key clauses in one GET is a URL no proxy is obliged to carry; the batch is chunked the way "
                + "the Azure DevOps connector chunks its id list.");
        }

        [Test]
        public async Task GetWorkItemsForTeamByReferenceId_DownloadsTheFullPayloadIncludingAPagedChangelog()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            jira.ChangelogEntryCount = 31;

            var workItems = await subject.GetWorkItemsForTeam(team, ["PROJ-1"]);

            var byKeyRequest = jira.SearchRequests.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems.Count(), Is.EqualTo(1), "positive control: the canned response was not read at all.");
                Assert.That(QueryValue(byKeyRequest, "fields"), Is.EqualTo(EveryField),
                    "Phase 2 is the download - narrowing it here would store a work item with holes in it.");
                Assert.That(byKeyRequest.Query, Does.Contain("expand=changelog"));
                Assert.That(jira.Requests.Any(uri => uri.AbsolutePath.EndsWith("/changelog", StringComparison.Ordinal)), Is.True,
                    "Jira caps the inlined changelog at 30 entries; the 31st only arrives via the paged endpoint.");
            }
        }

        [Test]
        public async Task GetParentFeaturesDetails_AndTheByKeyFetch_IssueTheSameQuery()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);

            await subject.GetWorkItemsForTeam(team, TheTwoKeysAskedFor);
            var byKeyQuery = QueryValue(jira.SearchRequests.Last(), "jql");

            await subject.GetParentFeaturesDetails(CreatePortfolio(team), TheTwoKeysAskedFor);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(byKeyQuery),
                "Two callers, one query. A second copy of the key-OR builder drifts the moment either side "
                + "learns to escape a quote.");
        }

        [Test]
        public async Task SweepFeaturesForPortfolio_AsksTheVeryQuestionTheWholeFeatureQueryAsks()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);
            var fullFetchQuery = QueryValue(jira.SearchRequests.Single(), "jql");

            await subject.SweepFeaturesForPortfolio(portfolio);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(fullFetchQuery),
                "Removal is 'stored minus swept'. A sweep that enumerates anything other than the exact "
                + "query the whole Feature fetch enumerates deletes whatever the two disagree about.");
        }

        [Test]
        public async Task SweepFeaturesForPortfolio_AsksOnlyForIdentityAndTheChangeStamp()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);

            await subject.SweepFeaturesForPortfolio(portfolio);

            var sweep = jira.SearchRequests.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(QueryValue(sweep, "fields"), Is.EqualTo(SweepFieldList),
                    "Downloading *all during the sweep would cost exactly what the two-phase fetch exists to save.");
                Assert.That(sweep.Query, Does.Not.Contain("expand=changelog"),
                    "The changelog is the single most expensive part of a Jira issue and phase 1 never reads it.");
            }
        }

        [Test]
        public async Task SweepFeaturesForPortfolio_WalksEveryPage()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);
            jira.QueueSweepPage(SweepIssue("PROJ-1", "2026-08-01T10:00:00.000+0000"), nextPageToken: TheSecondPage);
            jira.QueueSweepPage(SweepIssue("PROJ-2", "2026-08-02T10:00:00.000+0000"), nextPageToken: null);

            var stamps = await subject.SweepFeaturesForPortfolio(portfolio);

            Assert.That(stamps.Select(stamp => stamp.ReferenceId), Is.EqualTo(BothPages),
                "A sweep that stops at page one under-reports the query, and 'stored minus swept' deletes every Feature it missed.");
        }

        [Test]
        public async Task SweepFeaturesForPortfolio_RefusesToReportAHalfWalkedQuery()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);
            jira.QueueSweepPage(SweepIssue("PROJ-1", "2026-08-01T10:00:00.000+0000"), nextPageToken: TheSecondPage);
            jira.FailTheSearchAfterTheNextOne();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(async () => await subject.SweepFeaturesForPortfolio(portfolio), Throws.TypeOf<InvalidOperationException>(),
                    "Returning the first page as if it were the whole query is the one answer removal cannot survive: "
                    + "every Feature on the pages that never arrived would be deleted. Throwing falls back to a full fetch. "
                    + "The rejected page is what has to be reported - a refusal to sweep this deployment at all would "
                    + "satisfy a looser assertion without a single page ever being walked.");
                Assert.That(TheTokensTheSweepAskedFor(jira), Does.Contain(TheSecondPage),
                    "The half-walk is the whole point: the token the first page handed back has to have been asked for "
                    + "before the rejection, or the refusal being asserted is some earlier refusal that never "
                    + "enumerated anything.");
            }
        }

        [Test]
        public async Task SweepFeaturesForPortfolio_ReportsTheChangeStampTheFullFetchWouldStore()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud, StampTwoHoursAheadOfUtc);
            jira.QueueSweepPage(SweepIssue("PROJ-1", StampTwoHoursAheadOfUtc), nextPageToken: null);

            var stamps = await subject.SweepFeaturesForPortfolio(portfolio);
            var stored = (await subject.GetFeaturesForProject(portfolio)).Single();

            Assert.That(stamps.Single().ChangedAt, Is.EqualTo(stored.LastChangedRemote),
                "The swept stamp is compared against the stored one Feature by Feature. Two different parses of "
                + "the same string would make every Feature look moved forever.");
        }

        [Test]
        public async Task SweepFeaturesForPortfolio_StillReportsARecordTheTrackerGaveNoStampFor()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);
            jira.QueueSweepPage(SweepIssue("PROJ-1", updated: null), nextPageToken: null);

            var stamps = await subject.SweepFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stamps.Select(stamp => stamp.ReferenceId), Is.EqualTo(TheOnlyRecord),
                    "Dropping it from the sweep would put it in 'stored minus swept' and delete a live Feature.");
                Assert.That(stamps.Single().ChangedAt, Is.Default,
                    "No stamp can never equal a stored stamp, so the Feature is re-downloaded rather than assumed unchanged.");
            }
        }

        [Test]
        public async Task GetFeaturesForProjectByReferenceId_NamesOnlyTheKeysItWasAskedFor()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);

            await subject.GetFeaturesForProject(portfolio, TheTwoKeysAskedFor);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(TheKeyedQuery),
                "Phase 2 downloads what moved and nothing else - re-applying the portfolio filter would let the cutoff "
                + "date silently drop a Feature the sweep just reported as changed.");
        }

        [Test]
        public async Task GetFeaturesForProjectByReferenceId_AsksTheTrackerNothingWhenNoKeysAreNamed()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);
            var searchesBefore = jira.SearchRequests.Count();
            var requestsBefore = jira.Requests.Count;

            var features = await subject.GetFeaturesForProject(portfolio, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features, Is.Empty);
                Assert.That(jira.SearchRequests.Count(), Is.EqualTo(searchesBefore),
                    "An empty key list as JQL is an empty JQL, which Jira answers with the whole project.");
                Assert.That(jira.Requests, Has.Count.EqualTo(requestsBefore),
                    "Nothing to fetch has to cost nothing, and the search is not the only round trip on this path - "
                    + "building Features re-reads the field catalogue. A quiet portfolio runs this every cycle, which "
                    + "is exactly the cost the epic exists to remove. Asked for: "
                    + string.Join(" | ", jira.Requests.Skip(requestsBefore).Select(uri => uri.AbsolutePath)));
            }
        }

        [Test]
        public async Task GetFeaturesForProjectByReferenceId_SplitsALargeSetAcrossSeveralRequests()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);
            var requestsBefore = jira.SearchRequests.Count();
            var manyKeys = Enumerable.Range(1, 250).Select(number => $"PROJ-{number}").ToList();

            await subject.GetFeaturesForProject(portfolio, manyKeys);

            Assert.That(jira.SearchRequests.Count() - requestsBefore, Is.EqualTo(2),
                "250 key clauses in one GET is a URL no proxy is obliged to carry; the batch is chunked the way "
                + "the by-key Work Item fetch chunks its id list.");
        }

        [Test]
        public async Task GetFeaturesForProjectByReferenceId_DownloadsTheFullPayloadIncludingAPagedChangelog()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);
            jira.ChangelogEntryCount = 31;

            var features = await subject.GetFeaturesForProject(portfolio, ["PROJ-1"]);

            var byKeyRequest = jira.SearchRequests.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features, Has.Count.EqualTo(1), "positive control: the canned response was not read at all.");
                Assert.That(features[0].LastChangedRemote, Is.Not.Null,
                    "The download has to stamp what it returns, or the next cycle has nothing to compare against "
                    + "and downloads everything, every time.");
                Assert.That(QueryValue(byKeyRequest, "fields"), Is.EqualTo(EveryField),
                    "Phase 2 is the download - narrowing it here would store a Feature with holes in it.");
                Assert.That(byKeyRequest.Query, Does.Contain("expand=changelog"));
                Assert.That(jira.Requests.Any(uri => uri.AbsolutePath.EndsWith("/changelog", StringComparison.Ordinal)), Is.True,
                    "Jira caps the inlined changelog at 30 entries; the 31st only arrives via the paged endpoint.");
            }
        }

        [Test]
        public async Task SweepParentFeatures_SweepsTheSameKeyedQueryTheParentDetailFetchIssues()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);

            await subject.GetParentFeaturesDetails(portfolio, TheTwoKeysAskedFor);
            var detailQuery = QueryValue(jira.SearchRequests.Last(), "jql");

            await subject.SweepParentFeatures(portfolio, TheTwoKeysAskedFor);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(detailQuery),
                "The parent sweep and the parent detail fetch answer for the same set of keys. A sweep that names "
                + "a different set makes the per-record comparison meaningless for whatever the two disagree about.");
        }

        [Test]
        public async Task SweepParentFeatures_AsksOnlyForIdentityAndTheChangeStamp()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);

            await subject.SweepParentFeatures(portfolio, ["PROJ-1"]);

            var sweep = jira.SearchRequests.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(QueryValue(sweep, "fields"), Is.EqualTo(SweepFieldList));
                Assert.That(sweep.Query, Does.Not.Contain("expand=changelog"),
                    "A parent sweep that pulled the changelog would cost more than the detail fetch it exists to avoid.");
            }
        }

        [Test]
        public async Task SweepParentFeatures_SplitsALargeSetAcrossSeveralRequests()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(Cloud);
            var requestsBefore = jira.SearchRequests.Count();
            var manyKeys = Enumerable.Range(1, 250).Select(number => $"PROJ-{number}").ToList();

            await subject.SweepParentFeatures(portfolio, manyKeys);

            Assert.That(jira.SearchRequests.Count() - requestsBefore, Is.EqualTo(2),
                "Chunked the way the keyed detail fetch is chunked - a single 250-clause URL is one no proxy is obliged to carry.");
        }

        // --- Jira Data Center ---

        [Test]
        public async Task SupportsIncrementalSync_IsTrueForDataCenterOnceTheDeploymentIsKnown()
        {
            var (subject, team, _) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter);

            Assert.That(subject.SupportsIncrementalSync(team.WorkTrackingSystemConnection), Is.True,
                "A real instance answered the question the probe was written for: three back-to-back walks over "
                + "5056 issues returned the same id set every time, so 'stored minus swept' can be trusted here.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_OnDataCenter_WalksTheOffsetPagedSearchEndpoint()
        {
            var (subject, team, jira) = await AJiraDataCenterThatPagesOneIssueAtATime();
            jira.OffsetSweepIssues.Add(SweepIssue("PROJ-1", "2026-08-01T10:00:00.000+0000"));
            jira.OffsetSweepIssues.Add(SweepIssue("PROJ-2", "2026-08-02T10:00:00.000+0000"));
            var searchesBefore = jira.SearchRequests.Count();

            var stamps = await subject.SweepWorkItemsForTeam(team);

            var sweepRequests = jira.SearchRequests.Skip(searchesBefore).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stamps.Select(stamp => stamp.ReferenceId), Is.EqualTo(BothPages),
                    "A sweep that stops at the first page under-reports the query, and 'stored minus swept' deletes everything it missed.");
                Assert.That(sweepRequests.ConvertAll(uri => uri.AbsolutePath).Distinct(), Is.EqualTo(TheDataCenterSearchPathOnly),
                    "Data Center has no rest/api/3/search/jql - the endpoint the Cloud walk uses answers 404 here, "
                    + "so a sweep that reuses it never enumerates anything at all.");
                Assert.That(sweepRequests.ConvertAll(uri => QueryValue(uri, "startAt")), Does.Contain("1"),
                    "Without a page token the only way to reach page two is to ask for the next offset.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_OnDataCenter_SpendsOneRoundTripPerPageAndNotOneMore()
        {
            var (subject, team, jira) = await AJiraDataCenterPagingBy(2);
            jira.OffsetSweepIssues.AddRange(FourRecordsOverTwoPages.Select(key => SweepIssue(key, "2026-08-01T10:00:00.000+0000")));
            var searchesBefore = jira.SearchRequests.Count();

            var stamps = await subject.SweepWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stamps.Select(stamp => stamp.ReferenceId), Is.EqualTo(FourRecordsOverTwoPages),
                    "Every record the query holds, each of them once. An offset that moves by anything other than the "
                    + "page size just read either steps over records or reads the same ones again.");
                Assert.That(jira.SearchRequests.Count() - searchesBefore, Is.EqualTo(2),
                    "Four records at two per page is two requests, and how many requests a sweep spends is the whole "
                    + "reason it exists. Asking once more after the offset has already reached the total wastes a "
                    + "round trip every cycle, and an offset creeping forward by one turns the walk over the "
                    + "instance this slice was written for into a request per issue.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_OnDataCenter_AsksOnlyForIdentityAndTheChangeStamp()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter);

            await subject.SweepWorkItemsForTeam(team);

            var sweep = jira.SearchRequests.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(QueryValue(sweep, "fields"), Is.EqualTo(SweepFieldList),
                    "Data Center returns every field when none is named, which costs exactly what the two-phase fetch exists to save.");
                Assert.That(sweep.Query, Does.Not.Contain("expand=changelog"),
                    "The changelog is the single most expensive part of a Jira issue, and on the instance this slice "
                    + "was written for it is decades deep. Phase 1 never reads it.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_OnDataCenter_EnumeratesTheSameQueryTheWholeDownloadEnumerates()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter);
            var fullFetchQuery = QueryValue(jira.SearchRequests.Single(), "jql").Trim();

            await subject.SweepWorkItemsForTeam(team);

            Assert.That(WithoutOrdering(QueryValue(jira.SearchRequests.Last(), "jql")), Is.EqualTo(fullFetchQuery),
                "Removal is 'stored minus swept'. A sweep that enumerates anything other than the exact query the "
                + "full fetch enumerates deletes whatever the two disagree about - and on this instance that is decades of work.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_OnDataCenter_WalksInAnOrderNoEditCanDisturb()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter);

            await subject.SweepWorkItemsForTeam(team);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Does.EndWith(TheDeterministicOrdering),
                "Offset paging asks for 'issues 500 to 549 of the current answer'. Someone editing an issue mid-walk "
                + "reshuffles Jira's default relevance ordering, which can move a record onto a page already read - so "
                + "it never appears in the sweep, and 'stored minus swept' deletes it. Ordering on the key cannot be "
                + "reshuffled by an edit, because an issue's key never changes. The probe ran on a quiet instance and "
                + "could not exercise this, which is why it is asserted rather than measured.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_OnDataCenter_LeavesAQueryThatAlreadyOrdersWithOneOrderingClause()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter);
            team.DataRetrievalValue = AQueryTheOperatorOrdered;

            await subject.SweepWorkItemsForTeam(team);

            var jql = QueryValue(jira.SearchRequests.Last(), "jql");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OrderingClausesIn(jql), Is.EqualTo(1),
                    "Two ordering clauses is not valid JQL, so the sweep would fail for every instance whose own query "
                    + "already orders - and an operator who writes their own JQL is exactly who runs this deployment. "
                    + $"Asked for: {jql}");
                Assert.That(jql, Does.EndWith(TheDeterministicOrdering),
                    "The one that survives has to be the sweep's: the user's ordering is the one an edit can reshuffle.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_OnDataCenter_RefusesToReportAHalfWalkedQuery()
        {
            var (subject, team, jira) = await AJiraDataCenterThatPagesOneIssueAtATime();
            jira.OffsetSweepIssues.Add(SweepIssue("PROJ-1", "2026-08-01T10:00:00.000+0000"));
            jira.OffsetSweepIssues.Add(SweepIssue("PROJ-2", "2026-08-02T10:00:00.000+0000"));
            jira.FailTheSearchAfterTheNextOne();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(async () => await subject.SweepWorkItemsForTeam(team), Throws.TypeOf<InvalidOperationException>(),
                    "Returning the first offset as if it were the whole query is the one answer removal cannot survive: "
                    + "every record on the pages that never arrived would be deleted. Throwing falls back to a full fetch. "
                    + "The rejected page is what has to be reported - a refusal to sweep this deployment at all would "
                    + "satisfy a looser assertion without a single page ever being walked.");
                Assert.That(jira.SearchRequests.ToList().ConvertAll(uri => QueryValue(uri, "startAt")), Does.Contain("1"),
                    "The half-walk is the whole point: one page has to have been read before the rejection, or the "
                    + "refusal being asserted is some earlier refusal that never enumerated anything.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_OnDataCenter_ReportsTheChangeStampTheFullFetchWouldStore()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter, StampTwoHoursAheadOfUtc);
            jira.OffsetSweepIssues.Add(SweepIssue("PROJ-1", StampTwoHoursAheadOfUtc));

            var stamps = await subject.SweepWorkItemsForTeam(team);
            var stored = (await subject.GetWorkItemsForTeam(team)).Single();

            Assert.That(stamps.Single().ChangedAt, Is.EqualTo(stored.LastChangedRemote),
                "The sweep and the full fetch are compared against each other per record, with no watermark in "
                + "between. Two different readings of the same string would report every record as moved, forever.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_OnDataCenter_StillReportsARecordTheTrackerGaveNoStampFor()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter);
            jira.OffsetSweepIssues.Add(SweepIssue("PROJ-1", updated: null));

            var stamps = await subject.SweepWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stamps.Select(stamp => stamp.ReferenceId), Is.EqualTo(TheOnlyRecord),
                    "Dropping it from the sweep would put it in 'stored minus swept' and delete a live item.");
                Assert.That(stamps.Single().ChangedAt, Is.Default,
                    "No stamp can never equal a stored stamp, so the record is re-downloaded rather than assumed unchanged.");
            }
        }

        [Test]
        public async Task SweepFeaturesForPortfolio_OnDataCenter_EnumeratesTheSameFeatureQueryTheWholeDownloadEnumerates()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(DataCenter);
            var fullFetchQuery = QueryValue(jira.SearchRequests.Single(), "jql").Trim();

            await subject.SweepFeaturesForPortfolio(portfolio);

            var sweep = jira.SearchRequests.Last();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WithoutOrdering(QueryValue(sweep, "jql")), Is.EqualTo(fullFetchQuery),
                    "The portfolio half deletes Features the same way, so it enumerates the same query the same way.");
                Assert.That(sweep.AbsolutePath, Does.EndWith(DataCenterSearchPath),
                    "One sweep, reached by both entity types: a portfolio that walked its own way would be a second "
                    + "implementation to keep in step with the first.");
            }
        }

        [Test]
        public async Task SweepParentFeatures_OnDataCenter_SweepsTheSameKeyedQueryTheParentDetailFetchIssues()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(DataCenter);

            await subject.GetParentFeaturesDetails(portfolio, TheTwoKeysAskedFor);
            var detailQuery = QueryValue(jira.SearchRequests.Last(), "jql").Trim();

            await subject.SweepParentFeatures(portfolio, TheTwoKeysAskedFor);

            Assert.That(WithoutOrdering(QueryValue(jira.SearchRequests.Last(), "jql")), Is.EqualTo(detailQuery),
                "The parent sweep and the parent detail fetch answer for the same set of keys. A sweep that names a "
                + "different set makes the per-record comparison meaningless for whatever the two disagree about.");
        }

        [Test]
        public async Task GetFeaturesForProjectByReferenceId_OnDataCenter_NamesOnlyTheKeysItWasAskedFor()
        {
            var (subject, portfolio, jira) = await AJiraPortfolioThatHasAlreadyBeenTalkedTo(DataCenter);

            var features = await subject.GetFeaturesForProject(portfolio, TheTwoKeysAskedFor);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features, Is.Not.Empty, "positive control: the canned response was not read at all.");
                Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(TheKeyedQuery),
                    "Phase 2 downloads what moved and nothing else - re-applying the portfolio filter would let the "
                    + "cutoff date silently drop a Feature the sweep just reported as changed.");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeamByReferenceId_OnDataCenter_NamesOnlyTheKeysItWasAskedFor()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter);

            var workItems = await subject.GetWorkItemsForTeam(team, TheTwoKeysAskedFor);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Not.Empty, "positive control: the canned response was not read at all.");
                Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(TheKeyedQuery),
                    "Phase 2 downloads what moved and nothing else - re-applying the team filter would let the cutoff "
                    + "date silently drop an item the sweep just reported as changed.");
            }
        }

        private static Task<(JiraWorkTrackingConnector Subject, Team Team, JiraStub Jira)> AJiraDataCenterThatPagesOneIssueAtATime()
            => AJiraDataCenterPagingBy(1);

        /// <summary>
        /// A Data Center instance whose pages hold <paramref name="issuesPerPage"/> issues each, so both how far a
        /// walk gets and how many round trips it takes to get there are visible. The page size is a connection
        /// setting, which is the same lever an operator has in the field.
        /// </summary>
        private static async Task<(JiraWorkTrackingConnector Subject, Team Team, JiraStub Jira)> AJiraDataCenterPagingBy(int issuesPerPage)
        {
            var jira = new JiraStub(DataCenter);
            var subject = CreateSubject(jira.Handler);
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud(
                issuesPerRequestOption: issuesPerPage.ToString(CultureInfo.InvariantCulture), doneItemsCutoffDays: 30);

            // The deployment is discovered by asking the instance, so the capability probe only answers once a
            // first cycle has run - and that first cycle is a full download.
            await subject.GetWorkItemsForTeam(team);

            return (subject, team, jira);
        }

        /// <summary>The query without whatever it is ordered by - which is the part that says what it enumerates.</summary>
        private static string WithoutOrdering(string jql)
        {
            var orderingStarts = jql.IndexOf(OrderingKeyword, StringComparison.OrdinalIgnoreCase);

            return (orderingStarts < 0 ? jql : jql[..orderingStarts]).Trim();
        }

        private static int OrderingClausesIn(string jql)
            => jql.ToUpperInvariant().Split(OrderingKeyword, StringSplitOptions.None).Length - 1;

        private static async Task<(JiraWorkTrackingConnector Subject, Portfolio Portfolio, JiraStub Jira)> AJiraPortfolioThatHasAlreadyBeenTalkedTo(
            string deploymentType, string? updated = "2026-08-01T10:00:00.000+0000")
        {
            var jira = new JiraStub(deploymentType) { Updated = updated };
            var subject = CreateSubject(jira.Handler);
            var portfolio = CreatePortfolio(CreateTeam());

            // Same reason as the team-side helper: the deployment is discovered by asking the instance, so the
            // capability probe only answers once a first cycle has run - and that first cycle is a full download.
            await subject.GetFeaturesForProject(portfolio);

            return (subject, portfolio, jira);
        }

        private static async Task<(JiraWorkTrackingConnector Subject, Team Team, JiraStub Jira)> AJiraThatHasAlreadyBeenTalkedTo(
            string deploymentType, string? updated = "2026-08-01T10:00:00.000+0000")
        {
            var jira = new JiraStub(deploymentType) { Updated = updated };
            var subject = CreateSubject(jira.Handler);
            var team = CreateTeam();

            // Lighthouse learns the deployment by asking the instance, so the capability probe can only answer
            // once a first cycle has run. That first cycle is a full download, which is the safe resolution anyway.
            await subject.GetWorkItemsForTeam(team);

            return (subject, team, jira);
        }

        private static string SweepIssue(string key, string? updated)
        {
            var updatedField = updated is null ? string.Empty : $"\"updated\":{JsonSerializer.Serialize(updated)}";
            return $"{{\"key\":\"{key}\",\"fields\":{{{updatedField}}}}}";
        }

        /// <summary>
        /// The page tokens the Cloud sweep put on the wire, in order. Cloud has no offset to look at: the only
        /// evidence a second page was ever asked for is the token the first page handed back coming straight
        /// back out on the next request.
        /// </summary>
        private static List<string> TheTokensTheSweepAskedFor(JiraStub jira)
            => jira.SearchRequests.ToList().ConvertAll(uri => QueryValue(uri, "nextPageToken"));

        /// <summary>
        /// What has to hold of every query Lighthouse builds: Jira parses it. An empty bracket pair is the shape
        /// worth naming, because a query that consists only of an ordering clause - valid JQL, and a plausible
        /// saved-filter value - leaves nothing to wrap. Should Jira read the empty pair as "matches nothing"
        /// rather than rejecting it, the sweep reports no records at all, and removal is "stored minus swept":
        /// every stored record for that team or portfolio goes.
        /// </summary>
        private static void AssertJiraCanParseTheWholeFilter(string jql)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(jql, Does.Not.Contain("()"),
                    $"An empty filter is not the operator's filter, and Jira owes no answer to it. Asked for: {jql}");
                Assert.That(jql.Count(character => character == '('), Is.EqualTo(jql.Count(character => character == ')')),
                    $"Jira rejects an unbalanced query outright, and the team then fetches nothing at all. Asked for: {jql}");
                Assert.That(jql, Does.StartWith(TheTypeFilterOnItsOwn),
                    "Dropping the empty pair may not take the filters hung off it with it, and what is left may not "
                    + $"open on a dangling conjunction. Asked for: {jql}");
            }
        }

        private static string QueryValue(Uri uri, string name)
        {
            var pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            var match = Array.Find(pairs, pair => pair.StartsWith($"{name}=", StringComparison.Ordinal));

            return match is null ? string.Empty : Uri.UnescapeDataString(match[(name.Length + 1)..]);
        }

        private static async Task<string> TheQueryJiraIsAskedFor(string theTeamsOwnQuery)
        {
            var jira = new JiraStub(Cloud);
            var subject = CreateSubject(jira.Handler);
            var team = CreateTeam();
            team.DataRetrievalValue = theTeamsOwnQuery;

            await subject.GetWorkItemsForTeam(team);

            return QueryValue(jira.SearchRequests.Last(), "jql");
        }

        private static async Task<WorkItem> TheSingleWorkItemFetchedFrom(string deploymentType, string? updated)
        {
            var jira = new JiraStub(deploymentType) { Updated = updated };
            var subject = CreateSubject(jira.Handler);

            var workItems = await subject.GetWorkItemsForTeam(CreateTeam());

            Assert.That(workItems.Count(), Is.EqualTo(1), "positive control: the canned response was not read at all.");

            return workItems.Single();
        }

        /// <summary>
        /// A recording stand-in for a Jira instance: it answers the handful of endpoints the connector calls and
        /// keeps every request URI, which is where the interesting assertions live (which JQL, which fields).
        /// </summary>
        private sealed class JiraStub
        {
            private readonly string deploymentType;
            private readonly Queue<string> sweepPages = new();
            private int failSearchesFromRequestNumber = int.MaxValue;
            private int searchCount;

            public JiraStub(string deploymentType)
            {
                this.deploymentType = deploymentType;

                var mock = new Mock<HttpMessageHandler>();
                mock.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(Respond(request)));

                Handler = mock.Object;
            }

            public HttpMessageHandler Handler { get; }

            public List<Uri> Requests { get; } = [];

            public string? Updated { get; set; } = "2026-08-01T10:00:00.000+0000";

            public int ChangelogEntryCount { get; set; }

            /// <summary>
            /// What a Data Center sweep finds, in the order the instance would hand it back. Data Center has
            /// no page token, so pages are not queued: the stub slices this list by the offset and page size
            /// the request asks for, which is what makes a walk that ignores either one visible.
            /// </summary>
            public List<string> OffsetSweepIssues { get; } = [];

            public IEnumerable<Uri> SearchRequests => Requests.Where(uri => uri.AbsolutePath.Contains("search", StringComparison.Ordinal));

            public void QueueSweepPage(string issueJson, string? nextPageToken)
            {
                var token = nextPageToken is null ? string.Empty : $",\"nextPageToken\":\"{nextPageToken}\"";
                sweepPages.Enqueue($"{{\"issues\":[{issueJson}]{token}}}");
            }

            public void FailTheSearchAfterTheNextOne() => failSearchesFromRequestNumber = searchCount + 2;

            private HttpResponseMessage Respond(HttpRequestMessage request)
            {
                var uri = request.RequestUri ?? new Uri("https://unknown.invalid/");
                Requests.Add(uri);

                var path = uri.AbsolutePath;

                if (path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal))
                {
                    return Ok($"{{\"deploymentType\":\"{deploymentType}\"}}");
                }

                if (path.EndsWith("rest/api/latest/field", StringComparison.Ordinal))
                {
                    return Ok("[]");
                }

                if (path.EndsWith("/changelog", StringComparison.Ordinal))
                {
                    return Ok($"{{\"values\":[],\"isLast\":true,\"total\":{ChangelogEntryCount}}}");
                }

                if (path.Contains("search", StringComparison.Ordinal))
                {
                    return RespondToSearch(uri);
                }

                return Ok("{}");
            }

            private HttpResponseMessage RespondToSearch(Uri uri)
            {
                searchCount++;

                if (searchCount >= failSearchesFromRequestNumber)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                    };
                }

                if (QueryValue(uri, "fields") == SweepFieldList && !uri.AbsolutePath.Contains(CloudSearchPath, StringComparison.Ordinal))
                {
                    return Ok(OffsetSweepPage(uri));
                }

                if (sweepPages.Count > 0 && QueryValue(uri, "fields") != EveryField)
                {
                    return Ok(sweepPages.Dequeue());
                }

                var issues = string.Join(",", KeysAskedFor(uri).Select(IssueJson));

                return uri.AbsolutePath.Contains(CloudSearchPath, StringComparison.Ordinal)
                    ? Ok($"{{\"issues\":[{issues}]}}")
                    : Ok($"{{\"startAt\":0,\"maxResults\":50,\"total\":1,\"issues\":[{issues}]}}");
            }

            /// <summary>
            /// One page of an offset walk, echoing back the offset and page size that were asked for and the
            /// size of the whole result, the way Data Center's search endpoint answers.
            /// </summary>
            private string OffsetSweepPage(Uri uri)
            {
                var startAt = int.TryParse(QueryValue(uri, "startAt"), out var offset) ? offset : 0;
                var pageSize = int.TryParse(QueryValue(uri, "maxResults"), out var size) && size > 0 ? size : 50;
                var page = OffsetSweepIssues.Skip(startAt).Take(pageSize);

                return $"{{\"startAt\":{startAt},\"maxResults\":{pageSize},\"total\":{OffsetSweepIssues.Count},\"issues\":[{string.Join(",", page)}]}}";
            }

            private static IEnumerable<string> KeysAskedFor(Uri uri)
            {
                var jql = QueryValue(uri, "jql");

                if (!jql.StartsWith("key = ", StringComparison.Ordinal))
                {
                    return ["PROJ-1"];
                }

                return jql.Split(" OR ", StringSplitOptions.RemoveEmptyEntries)
                    .Select(clause => clause.Replace("key = ", string.Empty, StringComparison.Ordinal).Trim('"'));
            }

            private string IssueJson(string key)
            {
                var updatedField = Updated is null ? string.Empty : $"\"updated\":{JsonSerializer.Serialize(Updated)},";
                var changelog = ChangelogEntryCount > 0
                    ? $"\"changelog\":{{\"total\":{ChangelogEntryCount},\"histories\":[]}},"
                    : string.Empty;

                return $"{{\"key\":\"{key}\",{changelog}\"fields\":{{"
                    + "\"summary\":\"An issue\","
                    + "\"created\":\"2026-08-01T09:00:00.000+0000\","
                    + updatedField
                    + "\"status\":{\"name\":\"In Progress\"},"
                    + "\"issuetype\":{\"name\":\"Story\"}"
                    + "}}";
            }

            private static HttpResponseMessage Ok(string body) => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static JiraWorkTrackingConnector CreateSubject(HttpMessageHandler handler)
            => JiraConnectorTestSetup.AConnectorOver(handler);

        private static Portfolio CreatePortfolio(Team team)
        {
            var portfolio = new Portfolio
            {
                Id = team.Id,
                Name = $"Portfolio {team.Id}",
                DataRetrievalValue = "project = PROJ",
                WorkTrackingSystemConnectionId = team.WorkTrackingSystemConnectionId,
                WorkTrackingSystemConnection = team.WorkTrackingSystemConnection,
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Story");

            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("Done");
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("In Progress");
            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("To Do");

            return portfolio;
        }

        private static Team CreateTeam() => JiraConnectorTestSetup.ATeamOnJiraCloud(doneItemsCutoffDays: 30);
    }
}
