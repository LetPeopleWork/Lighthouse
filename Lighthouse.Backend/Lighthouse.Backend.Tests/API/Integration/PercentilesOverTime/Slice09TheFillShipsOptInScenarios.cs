using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Acceptance scenarios for what the history fill leaves alone and what it costs. The fill runs on
    /// every instance and reads no setting, so the daily recording is still the daily recording - a
    /// refresh writes today and nothing else, and leaves a quiet day blank - and opening a chart never
    /// looks a behaviour setting up.
    ///
    /// Numbered Slice09 because the directory numbers fixtures, and Slice05-Slice08 were already taken.
    ///
    /// Step definitions live in Slice09TheFillShipsOptInSpecifications.cs (same partial class).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-09")]
    public partial class Slice09TheFillShipsOptInTest
    {
        /// <summary>
        /// The daily recording is not the fill. Today is written on a refresh exactly as before, and a
        /// refresh writes nothing but today.
        /// </summary>
        // @us-05 @not-gated @regression @real-io @contract-shape:bounded-change
        [Test]
        public async Task The_daily_recording_still_writes_today()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);

            await WhenTheTeamsRefreshRuns(teamId);

            using (Assert.EnterMultipleScope())
            {
                ThenTodayWasRecordedOverEveryCycleTimeLookBack(teamId);
                ThenNothingButTodayWasWritten(teamId);
            }
        }

        /// <summary>
        /// The recorder's refusal to write four zeros for a quiet day shipped for everyone. The same
        /// refresh writes the team's age reading for today, which is what shows the recording genuinely
        /// ran for this team and chose to leave cycle time blank.
        /// </summary>
        // @us-05 @not-gated @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_quiet_day_is_still_left_blank_by_the_daily_recording()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay.AddDays(-150));
            GivenTheTeamHasOneItemStillInProgressSince(teamId, TodayDay.AddDays(-10));

            await WhenTheTeamsRefreshRuns(teamId);

            using (Assert.EnterMultipleScope())
            {
                ThenTheAgeReadingWasRecordedForToday(teamId);
                ThenNoCycleTimeReadingWasRecordedForToday(teamId);
            }
        }

        /// <summary>
        /// Nothing about filling in a chart depends on a behaviour setting any more, so neither the
        /// settled, everyday chart that is missing nothing nor one that finds days to ask for looks one
        /// up. A lookup coming back here is a gate coming back.
        /// </summary>
        // @us-05 @read-cost @real-io @contract-shape:pure-function
        [Test]
        public async Task Opening_a_chart_never_looks_up_a_behaviour_setting()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenEveryDayWasAlreadyRecordedFrom(teamId, TodayDay.AddDays(-5), TodayDay);
            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-2), TodayDay);

            var missingNothing = await WhatOpeningTheCycleTimeTrendAsksOfTheStore(teamId, TodayDay.AddDays(-5), TodayDay);
            var missingDays = await WhatOpeningTheCycleTimeTrendAsksOfTheStore(teamId, TodayDay.AddDays(-10), TodayDay);

            using (Assert.EnterMultipleScope())
            {
                ThenNoBehaviourSettingWasLookedUp(missingNothing);
                ThenNoBehaviourSettingWasLookedUp(missingDays);
            }
        }
    }
}
