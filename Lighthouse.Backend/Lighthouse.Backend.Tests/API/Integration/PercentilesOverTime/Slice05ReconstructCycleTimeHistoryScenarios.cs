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
    /// Step definitions live in Slice05ReconstructCycleTimeHistorySpecifications.cs (same partial class).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-05")]
    public partial class Slice05ReconstructCycleTimeHistoryTest
    {
        /// <summary>
        /// The one scenario in this fixture that runs today. It pins what must survive the story rather
        /// than what the story adds: a chart load answers with what is there, and the filling in happens
        /// out of the reader's way.
        /// </summary>
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
        /// Filling in history and a routine refresh can land on the database at the same moment. Neither
        /// may lose its writes to the other, and nothing may end up on the chart twice.
        /// </summary>
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
    }
}
