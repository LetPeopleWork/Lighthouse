using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Acceptance scenarios for story 6053 - a day worked out afterwards is only worked out from work the
    /// team still keeps. Every refresh deletes finished work older than the team keeps it for, so a day
    /// whose reading reaches back past that edge would be averaged over a stretch that is partly gone,
    /// and would be written once and never corrected.
    ///
    /// Each scenario pairs the refusal with the days just inside the edge, so a fill that refused
    /// everything cannot pass it.
    ///
    /// Driving port: the shipped over-time read endpoints, team routes.
    ///
    /// Step definitions live in Slice10OnlyWhatTheTeamStillKeepsSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-10")]
    public partial class Slice10OnlyWhatTheTeamStillKeepsTest
    {
        // @driving_port @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Cycle_time_is_filled_in_only_for_days_whose_whole_window_the_team_still_keeps()
        {
            var teamId = GivenATeamStillBeingRefreshedThatKeepsFinishedWorkFor(DaysTheTeamKeepsFinishedWorkFor);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheFlowCoachOpensTheThirtyDayCycleTimeTrend(teamId, TheOldestDayTheTeamStillKeeps, TodayDay.AddDays(-30));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenNoCycleTimeDayReadsFurtherBackThanTheTeamKeepsWork(teamId);
                ThenEveryCycleTimeDayReadingOnlyKeptWorkWasFilledIn(teamId, TodayDay.AddDays(-30));
            }
        }

        // @driving_port @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Limits_are_filled_in_only_for_days_whose_whole_stretch_the_team_still_keeps()
        {
            var teamId = GivenATeamStillBeingRefreshedThatKeepsFinishedWorkFor(DaysTheTeamKeepsFinishedWorkFor);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheDeliveryLeadOpensTheThroughputLimits(teamId, TheOldestDayTheTeamStillKeeps, TodayDay.AddDays(-30));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenNoLimitDayReadsFurtherBackThanTheTeamKeepsWork(teamId);
                ThenEveryLimitDayReadingOnlyKeptWorkWasFilledIn(teamId, TodayDay.AddDays(-30));
            }
        }

        /// <summary>
        /// A pinned stretch does not follow the day, so it can fall behind what the team keeps while every
        /// day's own window is still inside it. Judged as of each day being worked out the stretch is in
        /// reach, which is why this needs its own scenario: only judging it against the team's last refresh
        /// sees that the work it was drawn from has since been deleted.
        ///
        /// The second team is the same team with its stretch pinned a little later, inside what it keeps,
        /// so the same days do get limits there - a fill that wrote no limits for anyone cannot pass this.
        /// </summary>
        // @driving_port @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Limits_drawn_from_a_pinned_stretch_the_team_no_longer_keeps_are_not_filled_in()
        {
            var teamId = GivenATeamStillBeingRefreshedThatKeepsFinishedWorkFor(DaysTheTeamKeepsFinishedWorkFor);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);
            GivenTheTeamPinnedTheStretchItsLimitsAreDrawnFrom(teamId, TheOldestDayTheTeamStillKeeps.AddDays(-10), TheOldestDayTheTeamStillKeeps.AddDays(20));
            GivenThatStretchStillReadsAsOfTheOldestDayOpened(teamId, TodayDay.AddDays(-40));
            var teamThatPinnedInsideId = GivenATeamStillBeingRefreshedThatKeepsFinishedWorkFor(DaysTheTeamKeepsFinishedWorkFor);
            GivenTheTeamFinishedOneItemADayFrom(teamThatPinnedInsideId, TodayDay.AddDays(-200), TodayDay);
            GivenTheTeamPinnedTheStretchItsLimitsAreDrawnFrom(teamThatPinnedInsideId, TheOldestDayTheTeamStillKeeps.AddDays(5), TheOldestDayTheTeamStillKeeps.AddDays(35));

            await WhenTheDeliveryLeadOpensTheThroughputLimits(teamId, TodayDay.AddDays(-40), TodayDay.AddDays(-30));
            await WhenTheDeliveryLeadOpensTheThroughputLimits(teamThatPinnedInsideId, TodayDay.AddDays(-40), TodayDay.AddDays(-30));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenNoLimitDayWasFilledInBetween(teamId, TodayDay.AddDays(-40), TodayDay.AddDays(-30));
                ThenEveryLimitDayWasFilledInBetween(teamThatPinnedInsideId, TodayDay.AddDays(-40), TodayDay.AddDays(-30));
            }
        }

        /// <summary>
        /// Age is read as of the day alone, so it has no window behind it - but a day older than the
        /// edge is still one whose finished work is gone, and an item that was in progress on it and
        /// finished before the edge is no longer there to be counted.
        /// </summary>
        // @driving_port @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Work_item_age_is_filled_in_from_the_oldest_day_the_team_still_keeps_and_not_before()
        {
            var teamId = GivenATeamStillBeingRefreshedThatKeepsFinishedWorkFor(DaysTheTeamKeepsFinishedWorkFor);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);
            GivenAnItemTheTeamHasHadInProgressSince(teamId, TodayDay.AddDays(-300));

            await WhenTheFlowCoachOpensTheWorkItemAgeTrend(teamId, TheOldestDayTheTeamStillKeeps.AddDays(-20), TheOldestDayTheTeamStillKeeps.AddDays(10));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenNoAgeDayIsOlderThanTheWorkTheTeamKeeps(teamId);
                ThenEveryAgeDayFromTheOldestKeptDayOnWasFilledIn(teamId, TheOldestDayTheTeamStillKeeps.AddDays(10));
            }
        }
    }
}
