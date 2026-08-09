using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5687 — Faster Updates), slice 01: every completed update says
    /// what it did, once, and stops burying that line under what it iterated over.
    /// Driving port: the scheduled refresh. US-01, AC-1.1 … AC-1.3 and AC-1.5 … AC-1.9.
    ///
    /// AC-1.4 (a skipped entity says nothing) lives in
    /// <c>Services/Implementation/BackgroundServices/Update/Slice01SkippedEntityLogTest</c> — it is a
    /// promise about the background loop, which the test host does not run.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5687-faster-updates")]
    [Category("slice-01")]
    public partial class Slice01UpdateLogSignalTest
    {
        /// <summary>
        /// DISTILL hands these over failing on their assertions. DELIVER removes one <c>[Ignore]</c> at a
        /// time — that is the RED entry gate for each cycle.
        /// </summary>
        private const string RedUntilDelivered = "DISTILL RED (Epic 5687 slice 01) — un-ignore one at a time in DELIVER";

        // @walking_skeleton @driving_port @real-io @AC-1.1 @AC-1.3
        [Test]
        public async Task A_completed_team_update_says_what_it_did()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerHolds(2);

            await WhenTheScheduledRefreshRuns(team);

            ThenTheOperatorSeesExactlyOneUpdateSummary();
            ThenThatSummaryNamesTheTeam(team);
            ThenThatSummaryReportsAFullUpdate();
            ThenThatSummaryReportsRecordsSeenAndFetched(scanned: 2, fetched: 2);
            ThenThatSummaryReportsHowLongItTook();
            ThenThatSummaryReportsItSucceeded();
        }

        // @driving_port @real-io @AC-1.2 — the same line shape from the other half of the cycle.
        [Test]
        [Ignore(RedUntilDelivered)]
        public async Task A_completed_portfolio_update_says_the_same_thing_in_the_same_shape()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            GivenTheTrackerHoldsFeatures(3);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheOperatorSeesExactlyOneUpdateSummary();
            ThenThatSummaryNamesThePortfolio(portfolio);
            ThenThatSummaryReportsAFullUpdate();
            ThenThatSummaryReportsRecordsSeenAndFetched(scanned: 3, fetched: 3);
            ThenThatSummaryReportsHowLongItTook();
            ThenThatSummaryReportsItSucceeded();
        }

        // @driving_port @AC-1.7 @KPI-5 — the criterion that makes the summary readable at all.
        [Test]
        [Ignore(RedUntilDelivered)]
        public async Task A_team_update_writes_no_more_than_two_lines_the_operator_has_to_read()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerHolds(25);

            await WhenTheScheduledRefreshRuns(team);

            ThenTheOperatorHadAtMostTwoLinesToRead();
        }

        // @driving_port @AC-1.7 @KPI-5 — and the same budget on the portfolio half.
        [Test]
        [Ignore(RedUntilDelivered)]
        public async Task A_portfolio_update_writes_no_more_than_two_lines_the_operator_has_to_read()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            GivenTheTrackerHoldsFeatures(25);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheOperatorHadAtMostTwoLinesToRead();
        }

        // @driving_port @AC-1.5 — three copies of the same announcement is the loudest single offender.
        [Test]
        [Ignore(RedUntilDelivered)]
        public async Task A_team_update_announces_itself_once()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerHolds(2);

            await WhenTheScheduledRefreshRuns(team);

            ThenTheTeamIsAnnouncedAtMostOnce(team);
        }

        // @driving_port @AC-1.6 — demoted, never deleted: the per-record stream stays available at Debug.
        // Driven from the portfolio half, which is where the per-Feature narration is emitted.
        [Test]
        [Ignore(RedUntilDelivered)]
        public async Task An_update_keeps_its_per_record_chatter_out_of_the_operators_log()
        {
            var portfolio = GivenAPortfolioThatIsRefreshedOnSchedule();
            GivenTheTrackerHoldsFeatures(5);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenNoPerRecordLineIsOperatorVisible();
            ThenThePerRecordDetailIsStillAvailableToWhoeverAsksForIt();
        }

        // @driving_port @AC-1.8 — the numbers the summary reports are the numbers that get persisted.
        [Test]
        public async Task A_completed_team_update_records_the_mode_and_both_counts()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerHolds(2);

            await WhenTheScheduledRefreshRuns(team);

            ThenTheRecordedUpdateReportsAFullUpdateOf(team, scanned: 2, fetched: 2);
            ThenTheRecordedUpdateStillCarriesEverythingItAlwaysDid(team, itemCount: 2);
        }

        // @error @driving_port @AC-1.9 — a failed update is the one an operator most needs to read.
        [Test]
        public async Task An_update_that_failed_still_says_what_it_did()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerIsUnreachable();

            await WhenTheScheduledRefreshRuns(team);

            ThenTheOperatorSeesExactlyOneUpdateSummary();
            ThenThatSummaryReportsItFailed();
            ThenTheFailureIsReportedWithItsCause();
        }
    }
}
