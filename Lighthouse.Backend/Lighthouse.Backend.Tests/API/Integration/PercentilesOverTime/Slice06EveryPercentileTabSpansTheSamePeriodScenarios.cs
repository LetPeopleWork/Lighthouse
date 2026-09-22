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
    /// route.
    ///
    /// Step definitions live in Slice06EveryPercentileTabSpansTheSamePeriodSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-06")]
    public partial class Slice06EveryPercentileTabSpansTheSamePeriodTest
    {
        /// <summary>
        /// Coverage and nothing else: the period asked for comes back whole. What each look-back makes of
        /// a day is a different question, and the scenario below is the one that asks it.
        ///
        /// Sixty days rather than thirty or ninety because the other scenarios in this file already open
        /// those two, so taking the one nobody else opens leaves all three exercised somewhere.
        /// </summary>
        // @driving_port @us-02 @real-io @contract-shape:bounded-change
        [Test]
        public async Task A_look_back_fills_in_every_day_of_the_period_asked_for()
        {
            const int overSixtyDays = 60;

            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheFlowCoachOpensTheCycleTimeTab(teamId, overSixtyDays, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTabCoversEveryDayFrom(teamId, OwnerType.Team, MetricType.CycleTime, overSixtyDays, TodayDay.AddDays(-30), TodayDay);
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
        public async Task A_portfolio_fills_in_its_delivery_cycle_time_the_same_way_a_team_does()
        {
            var portfolioId = GivenAPortfolioStillBeingRefreshed();
            GivenThePortfolioFinishedOneDeliveryADayFrom(portfolioId, TodayDay.AddDays(-120), TodayDay);

            await WhenTheFlowCoachOpensThePortfolioCycleTimeTab(portfolioId, 30, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheTabCoversEveryDayFrom(portfolioId, OwnerType.Portfolio, MetricType.CycleTime, 30, TodayDay.AddDays(-30), TodayDay);
        }

        /// <summary>
        /// Age reads what was running on the day itself, so a day needs something running on it before it
        /// has anything to say - and a delivery that closed on that day was no longer running by it. The
        /// finished deliveries alone therefore leave the last day of the window with nothing to measure,
        /// which is correctly left blank rather than drawn at zero. One delivery still in flight is what
        /// gives every day of the period something to report, the same way the team scenarios seed one.
        /// </summary>
        // @driving_port @us-02 @real-io @contract-shape:bounded-change
        [Test]
        public async Task A_portfolio_fills_in_its_delivery_age_tab_too()
        {
            var portfolioId = GivenAPortfolioStillBeingRefreshed();
            GivenThePortfolioFinishedOneDeliveryADayFrom(portfolioId, TodayDay.AddDays(-120), TodayDay);
            GivenADeliveryStillInFlightSince(portfolioId, "in-flight-throughout", TodayDay.AddDays(-150));

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

        /// <summary>
        /// Past its last observation a team's items are frozen at the break: whatever was open then still
        /// reads as open, and a walk that carries on ages it by a day for every day since - a confidently
        /// rising line over a period nobody watched. That is why one item is left open at the break here.
        /// Without it the days past the break have nothing in flight and so nothing to write either, and
        /// the tab would stop in the right place whether the break were honoured or ignored.
        ///
        /// Stating the whole span rather than only where it ends also says the fill reached the break. A
        /// pass that wrote nothing at all stops in the right place too.
        /// </summary>
        // @us-02 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task The_age_tab_stops_where_the_team_stopped_being_watched()
        {
            var lastObservedOn = TodayDay.AddDays(-60);
            var teamId = GivenATeamLastObservedOn(lastObservedOn);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-120), lastObservedOn);
            GivenAnItemStillInFlightSince(teamId, "open-at-the-break", TodayDay.AddDays(-100));

            await WhenTheFlowCoachOpensTheWorkItemAgeTab(teamId, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheTabStopsOn(teamId, OwnerType.Team, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon, lastObservedOn);
                ThenTheTabCoversEveryDayFrom(teamId, OwnerType.Team, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon, TodayDay.AddDays(-90), lastObservedOn);
            }
        }

        /// <summary>
        /// The portfolio half of the same break, and the half nothing was watching: every other portfolio
        /// scenario here is of a portfolio still being refreshed, where its last observation and today are
        /// one and the same day, so a reading anchored to today rather than to the break looks identical.
        /// </summary>
        // @us-02 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task The_portfolio_age_tab_stops_where_the_portfolio_stopped_being_watched()
        {
            var lastObservedOn = TodayDay.AddDays(-60);
            var portfolioId = GivenAPortfolioLastObservedOn(lastObservedOn);
            GivenThePortfolioFinishedOneDeliveryADayFrom(portfolioId, TodayDay.AddDays(-120), lastObservedOn);
            GivenADeliveryStillInFlightSince(portfolioId, "open-at-the-break", TodayDay.AddDays(-100));

            await WhenTheFlowCoachOpensThePortfolioWorkItemAgeTab(portfolioId, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheTabStopsOn(portfolioId, OwnerType.Portfolio, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon, lastObservedOn);
                ThenTheTabCoversEveryDayFrom(portfolioId, OwnerType.Portfolio, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon, TodayDay.AddDays(-90), lastObservedOn);
            }
        }

        /// <summary>
        /// A day the team had nothing running has no age to report, and four zeroes there would draw a
        /// floor the team never stood on - worked out across a thin stretch of history they line up into a
        /// confident falsehood. The team here finished an item a day for a stretch and then nothing, so the
        /// days after that stretch are genuinely quiet and the tab must end where the work did.
        /// </summary>
        // @us-02 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_day_the_team_had_nothing_in_flight_is_left_blank_rather_than_drawn_at_zero()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-25), TodayDay.AddDays(-20));

            await WhenTheFlowCoachOpensTheWorkItemAgeTab(teamId, TodayDay.AddDays(-25), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheTabCoversEveryDayFrom(teamId, OwnerType.Team, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon, TodayDay.AddDays(-25), TodayDay.AddDays(-21));
                ThenThoseDaysAreLeftBlank(teamId, OwnerType.Team, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon, TodayDay.AddDays(-20), TodayDay);
            }
        }
    }
}
