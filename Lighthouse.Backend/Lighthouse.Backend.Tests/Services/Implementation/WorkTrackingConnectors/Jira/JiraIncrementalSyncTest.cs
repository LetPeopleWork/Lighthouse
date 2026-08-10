using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Epic #5687 AC-2.1: the whole-query path carries Jira's <c>updated</c> onto
    /// <see cref="WorkItemBase.LastChangedRemote"/>. Without it a full cycle stores nothing to compare
    /// against, so every later cycle resolves to a full download (D8) and the delta never engages.
    /// Cloud and Data Center are the same connector class, so both deployments are exercised.
    ///
    /// The rest of the fixture covers slice 02's two-phase fetch for Jira Cloud: the identity sweep (phase 1)
    /// and the fetch-by-key (phase 2). The acceptance suite fakes <c>IWorkTrackingConnector</c> by policy and
    /// therefore cannot see any of this - these tests are the only evidence the Jira side actually works.
    /// </summary>
    [TestFixture]
    public class JiraIncrementalSyncTest
    {
        private const string Cloud = "Cloud";
        private const string DataCenter = "Server";

        private static readonly string[] BothPages = ["PROJ-1", "PROJ-2"];
        private static readonly string[] TheOnlyRecord = ["PROJ-1"];

        private static int connectionIdSeed = 9500;

        [TestCase(Cloud)]
        [TestCase(DataCenter)]
        public async Task GetWorkItemsForTeam_RemembersWhenTheIssueLastChangedRemotely(string deploymentType)
        {
            var workItem = await TheSingleWorkItemFetchedFrom(deploymentType, updated: "2026-08-05T14:30:00.000+0200");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.LastChangedRemote, Is.EqualTo(new DateTime(2026, 8, 5, 12, 30, 0, DateTimeKind.Utc)));
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
                "No stamp means 'never swept', which resolves the next update to a full fetch (D8). "
                + "A sentinel date would claim knowledge the tracker never gave.");
        }

        [TestCase(Cloud)]
        [TestCase(DataCenter)]
        public async Task GetWorkItemsForTeam_LeavesTheStampEmptyWhenTheTrackerReportsSomethingUnreadable(string deploymentType)
        {
            var workItem = await TheSingleWorkItemFetchedFrom(deploymentType, updated: "not a date");

            Assert.That(workItem.LastChangedRemote, Is.Null,
                "An unreadable stamp is no knowledge at all - falling back to a full fetch is the safe resolution (D8).");
        }

        [Test]
        public void SupportsIncrementalSync_IsFalseBeforeLighthouseHasEverReachedTheInstance()
        {
            var jira = new JiraStub(Cloud);
            var subject = CreateSubject(jira.Handler);
            var team = CreateTeam();

            Assert.That(subject.SupportsIncrementalSync(team.WorkTrackingSystemConnection), Is.False,
                "The port member cannot block on a network round trip, so an undiscovered deployment answers "
                + "'no' - which resolves the cycle to a full download (D8) rather than to a guess.");
        }

        [Test]
        public async Task SupportsIncrementalSync_IsTrueForCloudOnceTheDeploymentIsKnown()
        {
            var (subject, team, _) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);

            Assert.That(subject.SupportsIncrementalSync(team.WorkTrackingSystemConnection), Is.True);
        }

        [Test]
        public async Task SupportsIncrementalSync_StaysFalseForDataCenter()
        {
            var (subject, team, _) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter);

            Assert.That(subject.SupportsIncrementalSync(team.WorkTrackingSystemConnection), Is.False,
                "Data Center answers only once slice 04 settles OQ-1 - whether its offset pagination returns a "
                + "stable id set. An unstable set turns 'removed = stored - swept' into a deletion of live items.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_AsksTheVeryQuestionTheWholeQueryAsks()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            var fullFetchQuery = QueryValue(jira.SearchRequests.Single(), "jql");

            await subject.SweepWorkItemsForTeam(team);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(fullFetchQuery),
                "Removal is 'stored minus swept' (D2). A sweep that enumerates anything other than the exact "
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
                Assert.That(QueryValue(sweep, "fields"), Is.EqualTo("key,updated"),
                    "Downloading *all during the sweep would cost exactly what the two-phase fetch exists to save.");
                Assert.That(sweep.Query, Does.Not.Contain("expand=changelog"),
                    "The changelog is the single most expensive part of a Jira issue and phase 1 never reads it.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_WalksEveryPage()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            jira.QueueSweepPage(SweepIssue("PROJ-1", "2026-08-01T10:00:00.000+0000"), nextPageToken: "page-2");
            jira.QueueSweepPage(SweepIssue("PROJ-2", "2026-08-02T10:00:00.000+0000"), nextPageToken: null);

            var stamps = await subject.SweepWorkItemsForTeam(team);

            Assert.That(stamps.Select(stamp => stamp.ReferenceId), Is.EqualTo(BothPages),
                "A sweep that stops at page one under-reports the query, and D2 deletes everything it missed.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_ReportsTheChangeStampTheFullFetchWouldStore()
        {
            const string updated = "2026-08-05T14:30:00.000+0200";

            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud, updated);
            jira.QueueSweepPage(SweepIssue("PROJ-1", updated), nextPageToken: null);

            var stamps = await subject.SweepWorkItemsForTeam(team);
            var stored = (await subject.GetWorkItemsForTeam(team)).Single();

            Assert.That(stamps.Single().ChangedAt, Is.EqualTo(stored.LastChangedRemote),
                "D12 compares the swept stamp against the stored one per item. Two different parses of the same "
                + "string would make every item look moved forever.");
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
                    "Dropping it from the sweep would put it in 'stored minus swept' and delete a live item (D2).");
                Assert.That(stamps.Single().ChangedAt, Is.Default,
                    "No stamp can never equal a stored stamp, so the record is re-downloaded rather than assumed unchanged (D8).");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_RefusesToReportAHalfWalkedQuery()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);
            jira.QueueSweepPage(SweepIssue("PROJ-1", "2026-08-01T10:00:00.000+0000"), nextPageToken: "page-2");
            jira.FailTheSearchAfterTheNextOne();

            Assert.That(async () => await subject.SweepWorkItemsForTeam(team), Throws.Exception,
                "Returning the first page as if it were the whole query is the one answer D2 cannot survive: "
                + "every record on the pages that never arrived would be deleted. Throwing falls back to a full fetch.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_RefusesOnDataCenter()
        {
            var (subject, team, _) = await AJiraThatHasAlreadyBeenTalkedTo(DataCenter);

            Assert.That(async () => await subject.SweepWorkItemsForTeam(team), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public async Task GetWorkItemsForTeamByReferenceId_NamesOnlyTheKeysItWasAskedFor()
        {
            var (subject, team, jira) = await AJiraThatHasAlreadyBeenTalkedTo(Cloud);

            await subject.GetWorkItemsForTeam(team, ["PROJ-1", "PROJ-3"]);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo("key = \"PROJ-1\" OR key = \"PROJ-3\""),
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
                Assert.That(QueryValue(byKeyRequest, "fields"), Is.EqualTo("*all"),
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
            string[] keys = ["PROJ-1", "PROJ-3"];

            await subject.GetWorkItemsForTeam(team, keys);
            var byKeyQuery = QueryValue(jira.SearchRequests.Last(), "jql");

            await subject.GetParentFeaturesDetails(CreatePortfolio(team), keys);

            Assert.That(QueryValue(jira.SearchRequests.Last(), "jql"), Is.EqualTo(byKeyQuery),
                "Two callers, one query. A second copy of the key-OR builder drifts the moment either side "
                + "learns to escape a quote.");
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

        private static string QueryValue(Uri uri, string name)
        {
            var pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            var match = Array.Find(pairs, pair => pair.StartsWith($"{name}=", StringComparison.Ordinal));

            return match is null ? string.Empty : Uri.UnescapeDataString(match[(name.Length + 1)..]);
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

                if (sweepPages.Count > 0 && QueryValue(uri, "fields") != "*all")
                {
                    return Ok(sweepPages.Dequeue());
                }

                var issues = string.Join(",", KeysAskedFor(uri).Select(IssueJson));

                return uri.AbsolutePath.Contains("rest/api/3/search/jql", StringComparison.Ordinal)
                    ? Ok($"{{\"issues\":[{issues}]}}")
                    : Ok($"{{\"startAt\":0,\"maxResults\":50,\"total\":1,\"issues\":[{issues}]}}");
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
        {
            var strategyMock = new Mock<IWorkTrackingAuthStrategy>();
            strategyMock
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factoryMock = new Mock<IWorkTrackingAuthStrategyFactory>();
            factoryMock
                .Setup(f => f.Resolve(It.IsAny<string>()))
                .Returns(strategyMock.Object);

            return new JiraWorkTrackingConnector(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                factoryMock.Object,
                handler);
        }

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

        private static Team CreateTeam()
        {
            var connectionId = Interlocked.Increment(ref connectionIdSeed);
            var url = $"https://jira-{connectionId}.example.invalid";

            var connection = new WorkTrackingSystemConnection
            {
                Id = connectionId,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = $"Test Setting {connectionId}",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = url, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "user@example.com", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = "token", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "10", IsSecret = false },
            ]);

            var team = new Team
            {
                Id = connectionId,
                Name = $"Team {connectionId}",
                DataRetrievalValue = "project = PROJ",
                DoneItemsCutoffDays = 30,
                WorkTrackingSystemConnectionId = connectionId,
                WorkTrackingSystemConnection = connection,
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Story");

            team.DoneStates.Clear();
            team.DoneStates.Add("Done");
            team.DoingStates.Clear();
            team.DoingStates.Add("In Progress");
            team.ToDoStates.Clear();
            team.ToDoStates.Add("To Do");

            return team;
        }
    }
}
