using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DISTILL acceptance scenarios for story 6053, slice 01 - the cycle-time trend a team's data already
    /// supports. A flow coach opens Team - Metrics - Predictability - Percentiles Over Time on an instance
    /// that has been running intermittently, and reads a continuous line across the period under review
    /// instead of the handful of days the recorder happened to catch.
    ///
    /// Driving port: the shipped percentiles-over-time read endpoint. Nothing here reaches for a filler or
    /// a reconciler to start the work - opening the chart is what starts it, which is the whole claim.
    ///
    /// Every scenario but the last is pending: the behaviour does not exist yet. The one that runs pins a
    /// property that must survive this story rather than arrive with it - opening a chart writes nothing
    /// while the reader waits.
    ///
    /// Step definitions live in Slice05ReconstructCycleTimeHistorySpecifications.cs (same partial class).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-05")]
    public partial class Slice05ReconstructCycleTimeHistoryTest
    {
        private const string Pending = "Pending: reconstruction of missing over-time days is not built yet (story 6053, slice 01).";

        // @driving_port @us-01 @real-io @contract-shape:bounded-change
        [Test]
        public async Task The_flow_coach_reads_the_run_of_days_before_the_first_one_that_was_recorded()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            GivenTheDayWasAlreadyRecordedAs(teamId, TodayDay, 2, 2, 3, 4);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTrendCoversEveryDayFrom(teamId, TodayDay.AddDays(-30), TodayDay);
        }

        // @driving_port @us-01 @real-io @contract-shape:bounded-change
        [Test]
        public async Task The_flow_coach_reads_across_the_stretch_the_instance_was_switched_off()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            GivenTheDayWasAlreadyRecordedAs(teamId, TodayDay.AddDays(-30), 1, 1, 2, 2);
            GivenTheDayWasAlreadyRecordedAs(teamId, TodayDay.AddDays(-10), 1, 1, 2, 2);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay.AddDays(-10));
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTrendCoversEveryDayFrom(teamId, TodayDay.AddDays(-30), TodayDay.AddDays(-10));
        }

        // @us-01 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_team_nobody_is_syncing_any_more_gains_no_days_since_it_stopped()
        {
            var lastObservedOn = TodayDay.AddDays(-60);
            var teamId = GivenATeamLastObservedOn(lastObservedOn);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), lastObservedOn);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTrendStopsOn(teamId, lastObservedOn);
        }

        // @us-01 @boundary @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task The_trend_reaches_back_only_as_far_as_the_team_has_finished_anything()
        {
            var earliestFinishedDay = TodayDay.AddDays(-40);
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, earliestFinishedDay, TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-300), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTrendReachesBackNoFurtherThan(teamId, earliestFinishedDay);
        }

        // @us-01 @boundary @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task A_year_wide_range_fills_in_over_several_visits_rather_than_all_at_once()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-200), TodayDay);
            await WhenTheChartHasFinishedFillingIn();
            ThenTheChartGainedAtMostOnePassWorthOfDays(teamId, heldBefore: 0);

            var heldAfterTheFirstVisit = DaysHeldFor(teamId).Count;
            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-200), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenOpeningTheTrendAgainKeepsFillingItIn(teamId, heldAfterTheFirstVisit);
        }

        // @us-01 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_stretch_in_which_the_team_finished_nothing_stays_blank_instead_of_reading_zero()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay.AddDays(-150));

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-60), TodayDay.AddDays(-50));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheDaysAreLeftAbsent(teamId, TodayDay.AddDays(-60), TodayDay.AddDays(-50));
                ThenNoDayReadsAsFourZeroes(teamId);
            }
        }

        /// <summary>
        /// The same rule has to hold on the path that writes today's value, or a day's reading would
        /// depend on which of the two wrote it first - and because an existing day is never rewritten,
        /// the first one to get there wins permanently. This is a change to behaviour that has already
        /// shipped: the recorder used to write four zeros for a quiet day.
        /// </summary>
        // @us-01 @regression @driving_port @real-io @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task A_quiet_day_is_left_blank_by_the_daily_recording_too()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay.AddDays(-150));

            await WhenTheTeamsRefreshRuns(teamId);

            using (Assert.EnterMultipleScope())
            {
                ThenTheDaysAreLeftAbsent(teamId, TodayDay, TodayDay);
                ThenNoDayReadsAsFourZeroes(teamId);
            }
        }

        // @us-01 @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_day_that_was_actually_watched_keeps_the_value_it_was_watched_at()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            var asWatched = GivenTheDayWasAlreadyRecordedAs(teamId, TodayDay.AddDays(-20), 11, 13, 17, 19);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheDayStillReadsExactlyAsRecorded(teamId, asWatched);
        }

        // @us-01 @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Looking_at_the_same_period_twice_costs_nothing_the_second_time()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();
            var afterTheFirstVisit = DaysHeldFor(teamId);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTrendIsUnchangedSince(teamId, afterTheFirstVisit);
        }

        /// <summary>
        /// The standing version of the probe that decided this story was worth building: a day computed
        /// afterwards must equal the day that was watched. If it stops being true, the chart is drawing
        /// two different readings as one line.
        /// </summary>
        // @us-01 @fidelity @real-io @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task A_day_worked_out_afterwards_reads_the_same_as_the_day_that_was_watched()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            var asWatched = await GivenADayTheRecorderGenuinelyWroteAndThenLost(teamId, TodayDay.AddDays(-15));

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheDayCameBackTheSameAsWhenItWasWatched(teamId, asWatched);
        }

        /// <summary>
        /// The one scenario in this fixture that runs today. It pins what must survive the story rather
        /// than what the story adds: a chart load answers with what is there, and the filling in happens
        /// out of the reader's way.
        /// </summary>
        // @driving_port @us-01 @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Opening_the_trend_answers_with_what_is_there_and_writes_nothing_while_the_coach_waits()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            var alreadyWritten = GivenTheDayWasAlreadyRecordedAs(teamId, TodayDay.AddDays(-1), 4, 5, 6, 7);

            var heldBefore = TotalPercentileDaysHeld();
            var response = await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-90), TodayDay);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTrendHeldOnlyTheDaysAlreadyWritten(response, [alreadyWritten.RecordedAt]);
                ThenNothingWasWrittenWhileTheReaderWaited(heldBefore);
            }
        }

        // @us-01 @real-io @contract-shape:bounded-change
        [Test]
        public async Task The_days_the_first_visit_could_not_show_are_there_on_the_next_one()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);

            var firstVisit = await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            ThenTheTrendHeldOnlyTheDaysAlreadyWritten(firstVisit, []);

            await WhenTheChartHasFinishedFillingIn();

            ThenTheTrendCoversEveryDayFrom(teamId, TodayDay.AddDays(-30), TodayDay);
        }

        /// <summary>
        /// Restoring or clearing the database while days are being written would swap the file out from
        /// under an open transaction. The operator is told to wait.
        /// </summary>
        // @us-01 @maintenance @real-io @contract-shape:pure-function
        [Test]
        public async Task An_operator_cannot_start_a_database_restore_while_the_chart_is_filling_in()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-90), TodayDay);

            using (AReconstructionPassHeldInFlight())
            {
                ThenStartingADatabaseRestoreIsRefused();
            }

            ThenStartingADatabaseRestoreIsAllowedAgain();
        }

        /// <summary>
        /// The other half of the same coupling. Teaching only the gate leaves the window between it
        /// checking and the filling starting; the filling has to stand down as well. It can afford to,
        /// because every day it has already written stays written and the next chart load asks again.
        /// </summary>
        // @us-01 @maintenance @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task The_chart_stops_filling_itself_in_while_the_operator_is_restoring_the_database()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            GivenADatabaseRestoreIsAlreadyRunning();

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenNoDayWasAddedToTheTrend(teamId, heldBefore: 0);
        }

        /// <summary>
        /// Two copies of the application serving the same database can both start filling the same day.
        /// The database has the last word, and the scenario has to run against a real one: the in-memory
        /// provider the unit suite uses does not enforce the uniqueness this rests on, so it would pass
        /// against an implementation with no backstop whatsoever.
        /// </summary>
        // @us-01 @concurrency @real-io @sqlite @contract-shape:bounded-change
        [Test]
        public async Task Two_copies_of_the_application_filling_the_same_day_leave_one_point_not_two()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await TwoReconstructionPassesRunAtTheSameInstant(teamId, OwnerType.Team);

            ThenTheDayIsHeldExactlyOnce(teamId, TodayDay.AddDays(-15));
        }

        /// <summary>
        /// A day lost to another copy costs that day, and only that day. The pass is stopped at the
        /// instant it has worked its first day out and is about to write it, another copy records that
        /// same day, and the pass is let go into a refusal it did not see coming - with eighty-nine days
        /// of the window still ahead of it.
        ///
        /// One pass rather than two, and that is the whole reason this scenario exists next to the one
        /// above it. With two copies walking the same window, whatever one abandons the other writes, so
        /// the chart comes out whole whether the refusal was absorbed or took the rest of the walk down
        /// with it. Only a single walk with nobody behind it leaves a hole that can be seen.
        /// </summary>
        // @us-01 @concurrency @real-io @sqlite @contract-shape:bounded-change
        [Test]
        public async Task A_day_another_copy_records_mid_pass_costs_that_day_and_not_the_rest_of_the_window()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);

            var firstDayOfAPassWideWindow = TodayDay.AddDays(1 - ReconstructionCapInDays);
            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, firstDayOfAPassWideWindow, TodayDay);
            var takenByTheOtherCopy = await WhenAnotherCopyRecordsTheDayThisPassIsAboutToWrite(teamId);

            using (Assert.EnterMultipleScope())
            {
                ThenTheRestOfTheWindowWasStillFilledIn(teamId, firstDayOfAPassWideWindow, TodayDay, takenByTheOtherCopy.RecordedAt);
                ThenTheDayStillReadsExactlyAsRecorded(teamId, takenByTheOtherCopy);
            }
        }

        /// <summary>
        /// Filling in history and a routine refresh can land on the database at the same moment. Neither
        /// may lose its writes to the other, and nothing may end up on the chart twice.
        /// </summary>
        // @us-01 @concurrency @real-io @sqlite @contract-shape:bounded-change
        [Test]
        public async Task A_refresh_landing_mid_fill_neither_loses_its_day_nor_duplicates_one()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);

            using (AReconstructionPassHeldInFlight())
            {
                await WhenTheTeamsRefreshRuns(teamId);
            }

            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheTrendCoversEveryDayFrom(teamId, TodayDay.AddDays(-30), TodayDay);
                ThenEveryDayIsHeldExactlyOnceAcross(teamId, TodayDay.AddDays(-30), TodayDay);
            }
        }

        /// <summary>
        /// A demonstration instance carries invented values backdated into the same table. They are not
        /// observations, but they are also not this story's to correct: a day that already carries a
        /// value is left alone, whoever put it there.
        /// </summary>
        // @us-01 @demo @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Backdated_demonstration_values_are_stepped_over_rather_than_corrected()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            var backdated = GivenTheDemoLoaderAlreadyBackdated(teamId, TodayDay.AddDays(-12), 8, 9, 10, 11);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheDayStillReadsExactlyAsRecorded(teamId, backdated);
        }
    }
}
