using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// The cycle-time trend a team's data already supports, read through the percentiles-over-time
    /// endpoint. Opening the chart is what starts the filling in; nothing here starts a filler or a
    /// reconciler by hand. These rows keep the wiring: the read route, the read-fill-read loop, and a
    /// refresh landing mid-fill. The fill's rules themselves are tested on the classes that compute them.
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
        /// A chart load answers with what is there, and the filling in happens out of the reader's way.
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
