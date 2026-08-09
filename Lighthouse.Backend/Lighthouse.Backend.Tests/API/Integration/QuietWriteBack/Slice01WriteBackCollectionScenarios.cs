using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.QuietWriteBack
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5500 - Quiet write-back), slice 01: one refresh, one
    /// conversation with the tracker. Walking skeleton: a scheduled portfolio refresh resolves every
    /// mapped value it can and reaches the tracker exactly once.
    /// Driving port: the scheduled refresh. US-04, AC-04.1 ... AC-04.7.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5500-quiet-writeback")]
    [Category("slice-01")]
    public partial class Slice01WriteBackCollectionTest
    {
        private const string RedScaffold = "RED - Epic 5500 slice 01 (write-back collection seam) not implemented";

        // @walking_skeleton @driving_port @real-io @AC-04.1a
        // Today this refresh talks to the tracker twice - once after features, once after forecasts.
        [Test]
        [Ignore(RedScaffold)]
        public async Task One_scheduled_refresh_of_a_portfolio_reaches_the_tracker_once()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheTrackerWasWrittenTo(times: 1);
            ThenThatWriteCarriedBothTheSizeAndTheForecastFor("PROJ-1");
        }

        // @driving_port @AC-04.1b - the residue ADR-144 closes by making the stored copy true
        [Test]
        [Ignore(RedScaffold)]
        public async Task A_value_written_in_one_execution_is_not_written_again_by_the_next()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);

            await WhenTheScheduledRefreshRuns(portfolio);
            await WhenTheForecastRefreshRunsOnItsOwn(portfolio);

            ThenTheTrackerWasWrittenTo(times: 1);
        }

        // @error @driving_port @AC-04.3 - the D8 no-op guard, preserved
        [Test]
        [Ignore(RedScaffold)]
        public async Task A_refresh_in_which_nothing_changed_never_reaches_the_tracker()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredValuesAreAlreadyCorrect(portfolio, "PROJ-1", size: 5, workingDaysToCompletion: 10);
            GivenTheForecastRunCompletesIn(10);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheTrackerWasNeverWrittenTo();
        }

        // @driving_port @AC-04.4 - the seam is above the connector, so it is connector-agnostic by
        // construction. That the Azure DevOps adapter still asks for silence is asserted where the flag
        // is observable: AzureDevOpsWriteBackTest.
        [Test]
        [Ignore(RedScaffold)]
        public async Task An_azure_devops_portfolio_flushes_through_the_same_seam()
        {
            var portfolio = GivenAnAzureDevOpsPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "42", size: 5);
            GivenTheForecastRunCompletesIn(10);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheTrackerWasWrittenTo(times: 1);
            ThenThatWriteCarriedBothTheSizeAndTheForecastFor("42");
        }

        // @error @driving_port @parity @AC-04.6 - parity with today's swallow-and-log. Green from
        // DISTILL on purpose: the AC asks for behaviour that already holds and must survive the seam.
        [Test]
        public async Task A_flush_that_throws_leaves_the_refresh_round_finished()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);
            GivenTheTrackerIsUnreachable();

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheRefreshRoundWasRecordedAsComplete(portfolio);
        }

        // @driving_port @parity @AC-04.7 - a team refresh already reaches the tracker once and must go
        // on doing so through the collector. That it goes *through* the collector is asserted where it
        // is observable: WriteBackCollectorTest plus the single flush site in UpdateServiceBase.
        [Test]
        public async Task A_team_refresh_takes_part_in_the_same_collection_and_flush()
        {
            var team = GivenATeamWhoseItemAgeIsWrittenBack();
            GivenAWorkItemWhoseStoredAgeIsOutOfDate(team, "STORY-1", ageInDays: 4);

            await WhenTheScheduledTeamRefreshRuns(team);

            ThenTheTrackerWasWrittenTo(times: 1);
        }

        // @error @driving_port - the first bound on the D11 exception: the local copy may lag reality,
        // it may never lead it.
        [Test]
        [Ignore(RedScaffold)]
        public async Task A_write_the_tracker_refused_never_updates_the_local_copy()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);
            GivenTheTrackerRefusesTheSizeField();

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheStoredSizeOfIsStillTheOldOne("PROJ-1");
            ThenTheStoredForecastOfWasBroughtUpToDate("PROJ-1");
        }

        // @driving_port - the third bound: inbound sync still wins, so an apparent success that did not
        // take effect self-corrects within one cycle.
        [Test]
        [Ignore(RedScaffold)]
        public async Task The_next_inbound_sync_still_overrides_a_locally_persisted_value()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);
            await WhenTheScheduledRefreshRuns(portfolio);
            ThenTheStoredSizeOfWasBroughtUpToDate("PROJ-1", "5");

            WhenTheTrackerReportsADifferentSizeOnTheNextSync("PROJ-1", "99");

            ThenTheStoredSizeOfIs("PROJ-1", "99");
        }

        // @driving_port @parity - D11 stands: the exception must not become jitter damping. Green from
        // DISTILL because today nothing damps anything; it is the guard that catches D-A7-R being
        // widened from "persist what was written" into "do not write small movements".
        [Test]
        public async Task A_forecast_that_genuinely_moved_is_still_written()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);
            await WhenTheScheduledRefreshRuns(portfolio);

            GivenTheForecastRunCompletesIn(11);
            await WhenTheForecastRefreshRunsOnItsOwn(portfolio);

            ThenTheLastWriteCarriedTheForecastFor("PROJ-1");
        }

        // @driving_port - ADR-144's first decision, asserted from the outside: resolving is not writing.
        [Test]
        [Ignore(RedScaffold)]
        public async Task Resolving_a_portfolios_write_back_plan_never_reaches_the_tracker()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);

            await WhenTheWriteBackPlanForThePortfolioIsResolved(portfolio);

            ThenTheTrackerWasNeverWrittenTo();
        }
    }
}
