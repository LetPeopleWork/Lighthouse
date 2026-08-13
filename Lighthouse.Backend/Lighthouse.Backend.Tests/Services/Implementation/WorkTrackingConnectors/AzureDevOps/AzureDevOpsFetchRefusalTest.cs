using static Lighthouse.Backend.Tests.TestHelpers.AzureDevOpsOrganisation;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// The whole-query fetch, when the tracker will not answer it.
    ///
    /// Removal is a set difference against what the query returned, so a fetch that answers with no records
    /// deletes every record the team or portfolio holds. That is the right answer when the query genuinely
    /// matches nothing and a catastrophic one when the fetch merely failed - and the two are the same value.
    /// So the fetch has to refuse: the cycle fails, one team's update is skipped, and the records stay.
    ///
    /// What the failed cycle costs is one team, not the refresh - every entity's update is wrapped
    /// individually - and the fetch runs before anything is written, so a refusal leaves the stored data
    /// untouched rather than half-updated.
    /// </summary>
    [TestFixture]
    public class AzureDevOpsFetchRefusalTest
    {
        private const int TheOnlyItem = 1;

        [Test]
        public void GetWorkItemsForTeam_RefusesWhenTheTrackerWillNotRunTheQuery()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            Assert.That(async () => await subject.GetWorkItemsForTeam(team),
                Throws.Exception,
                "An expired token or a timeout is not the tracker saying the query matches nothing. Answering "
                + "with no records hands removal an empty query, which deletes every Work Item the team has.");
        }

        [Test]
        public void GetWorkItemsForTeam_RefusesWhenTheFieldLookupFails()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectTheFieldLookup = true;

            Assert.That(async () => await subject.GetWorkItemsForTeam(team),
                Throws.Exception,
                "The field lookup runs after the query already succeeded and before any payload is read, so "
                + "its failure is invisible to anything watching the query - and empties the team just the same.");
        }

        [Test]
        public void GetWorkItemsForTeam_RefusesWhenAPayloadBatchIsRejected()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectPayloadReads = true;

            Assert.That(async () => await subject.GetWorkItemsForTeam(team),
                Throws.Exception,
                "Azure DevOps fails a whole batch over one id deleted since the query ran. Reading that as an "
                + "empty query deletes the other one hundred and ninety-nine records in the batch too.");
        }

        [Test]
        public void GetWorkItemsForTeam_ByReferenceId_RefusesWhenTheFieldLookupFails()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectTheFieldLookup = true;

            Assert.That(async () => await subject.GetWorkItemsForTeam(team, [$"{TheOnlyItem}"]),
                Throws.Exception,
                "The keyed fetch is where the cheap refresh sends its traffic. A failure it answers with no "
                + "records reports the moved items as gone.");
        }

        [Test]
        public void GetFeaturesForProject_RefusesWhenTheTrackerWillNotRunTheQuery()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            Assert.That(async () => await subject.GetFeaturesForProject(portfolio),
                Throws.Exception,
                "On the portfolio half an empty answer strips every Feature's portfolio claim, and the "
                + "orphaned-Feature cleanup then deletes outright whatever no portfolio still claims.");
        }

        [Test]
        public void GetParentFeaturesDetails_RefusesWhenTheTrackerWillNotRunTheQuery()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            Assert.That(async () => await subject.GetParentFeaturesDetails(portfolio, [$"{TheOnlyItem}"]),
                Throws.Exception,
                "Parent Features are fetched by the same swallowing path, and a parent that comes back empty "
                + "is one every child Feature stops being able to name.");
        }

        [Test]
        public async Task GetWorkItemsForTeam_StillAnswersWithNoRecordsWhenTheQueryGenuinelyMatchesNothing()
        {
            var (subject, team, _) = AnAzureDevOpsThatHolds();

            var workItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(workItems, Is.Empty,
                "The distinction is the whole point. A team whose query really matches nothing has to keep "
                + "answering with nothing, or removal never runs and departed items live forever.");
        }

        [Test]
        public async Task ValidateTeamSettings_ReportsAFetchItCouldNotMakeAsAFailureRatherThanAsAnEmptyBoard()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            var result = await subject.ValidateTeamSettings(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("validation_failed"),
                    "Telling an operator whose token just expired to check their query, work item types and "
                    + "mapped states sends them to rewrite a configuration that was never wrong.");
            }
        }

        [Test]
        public async Task ValidatePortfolioSettings_ReportsAFetchItCouldNotMakeAsAFailureRatherThanAsAnEmptyBoard()
        {
            var (subject, portfolio, ado) = AnAzureDevOpsPortfolioThatHolds(TheOnlyItem);
            ado.RejectTheQuery = true;

            var result = await subject.ValidatePortfolioSettings(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Code, Is.EqualTo("validation_failed"),
                    "Same on the portfolio half: 'no features found' names a configuration problem, and a "
                    + "failed round trip is not one.");
            }
        }
    }
}
