using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DISTILL acceptance scenarios for story 6053, slice 02 - every tab of the Percentiles Over Time
    /// widget covers the same period. A flow coach toggles work item age against each cycle-time
    /// look-back, at team scope and at portfolio scope, and compares trends rather than comparing how
    /// long each tab happened to be recorded for.
    ///
    /// Driving port: the shipped percentiles-over-time read endpoint, on both the team and the portfolio
    /// route. All pending - the behaviour arrives with this story.
    ///
    /// Step definitions live in Slice06EveryPercentileTabSpansTheSamePeriodSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-06")]
    public partial class Slice06EveryPercentileTabSpansTheSamePeriodTest
    {
        private const string Pending = "Pending: reconstruction of missing over-time days is not built yet (story 6053, slice 02).";

        // @driving_port @us-02 @real-io @contract-shape:bounded-change
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(90)]
        public async Task Each_cycle_time_look_back_fills_in_over_its_own_period(int horizon)
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, horizon, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTabCoversEveryDayFrom(teamId, OwnerType.Team, MetricType.CycleTime, horizon, TodayDay.AddDays(-30), TodayDay);
        }

        /// <summary>
        /// Which days a look-back covers cannot tell one look-back from another. A fill that worked every
        /// row out over the same period and filed them under three different ones would cover the same
        /// days and be indistinguishable on the tab - and sixty and ninety would be wrong by exactly the
        /// amount nobody is looking at.
        ///
        /// What separates them is the stretch of work each one summarises. So this asks a team whose pace
        /// changed - slow until a month ago, quick since - what each look-back makes of the same day. The
        /// thirty-day one has moved past the slow stretch. The ninety-day one is still inside it, and must
        /// read higher for it.
        /// </summary>
        // @driving_port @us-02 @real-io @contract-shape:pure-function
        [Test]
        public async Task A_longer_look_back_still_carries_a_slow_stretch_the_shorter_one_has_left_behind()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamTookWeeksOverEachItemFrom(teamId, TodayDay.AddDays(-90), TodayDay.AddDays(-31));
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-30), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 30, TodayDay.AddDays(-2), TodayDay);
            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 90, TodayDay.AddDays(-2), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheLongerLookBackReadsHigherOn(teamId, TodayDay, shorter: 30, longer: 90);
        }

        /// <summary>
        /// The reading that makes or breaks the age tab. An item that ran from one day to another was in
        /// flight on the days between, at the age it had reached by each of them - not absent because it
        /// has since finished, and not at the age it finally reached.
        /// </summary>
        // @driving_port @us-02 @real-io @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task An_item_counts_towards_a_past_day_at_the_age_it_had_reached_by_then()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-61), TodayDay.AddDays(-60));
            GivenAnItemThatWasInFlightFrom(teamId, "long-runner", TodayDay.AddDays(-20), TodayDay.AddDays(-5));

            await WhenTheFlowCoachOpensTheWorkItemAgeTab(teamId, TodayDay.AddDays(-20), TodayDay.AddDays(-6));
            await WhenTheChartHasFinishedFillingIn();

            ThenTheItemCountsTowardsThatDayAtTheAgeItHadThen(teamId, TodayDay.AddDays(-16), expectedAge: 5);
        }

        // @us-02 @fidelity @real-io @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task An_age_day_worked_out_afterwards_reads_the_same_as_the_day_that_was_watched()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), TodayDay);
            GivenAnItemStillInFlightSince(teamId, "in-flight-throughout", TodayDay.AddDays(-40));
            var asWatched = await GivenAWorkItemAgeDayTheRecorderWroteAndThenLost(teamId, TodayDay.AddDays(-15));

            await WhenTheFlowCoachOpensTheWorkItemAgeTab(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheDayCameBackTheSameAsWhenItWasWatched(teamId, asWatched);
        }

        // @driving_port @us-02 @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task A_portfolio_fills_in_its_delivery_cycle_time_the_same_way_a_team_does()
        {
            var portfolioId = GivenAPortfolioStillBeingRefreshed();
            GivenThePortfolioFinishedOneDeliveryADayFrom(portfolioId, TodayDay.AddDays(-120), TodayDay);

            await WhenTheFlowCoachOpensThePortfolioCycleTimeTab(portfolioId, 30, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTabCoversEveryDayFrom(portfolioId, OwnerType.Portfolio, MetricType.CycleTime, 30, TodayDay.AddDays(-30), TodayDay);
        }

        // @driving_port @us-02 @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task A_portfolio_fills_in_its_delivery_age_tab_too()
        {
            var portfolioId = GivenAPortfolioStillBeingRefreshed();
            GivenThePortfolioFinishedOneDeliveryADayFrom(portfolioId, TodayDay.AddDays(-120), TodayDay);

            await WhenTheFlowCoachOpensThePortfolioWorkItemAgeTab(portfolioId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTabCoversEveryDayFrom(portfolioId, OwnerType.Portfolio, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon, TodayDay.AddDays(-30), TodayDay);
        }

        /// <summary>
        /// The tabs are four views of one owner over one period, so the second tab opened must find the
        /// work already done. If it started again, a coach flicking between tabs would pay for the same
        /// history four times.
        /// </summary>
        // @us-02 @real-io @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task Flicking_between_tabs_does_not_start_the_filling_over_again()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 30, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();
            var afterTheFirstTab = DaysOn(teamId, MetricType.CycleTime, 30);

            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 60, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTabIsUnchangedSince(teamId, MetricType.CycleTime, 30, afterTheFirstTab);
        }

        /// <summary>
        /// The comparison the combined widget exists for - is age climbing while finished cycle time
        /// looks steady - only works when both tabs cover the same period.
        /// </summary>
        // @driving_port @us-02 @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task Every_tab_covers_the_same_period_once_the_chart_has_filled_in()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);
            GivenAnItemStillInFlightSince(teamId, "in-flight-throughout", TodayDay.AddDays(-150));

            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 30, TodayDay.AddDays(-30), TodayDay);
            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 60, TodayDay.AddDays(-30), TodayDay);
            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, 90, TodayDay.AddDays(-30), TodayDay);
            await WhenTheFlowCoachOpensTheWorkItemAgeTab(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenEveryTabCoversTheSamePeriod(teamId, TodayDay.AddDays(-30), TodayDay);
        }

        // @us-02 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task The_age_tab_stops_where_the_team_stopped_being_watched_and_stays_blank_when_nothing_was_in_flight()
        {
            var lastObservedOn = TodayDay.AddDays(-60);
            var teamId = SeedTeamObservedUntil(lastObservedOn);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), lastObservedOn);

            await WhenTheFlowCoachOpensTheWorkItemAgeTab(teamId, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheTabStopsOn(teamId, OwnerType.Team, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon, lastObservedOn);
                ThenNoDayOnThatTabReadsAsFourZeroes(teamId, OwnerType.Team, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon);
            }
        }
    }
}
