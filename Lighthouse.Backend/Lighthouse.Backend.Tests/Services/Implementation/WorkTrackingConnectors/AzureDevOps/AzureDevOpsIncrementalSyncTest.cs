using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using static Lighthouse.Backend.Tests.TestHelpers.AzureDevOpsOrganisation;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// The two-phase fetch for Azure DevOps. On this connector the expensive part is not the query - a WIQL
    /// already answers with nothing but ids - it is what happens per returned id afterwards: a payload read
    /// and then one <c>GetRevisionsAsync</c> round trip per item to rebuild its state transitions, on every
    /// cycle, for every item, whether or not the item moved.
    ///
    /// A WIQL cannot report a change stamp - it answers with references, and field values need a separate
    /// read - so the sweep is the same WIQL plus one batched read of <c>System.ChangedDate</c> for the ids it
    /// returned. Two hundred ids per request, and no revisions at all.
    ///
    /// The acceptance suite fakes <c>IWorkTrackingConnector</c> by policy and therefore cannot see a single
    /// one of these requests. These tests are the only evidence the Azure DevOps side works.
    /// </summary>
    [TestFixture]
    [Category("epic-5687-faster-updates")]
    [Category("slice-06")]
    public class AzureDevOpsIncrementalSyncTest
    {
        private const int TheOnlyItem = 1;
        private const int TheItemThatMoved = 1;
        private const int TheItemThatDidNot = 2;

        private const string ARefusalHereStopsAWorkingTeam =
            "One of the three things a configuration can narrow on is enough to ask a question with. A "
            + "guard that refuses this stops an installation that was refreshing fine yesterday.";

        private static readonly int[] TheTwoIdsAskedFor = [TheItemThatMoved, 3];

        private static readonly string[] TheTwoReferenceIdsAskedFor = ["1", "3"];

        private static readonly string[] TheStampAndNothingElse = [AzureDevOpsFieldNames.ChangedDate];

        [Test]
        public void SupportsIncrementalSync_IsTrueForAzureDevOps()
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds(TheOnlyItem);

            Assert.That(subject.SupportsIncrementalSync(team.WorkTrackingSystemConnection), Is.True,
                "While this answers no, every Azure DevOps cycle downloads the whole query and re-reads every "
                + "item's revision history - which is the cost this slice exists to remove.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_RemembersWhenTheItemLastChangedRemotely()
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds(TheOnlyItem);

            var workItem = (await subject.GetWorkItemsForTeam(team, CancellationToken.None)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.LastChangedRemote, Is.EqualTo(WhenTheTrackerSaysItLastChanged),
                    "A full cycle that stores no stamp leaves the next cycle nothing to compare against, so it "
                    + "downloads everything too - the cheap path never engages and no test goes red.");
                Assert.That(workItem.LastChangedRemote?.Kind, Is.EqualTo(DateTimeKind.Utc),
                    "An instant has no time zone. Stored as anything else, the next cycle compares two readings "
                    + "of the same moment and reports every item as moved.");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_AsksTheTrackerForTheChangeStamp()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);

            await subject.GetWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(ado.FieldsOfTheItemRead, Does.Contain(AzureDevOpsFieldNames.ChangedDate),
                "A payload read that never names the stamp gets no stamp back, however carefully the mapping "
                + "reads it - and an item stored without one forces a full cycle forever.");
        }

        [Test]
        public async Task GetFeaturesForProject_RemembersWhenTheFeatureLastChangedRemotely()
        {
            var (subject, portfolio, _) = AnAzureDevOpsPortfolioThatHolds(TheOnlyItem);

            var feature = (await subject.GetFeaturesForProject(portfolio, CancellationToken.None)).Single();

            Assert.That(feature.LastChangedRemote, Is.EqualTo(WhenTheTrackerSaysItLastChanged),
                "The portfolio half stores its own stamps; a Feature without one keeps the portfolio on full "
                + "cycles while the team half is already cheap.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_AsksTheSameQuestionTheWholeDownloadAsks()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);

            await subject.GetWorkItemsForTeam(team, CancellationToken.None);
            var whatTheDownloadAsked = ado.WiqlQueries[^1];
            ado.WiqlQueries.Clear();

            await subject.SweepWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(ado.WiqlQueries, Has.One.EqualTo(whatTheDownloadAsked),
                "Removal is 'stored minus swept'. Any drift between the two queries deletes whatever they "
                + "disagree about, and a filter is easy to leave off one of them.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_AsksOnlyForTheChangeStamp()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);

            await subject.SweepWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(ado.PayloadReads, Has.Count.EqualTo(1),
                "One read per batch of ids is the whole sweep. A second read is the payload, the relations or the "
                + "revisions - whichever it is, it is the cost the sweep exists to avoid.");

            var stampRead = ado.PayloadReads[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stampRead.Fields, Is.EqualTo(TheStampAndNothingElse),
                    "The sweep runs for every item every cycle. Asking for one more field than the stamp is "
                    + "paying the download it exists to avoid.");
                Assert.That(stampRead.Expand, Is.Null.Or.EqualTo(WorkItemExpand.None),
                    "Azure DevOps refuses a read that names fields and an expansion at once, and every expansion "
                    + "there is - links, relations - is payload the sweep exists to skip.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_ReadsNoRevisionsAtAll()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheItemThatMoved, TheItemThatDidNot);

            await subject.SweepWorkItemsForTeam(team, CancellationToken.None);

            Assert.That(ado.RevisionReads, Is.Empty,
                "One revision read per item per cycle is the dominant cost on Azure DevOps. A sweep that pays "
                + "it has bought nothing, and would be indistinguishable from today by the clock.");
        }

        [Test]
        public async Task SweepWorkItemsForTeam_ReportsTheStampTheWholeDownloadWouldStore()
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds(TheOnlyItem);

            var whatTheDownloadStores = (await subject.GetWorkItemsForTeam(team, CancellationToken.None)).Single().LastChangedRemote;
            var whatTheSweepReports = (await subject.SweepWorkItemsForTeam(team, CancellationToken.None)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whatTheSweepReports.ChangedAt, Is.EqualTo(whatTheDownloadStores),
                    "The swept stamp is compared against the stored one with nothing in between. Two readings of "
                    + "the same field report every item as moved, forever, and nothing looks broken.");
                Assert.That(whatTheSweepReports.ChangedAt.Kind, Is.EqualTo(DateTimeKind.Utc),
                    "Two values an hour apart still compare unequal even when they name the same instant.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_StillReportsAnIdTheTrackerGaveNoStampFor()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.ChangedDate = null;

            var swept = await subject.SweepWorkItemsForTeam(team, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(swept.Select(record => record.ReferenceId), Does.Contain($"{TheOnlyItem}"),
                    "Leaving it out of the sweep puts it in 'stored minus swept', which deletes a live item. "
                    + "Reporting it without a stamp merely downloads it again.");
                Assert.That(swept.Single().ChangedAt, Is.Default,
                    "A stamp nobody can match is what makes the item look moved, which is the safe answer.");
            }
        }

        [Test]
        public void SweepWorkItemsForTeam_RefusesToReportASweepTheTrackerOnlyHalfAnswered()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheItemThatMoved, TheItemThatDidNot);
            ado.AnswerPayloadReadsWithNothing = true;

            Assert.That(async () => await subject.SweepWorkItemsForTeam(team, CancellationToken.None),
                Throws.TypeOf<InvalidOperationException>(),
                "A sweep that answers for fewer ids than the query returned puts every unanswered id in "
                + "'stored minus swept'. Refusing outright costs one full download; answering costs the items.");
        }

        [Test]
        public void SweepWorkItemsForTeam_RefusesWhenTheTrackerWillNotAnswerTheQueryAtAll()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheItemThatMoved, TheItemThatDidNot);
            ado.RejectTheQuery = true;

            Assert.That(async () => await subject.SweepWorkItemsForTeam(team, CancellationToken.None),
                Throws.Exception,
                "An empty sweep does not mean 'the query failed', it means 'the query matches nothing' - and "
                + "removal is 'stored minus swept', so answering a rejected query with an empty sweep deletes "
                + "every record the team has. One transient error would empty the team.");
        }

        [Test]
        public void SweepWorkItemsForTeam_RefusesWhenTheTrackerAnswersWithoutAResultSet()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheItemThatMoved, TheItemThatDidNot);
            ado.AnswerTheQueryWithoutAResultSet = true;

            Assert.That(async () => await subject.SweepWorkItemsForTeam(team, CancellationToken.None),
                Throws.TypeOf<InvalidOperationException>(),
                "No result set is not the same answer as an empty one. Treating it as 'nothing matched' hands "
                + "removal an empty sweep, which deletes every record the team has.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_ByReferenceId_AsksTheTrackerNothingWhenNothingMoved()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheItemThatMoved);

            var workItems = await subject.GetWorkItemsForTeam(team, [], CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems, Is.Empty);
                Assert.That(ado.EveryRequestMade, Is.Empty,
                    "A cycle in which nothing moved should cost nothing. A keyed read of no keys is still a "
                    + "round trip, and so is the field lookup that precedes it - on the quiet cycle this epic "
                    + "exists to make cheap.");
            }
        }

        [Test]
        public async Task SweepWorkItemsForTeam_ReadsTheStampsInBatchesTheTrackerAccepts()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds([.. Enumerable.Range(1, 201)]);

            await subject.SweepWorkItemsForTeam(team, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ado.PayloadReads, Has.Count.GreaterThan(1),
                    "Azure DevOps rejects a read of more than two hundred ids outright, so a single-request "
                    + "sweep fails on every real team - and the fallback hides it as merely slow.");
                Assert.That(ado.PayloadReads.ConvertAll(read => read.Ids.Count), Has.All.LessThanOrEqualTo(200));
            }
        }

        [Test]
        public async Task SweepFeaturesForPortfolio_AsksTheSameQuestionTheWholeFeatureDownloadAsks()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheOnlyItem);

            await subject.GetFeaturesForProject(portfolio, CancellationToken.None);
            var whatTheDownloadAsked = ado.WiqlQueries[^1];
            ado.WiqlQueries.Clear();

            await subject.SweepFeaturesForPortfolio(portfolio, CancellationToken.None);

            Assert.That(ado.WiqlQueries, Has.One.EqualTo(whatTheDownloadAsked),
                "The portfolio half is a second implementation of the same contract, and the orphaned-Feature "
                + "cleanup deletes whatever the two queries disagree about.");
        }

        [Test]
        public async Task SweepParentFeatures_AsksForTheSameKeysTheParentDetailFetchAsksFor()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheItemThatMoved, 3);
            var parentIds = TheTwoIdsAskedFor.Select(id => $"{id}").ToList();

            await subject.GetParentFeaturesDetails(portfolio, parentIds, CancellationToken.None);
            var whatTheDetailFetchAsked = ado.WiqlQueries[^1];
            ado.WiqlQueries.Clear();

            await subject.SweepParentFeatures(portfolio, parentIds, CancellationToken.None);

            Assert.That(ado.WiqlQueries, Has.One.EqualTo(whatTheDetailFetchAsked),
                "A stored parent key the sweep does not answer for is downloaded rather than removed, so a "
                + "parent sweep asking a narrower question quietly downloads every parent every cycle.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_ByReferenceId_DownloadsExactlyTheIdsItWasAskedFor()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheItemThatMoved, TheItemThatDidNot, 3);

            var workItems = await subject.GetWorkItemsForTeam(team, TheTwoIdsAskedFor.Select(id => $"{id}").ToList(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems.Select(workItem => workItem.ReferenceId), Is.EquivalentTo(TheTwoReferenceIdsAskedFor),
                    "positive control: the canned answer was read, and phase two returned what it was asked for.");
                Assert.That(ado.EveryIdRead, Is.EquivalentTo(TheTwoIdsAskedFor),
                    "Phase two downloads what moved and nothing else. Reading the whole query again is today's "
                    + "cost with an extra sweep on top.");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_ByReferenceId_DoesNotReApplyTheTeamsOwnFilter()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheItemThatMoved);
            team.DoneItemsCutoffDays = 1;

            await subject.GetWorkItemsForTeam(team, [$"{TheItemThatMoved}"], CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ado.WiqlQueries, Has.None.Contains(AzureDevOpsFieldNames.ClosedDate),
                    "The sweep already vouched for this id. Re-applying the cutoff lets the tracker drop an item "
                    + "the sweep reported as moved, and 'stored minus swept' then deletes it.");
                Assert.That(ado.WiqlQueries, Has.None.Contains(TheTeamsFilter),
                    "Same for the team's own filter: what phase one enumerated, phase two may only look up.");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_ByReferenceId_ReadsRevisionsOnlyForTheIdsItWasAskedFor()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheItemThatMoved, TheItemThatDidNot);

            await subject.GetWorkItemsForTeam(team, [$"{TheItemThatMoved}"], CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ado.RevisionReads, Is.Not.Empty,
                    "A phase two that reads no revisions at all satisfies 'only for the ids it was asked for' "
                    + "while dropping every transition. The count itself is not pinned: an item's revisions are "
                    + "read once for its transitions and again per state boundary its category needs.");
                Assert.That(ado.RevisionReads, Has.All.EqualTo(TheItemThatMoved),
                    "This is the round trip the slice exists to stop paying for quiet items - the one cost that "
                    + "scales with the board rather than with what changed.");
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_ByReferenceId_StillRebuildsTheTransitionsItWasFetchedFor()
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds(TheItemThatMoved);

            var workItem = (await subject.GetWorkItemsForTeam(team, [$"{TheItemThatMoved}"], CancellationToken.None)).Single();

            Assert.That(workItem.SyncedTransitions, Is.Not.Empty,
                "A phase two that skips the revision read is fast and silently wrong: time-in-state, aging pace "
                + "and blocked history are all built on transitions, and none of them complains when they thin out.");
        }

        [Test]
        public async Task GetFeaturesForProject_ByReferenceId_DownloadsExactlyTheIdsItWasAskedFor()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheItemThatMoved, TheItemThatDidNot, 3);

            var features = await subject.GetFeaturesForProject(portfolio, TheTwoIdsAskedFor.Select(id => $"{id}").ToList(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features.ConvertAll(feature => feature.ReferenceId), Is.EquivalentTo(TheTwoReferenceIdsAskedFor),
                    "positive control: the canned answer was read, and the portfolio half returned what it was asked for.");
                Assert.That(ado.EveryIdRead, Is.EquivalentTo(TheTwoIdsAskedFor));
            }
        }

        /// <summary>
        /// An owner can reach the settings API with no states mapped, and the clause built from them is then
        /// built from nothing. Emitting the bracket pair anyway writes a WIQL Azure DevOps cannot parse, and
        /// an owner whose refresh fails outright records nothing at all. Asking for the absent bracket pair,
        /// rather than for the clause that replaces it, is what keeps this from passing on a query that
        /// happens to contain the right substring somewhere else.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_NoStatesMapped_LeavesTheStateClauseOutRatherThanEmpty()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            team.ToDoStates.Clear();
            team.DoingStates.Clear();
            team.DoneStates.Clear();

            await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ado.WiqlQueries, Has.None.Contains("()"));
                Assert.That(ado.WiqlQueries, Has.One.Contains($"AND ([{AzureDevOpsFieldNames.WorkItemType}] = 'User Story')"),
                    "positive control: a change that dropped every clause would satisfy the assertion above "
                    + "while asking Azure DevOps for the whole organisation.");
            }
        }

        [Test]
        public async Task ValidateTeamSettings_NoWorkItemTypesMapped_LeavesTheTypeClauseOutRatherThanEmpty()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            team.WorkItemTypes.Clear();

            await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ado.WiqlQueries, Has.None.Contains("()"));
                Assert.That(ado.WiqlQueries, Has.One.Contains($"AND ([{AzureDevOpsFieldNames.State}] = 'New' OR [{AzureDevOpsFieldNames.State}] = 'Active' OR [{AzureDevOpsFieldNames.State}] = 'Closed')"),
                    "positive control: a change that dropped every clause would satisfy the assertion above "
                    + "while asking Azure DevOps for the whole organisation.");
            }
        }

        /// <summary>
        /// Nothing configured leaves nothing to select on, and every part of the WHERE clause is then built
        /// from nothing. What reaches Azure DevOps is a WHERE with no condition under it, which it refuses -
        /// so the refusal is only ever as clear as whatever the tracker happens to say about the syntax.
        /// Saying it here instead names the configuration that caused it.
        ///
        /// A query typed as nothing but spaces is that same configuration with something in the box. It
        /// narrows no more than an empty one does, so it has to be refused rather than sent.
        /// </summary>
        [TestCase("")]
        [TestCase("   ")]
        public async Task ValidateTeamSettings_NothingIsConfiguredAtAll_RefusesRatherThanAskingAzureDevOpsForEverything(string queryOfItsOwn)
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            NarrowOnNothing(team);
            team.DataRetrievalValue = queryOfItsOwn;

            var verdict = await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ado.WiqlQueries, Is.Empty,
                    "A query that selects on nothing must not be issued at all - whatever Azure DevOps "
                    + "answers it with is a number of items the configuration never asked for.");
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Message, Does.Contain("work item type").IgnoreCase.And.Contains("state").IgnoreCase,
                    "The refusal has to name what is missing, or it reads as a broken connection and the "
                    + "administrator goes looking at the wrong screen.");
                Assert.That(verdict.FieldName, Is.Not.Null.And.Not.Empty,
                    "The settings screen highlights the input the verdict names; without one the message is "
                    + "shown against nothing.");
            }
        }

        /// <summary>
        /// The refresh path has no verdict to return, so the refusal has to leave as an exception. Answering
        /// a refresh with an empty result would be read as "the team has no work left" and remove every
        /// record it has.
        /// </summary>
        [Test]
        public void GetWorkItemsForTeam_NothingIsConfiguredAtAll_RefusesRatherThanFetchingNothing()
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds(TheOnlyItem);
            NarrowOnNothing(team);

            Assert.That(async () => await subject.GetWorkItemsForTeam(team, CancellationToken.None),
                Throws.InstanceOf<AzureDevOpsReadException>());
        }

        /// <summary>
        /// The refusal fires only when all three of the things a configuration can narrow on are missing.
        /// One of them on its own is an ordinary, valid team - a team scoped by a query of its own and
        /// nothing else, or one scoped by its types alone - and refusing any of those stops a working
        /// installation from refreshing at all. These three say where the boundary is, which a test that
        /// only ever configures nothing cannot: every such test passes just as well against a guard that
        /// refuses whenever any one thing is missing.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_OnlyAQueryOfItsOwn_AsksItRatherThanRefusing()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            NarrowOnNothing(team);
            team.DataRetrievalValue = TheTeamsFilter;

            var verdict = await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True, ARefusalHereStopsAWorkingTeam);
                Assert.That(ado.WiqlQueries, Has.One.Contains($"WHERE ({TheTeamsFilter})"),
                    "positive control: the operator's query has to be what the team is scoped by, or the "
                    + "refusal was avoided by asking Azure DevOps for the whole organisation instead.");
            }
        }

        [Test]
        public async Task ValidateTeamSettings_OnlyWorkItemTypesMapped_AsksForThemRatherThanRefusing()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            NarrowOnNothing(team);
            team.WorkItemTypes.Add("User Story");

            var verdict = await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True, ARefusalHereStopsAWorkingTeam);
                Assert.That(ado.WiqlQueries, Has.One.Contains($"WHERE ([{AzureDevOpsFieldNames.WorkItemType}] = 'User Story')"),
                    "positive control: the type clause has to survive becoming the only one.");
            }
        }

        [Test]
        public async Task ValidateTeamSettings_OnlyStatesMapped_AsksForThemRatherThanRefusing()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            NarrowOnNothing(team);
            team.DoingStates.Add("Active");

            var verdict = await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True, ARefusalHereStopsAWorkingTeam);
                Assert.That(ado.WiqlQueries, Has.One.Contains($"WHERE ([{AzureDevOpsFieldNames.State}] = 'Active')"),
                    "positive control: the state clause has to survive becoming the only one.");
            }
        }

        /// <summary>
        /// The refusal is the whole of what an administrator gets: no query was sent, so there is no tracker
        /// answer to fall back on. Each phrase below is the only place its sentence says what it says, so
        /// emptying any one of them fails here on its own rather than being covered by the rest.
        /// </summary>
        [TestCase("selects no work item types")]
        [TestCase("nothing for Lighthouse to ask Azure DevOps for")]
        [TestCase("map at least one state")]
        public async Task ValidateTeamSettings_NothingIsConfiguredAtAll_SaysWhatIsMissingAndWhatToDoAboutIt(string phrase)
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds(TheOnlyItem);
            NarrowOnNothing(team);

            var verdict = await subject.ValidateTeamSettings(team);

            Assert.That(verdict.Message, Does.Contain(phrase));
        }

        /// <summary>
        /// The code is what the settings screen matches on to decide which input to highlight, and the
        /// technical detail is what an operator quotes into a support conversation. Neither is in the
        /// sentence the user reads, so neither is pinned by the phrases above.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_NothingIsConfiguredAtAll_NamesTheRefusalAndSaysNoQueryWasSent()
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds(TheOnlyItem);
            NarrowOnNothing(team);

            var verdict = await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("nothing_to_query"));
                Assert.That(verdict.TechnicalDetails, Does.Contain("empty WHERE clause"));
            }
        }

        /// <summary>
        /// An owner can have types and states mapped and still carry no query of its own - nothing asks for
        /// one, and the settings API will save it that way. The operator's query used to be wrapped in
        /// brackets whether or not there was anything to wrap, so that owner's every refresh failed on a
        /// syntax error about a query nobody wrote. Dropping the brackets leaves whatever follows starting
        /// with AND, which is no more valid than the empty pair was, so the leading conjunction has to come
        /// off with them.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_NoQueryOfItsOwn_LeavesTheBracketPairOutRatherThanEmptyingIt()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            team.DataRetrievalValue = string.Empty;

            await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ado.WiqlQueries, Has.None.Contains("()"));
                Assert.That(ado.WiqlQueries, Has.None.Contains("WHERE AND"),
                    "Dropping the empty pair and leaving the conjunction behind trades one invalid WIQL for "
                    + "another, and the owner's refresh fails exactly as it did before.");
                Assert.That(ado.WiqlQueries, Has.One.Contains($"WHERE ([{AzureDevOpsFieldNames.WorkItemType}] = 'User Story')"),
                    "positive control: the type clause has to survive becoming the first one, or the fix has "
                    + "widened the query to the whole organisation instead of narrowing it.");
                Assert.That(ado.WiqlQueries, Has.One.Contains($"AND ([{AzureDevOpsFieldNames.State}] = 'New' OR [{AzureDevOpsFieldNames.State}] = 'Active' OR [{AzureDevOpsFieldNames.State}] = 'Closed')"),
                    "positive control: the clauses after the first keep their conjunctions.");
            }
        }

        /// <summary>
        /// The whole WIQL, spacing included, for the configuration everyone actually has. An owner with a
        /// query of its own has to come out of this byte for byte unchanged - the sweep and the download
        /// build their queries here, and removal is stored minus swept, so the two drifting apart by a
        /// character reports work items as removed that were only ever described differently. The cutoff is
        /// off because its clause carries today's date, and a query that changes daily cannot be pinned.
        /// </summary>
        [Test]
        public async Task ValidateTeamSettings_AQueryOfItsOwn_StillAssemblesExactlyTheWiqlItAlwaysDid()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            team.DoneItemsCutoffDays = 0;

            await subject.ValidateTeamSettings(team);

            Assert.That(ado.WiqlQueries, Has.One.EqualTo(
                $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.Title}], "
                + $"[{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}] FROM WorkItems "
                + $"WHERE ({TheTeamsFilter}) AND ([{AzureDevOpsFieldNames.WorkItemType}] = 'User Story')  "
                + $"AND ([{AzureDevOpsFieldNames.State}] = 'New' OR [{AzureDevOpsFieldNames.State}] = 'Active' "
                + $"OR [{AzureDevOpsFieldNames.State}] = 'Closed')  "));
        }

        /// <summary>
        /// Strips the team back to what the settings API will happily save and what a restored database can
        /// carry: no types, no states, no query of its own - nothing at all to select on.
        /// </summary>
        private static void NarrowOnNothing(Team team)
        {
            team.WorkItemTypes.Clear();
            team.ToDoStates.Clear();
            team.DoingStates.Clear();
            team.DoneStates.Clear();
            team.DataRetrievalValue = string.Empty;
        }
    }
}
