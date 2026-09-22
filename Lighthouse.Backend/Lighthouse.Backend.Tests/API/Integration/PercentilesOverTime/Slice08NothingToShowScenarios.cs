using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DISTILL acceptance scenarios for story 6053, slice 04 - being told the truth when there is still
    /// nothing to show. After this story an over-time chart can be empty for reasons the current wording
    /// does not name, so each of those states has to be reachable and distinguishable before any words
    /// are chosen for it.
    ///
    /// Driving port: the two shipped over-time read endpoints. These scenarios pin the states; the words
    /// the widget uses for them are asserted in the widget's own tests, written once slices 05-07 settle
    /// which states remain reachable. Naming that split here rather than leaving it implicit - see the
    /// DISTILL section of the feature delta for the three states the widget must tell apart.
    ///
    /// Step definitions live in Slice08NothingToShowSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-08")]
    public partial class Slice08NothingToShowTest
    {
        private const string Pending = "Pending: reconstruction of missing over-time days is not built yet (story 6053, slice 04).";

        /// <summary>
        /// The first genuinely new empty state. The team has plenty of history, but not that far back,
        /// so the period cannot be worked out from anything - which is a different statement from
        /// "nothing has been recorded yet".
        /// </summary>
        // @driving_port @us-04 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task A_period_that_predates_everything_the_team_holds_stays_empty_and_nothing_is_invented()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            var heldBefore = EverythingTheChartsHold();

            var response = await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-700), TodayDay.AddDays(-500));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheChartComesBackEmpty(response);
                ThenNothingWasInventedToFillIt(heldBefore);
            }
        }

        // @driving_port @us-04 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task A_team_with_nothing_in_it_at_all_stays_empty_and_nothing_is_invented()
        {
            var teamId = GivenABrandNewTeamWithNothingInIt();
            var heldBefore = EverythingTheChartsHold();

            var response = await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheChartComesBackEmpty(response);
                ThenNothingWasInventedToFillIt(heldBefore);
            }
        }

        /// <summary>
        /// The third state. This team stopped being synced, so the recent part of the period will never
        /// fill in however long the coach waits - which is a different answer again from the other two.
        /// </summary>
        // @driving_port @us-04 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task A_team_nobody_is_syncing_any_more_stays_empty_for_the_period_since_it_stopped()
        {
            var lastObservedOn = TodayDay.AddDays(-120);
            var teamId = GivenATeamLastObservedOn(lastObservedOn);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), lastObservedOn);
            var heldBefore = EverythingTheChartsHold();

            var response = await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheChartComesBackEmpty(response);
                ThenNothingWasInventedToFillIt(heldBefore);
            }
        }

        /// <summary>
        /// Asking for more than can be answered must not turn into answering nothing: the part of the
        /// period the team's work does cover still arrives.
        /// </summary>
        // @driving_port @us-04 @boundary @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task A_period_that_reaches_further_back_than_the_team_does_still_returns_the_part_it_covers()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-40), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTrend(teamId, TodayDay.AddDays(-400), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheChartStillCoversThePeriodItCan(teamId, TodayDay.AddDays(-40), TodayDay);
        }

        // @driving_port @us-04 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task The_limits_chart_stays_empty_for_a_period_that_predates_everything_the_team_holds()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            var heldBefore = EverythingTheChartsHold();

            var response = await WhenTheDeliveryLeadOpensTheLimits(teamId, TodayDay.AddDays(-700), TodayDay.AddDays(-500));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheChartComesBackEmpty(response);
                ThenNothingWasInventedToFillIt(heldBefore);
            }
        }
    }
}
