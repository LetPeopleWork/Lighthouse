using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DISTILL acceptance scenarios for story 6053, slice 03 - where the process limits actually moved.
    /// A delivery lead opens Predictability - PBC Over Time and reads the upper limit, the average and
    /// the lower limit as three dated lines across the period under review, for every behaviour the
    /// toggle offers, rather than the three points the recorder happened to catch.
    ///
    /// Driving port: the shipped process-behavior-over-time read endpoint, team and portfolio routes.
    ///
    /// Step definitions live in Slice07WhereTheLimitsActuallyMovedSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-07")]
    public partial class Slice07WhereTheLimitsActuallyMovedTest
    {
        // @driving_port @us-03 @real-io @contract-shape:bounded-change
        [Test]
        public async Task A_team_fills_in_every_behaviour_it_reports_and_not_one_fewer()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheDeliveryLeadTogglesEveryTeamBehaviour(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenEveryBehaviourTheTeamReportsGotItsLimits(teamId);
        }

        // @driving_port @us-03 @real-io @contract-shape:bounded-change
        [Test]
        public async Task A_portfolio_fills_in_every_behaviour_it_reports_and_not_one_fewer()
        {
            var portfolioId = GivenAPortfolioStillBeingRefreshed();
            GivenThePortfolioFinishedOneDeliveryADayFrom(portfolioId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheDeliveryLeadTogglesEveryPortfolioBehaviour(portfolioId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenEveryBehaviourThePortfolioReportsGotItsLimits(portfolioId);
        }

        // @us-03 @real-io @contract-shape:bounded-change
        [Test]
        public async Task How_big_deliveries_are_getting_is_filled_in_for_a_portfolio_and_never_for_a_team()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);
            var portfolioId = GivenAPortfolioStillBeingRefreshed();
            GivenThePortfolioFinishedOneDeliveryADayFrom(portfolioId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheDeliveryLeadTogglesEveryTeamBehaviour(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheDeliveryLeadTogglesEveryPortfolioBehaviour(portfolioId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenDeliverySizeIsReportedForThePortfolioAndNotForTheTeam(teamId, portfolioId);
        }

        /// <summary>
        /// A reference stretch the team no longer keeps any finished work from cannot produce limits.
        /// The honest answer is no line, not a line at zero.
        ///
        /// Named for the stretch rather than for the period, because the stretch is the mechanism: the
        /// team pinned one reaching back further than it keeps finished work, so the limits are judged
        /// undrawable before any day's data is looked at. The earlier name claimed this was a period
        /// with nothing in it, which is a different refusal and belongs to the scenario below it.
        ///
        /// What this pins is the pair of honesty gates in the writer, not either one on its own. Every
        /// chart the status gate would refuse already satisfies the collapsed-band gate as well:
        /// ProcessBehaviourChart.NotReady hard-codes average, upper and lower limits to zero, and so
        /// does every other place a chart is built with a status other than Ready. Take the status gate
        /// away on its own and nothing here changes, because the band gate catches the same charts and
        /// this scenario stays green. Take the band gate away on its own and it fails; take both and it
        /// fails. So read it as "a stretch nothing can be drawn from gets no limits written", which is
        /// the promise, and not as a test of the status gate, which nothing observable separates. What
        /// keeps the status gate honest is the source-level invariant in
        /// OverTimeReconstructionSeamArchUnitTest - the one asserting that a chart which is not Ready
        /// carries no band - because that invariant is the reason the redundancy holds at all.
        /// </summary>
        // @us-03 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_stretch_pinned_further_back_than_the_team_keeps_work_reports_no_limits()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);
            GivenTheTeamPinnedAStretchItNoLongerKeepsAnyWorkFrom(teamId);

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenNoLimitsAreReportedFor(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-30), TodayDay);
        }

        // @us-03 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_stretch_in_which_the_team_finished_nothing_reports_no_band_rather_than_a_flat_zero_one()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay.AddDays(-150));

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-50));
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenNoLimitsAreReportedFor(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-50));
                ThenNoDayReportsACollapsedBand(teamId, ProcessBehaviorMetricType.Throughput);
            }
        }

        /// <summary>
        /// The scenario this slice exists for. Whether the limits are valid is currently judged against
        /// today, so a lead who fixed the reference stretch could ask about a past period and be told
        /// nothing - which reads as "your data did not support it" and is false. A fixed reference
        /// stretch produces limits that do not move; that is the reading, and an empty chart is not.
        ///
        /// The lead here keeps finished work for less long than the stretch they pinned reaches back,
        /// which is what makes "judged against today" and "judged against the day being rebuilt" two
        /// different answers rather than the same one twice. Equal-length retention would leave the
        /// scenario unable to fail; the guard below says so out loud rather than leaving it to be
        /// noticed. The lead's last refresh was shortly after the period under review rather than today,
        /// because a refresh today would already have deleted the stretch they pinned, and limits read from
        /// it would describe a store nobody could have. The team is also finishing more work every month,
        /// so a stretch that followed the window would give a different reading on every day of the
        /// period - which is what makes
        /// "the limits did not move" a claim about the pinned stretch rather than about flat data.
        ///
        /// For whoever makes this pass: the two assertions below fail to two different edits, and
        /// running one sabotage proves one half only.
        ///
        /// Judging validity against Clock.Today rather than against the day being rebuilt puts the
        /// pinned stretch back outside the retention window, the chart comes back unusable, no rows
        /// are written, and the FIRST assertion fails on an empty series. That is the half this slice
        /// is about.
        ///
        /// It does NOT move the limits, and expecting it to would mislead: with a stretch pinned, the
        /// average and the limits are computed from that fixed stretch alone and do not depend on the
        /// display window at all, so re-anchoring the window changes nothing about the stored triple.
        /// The SECOND assertion answers a different question - that the limits came from the pinned
        /// stretch and not from a rolling one - and the edit that fails it is ignoring the pin, that
        /// is, feeding the display window in as the baseline. With this seed that makes the limits
        /// climb day by day and the distinct count goes above one.
        /// </summary>
        // @us-03 @driving_port @real-io @contract-shape:bounded-change
        [Test]
        public async Task A_team_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_not_an_empty_chart()
        {
            var lastRefreshedOn = TodayDay.AddDays(-DaysAgoThePinnedStretchOwnerWasLastRefreshed);
            var teamId = GivenATeamLastRefreshedOnThatKeepsFinishedWorkFor(lastRefreshedOn, DaysFinishedWorkIsKeptForWhenTheStretchIsOutOfReach);
            GivenTheTeamFinishedMoreEachMonthFrom(teamId, TodayDay.AddDays(-250), lastRefreshedOn);
            GivenTheTeamPinnedTheStretchItsLimitsAreDrawnFrom(teamId, TodayDay.AddDays(-240), TodayDay.AddDays(-150));
            GivenTheTeamsStretchIsOutOfReachTodayAndInReachOverThePeriod(teamId, TodayDay.AddDays(-30));

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
            await WhenTheChartHasFinishedFillingIn();

            ThenTheLimitsHoldSteadyAcross(teamId, OwnerType.Team, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
        }

        // @us-03 @driving_port @real-io @contract-shape:bounded-change
        [Test]
        public async Task A_portfolio_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_too()
        {
            var lastRefreshedOn = TodayDay.AddDays(-DaysAgoThePinnedStretchOwnerWasLastRefreshed);
            var portfolioId = GivenAPortfolioLastRefreshedOnThatKeepsFinishedWorkFor(lastRefreshedOn, DaysFinishedWorkIsKeptForWhenTheStretchIsOutOfReach);
            GivenThePortfolioFinishedMoreDeliveriesEachMonthFrom(portfolioId, TodayDay.AddDays(-250), lastRefreshedOn);
            GivenThePortfolioPinnedTheStretchItsLimitsAreDrawnFrom(portfolioId, TodayDay.AddDays(-240), TodayDay.AddDays(-150));
            GivenThePortfoliosStretchIsOutOfReachTodayAndInReachOverThePeriod(portfolioId, TodayDay.AddDays(-30));

            await WhenTheDeliveryLeadOpensThePortfolioLimits(portfolioId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
            await WhenTheChartHasFinishedFillingIn();

            ThenTheLimitsHoldSteadyAcross(portfolioId, OwnerType.Portfolio, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
        }

        /// <summary>
        /// The other half. A team that has not fixed the stretch draws each day's limits from that day's
        /// own recent history, so the lines are free to move - which is what makes "did the limits move"
        /// answerable at all.
        ///
        /// This team is finishing more work every month, so the lines must actually climb over the
        /// period, and the Then says so. With a flat series they could not have moved whatever the
        /// implementation did, and this scenario would have agreed with the fixed-stretch one above
        /// instead of reading differently from it.
        /// </summary>
        // @us-03 @real-io @contract-shape:bounded-change
        [Test]
        public async Task A_team_that_did_not_fix_the_stretch_reads_limits_drawn_from_each_days_own_history()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedMoreEachMonthFrom(teamId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
            await WhenTheChartHasFinishedFillingIn();

            ThenTheLimitsAreFreeToMoveAcross(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
        }

        /// <summary>
        /// Worked out afterwards, a day has to be worked out the way it would have been on the day: over
        /// the stretch that ends on it, and judged as of it rather than as of today. Equality with what the
        /// recorder wrote only proves that if the wrong ways give a different answer, and with one item
        /// finished every day they did not - every stretch read the same, so reconstruction anchored on
        /// today, a day early or over twice the span all passed. The team here is the one getting faster,
        /// and it keeps finished work for just long enough that the watched day's stretch is in reach as of
        /// that day and out of reach as of today. It was last refreshed a little before today, so the store
        /// still holds that stretch - a team refreshed today would already have deleted it. The last Given
        /// checks that every one of those wrong ways really does read differently, and fails before the
        /// chart is opened if one does not.
        /// </summary>
        // @us-03 @fidelity @real-io @contract-shape:pure-function
        [Test]
        public async Task Limits_worked_out_afterwards_read_the_same_as_the_day_they_were_watched()
        {
            var teamId = GivenATeamLastRefreshedOnThatKeepsFinishedWorkFor(
                TodayDay.AddDays(-DaysAgoTheWatchedTeamWasLastRefreshed), DaysFinishedWorkIsKeptForWhenTheWatchedDayIsOutOfReachToday);
            GivenTheTeamFinishedMoreEachMonthFrom(teamId, TodayDay.AddDays(-200), TodayDay);
            var asWatched = await GivenALimitDayTheRecorderWroteAndThenLost(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-43));
            GivenThatDayReadsDifferentlyWhenWorkedOutAnyOtherWay(teamId, asWatched);

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-45), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheLimitsCameBackTheSameAsWhenTheyWereWatched(teamId, ProcessBehaviorMetricType.Throughput, asWatched);
        }

        // @us-03 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task Limits_stop_where_the_team_stopped_being_watched_and_a_second_look_changes_nothing()
        {
            var lastObservedOn = TodayDay.AddDays(-60);
            var teamId = GivenATeamLastObservedOn(lastObservedOn);
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), lastObservedOn);

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();
            var afterTheFirstVisit = LimitsReportedFor(teamId, ProcessBehaviorMetricType.Throughput);

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-90), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenTheLimitsStopOn(teamId, ProcessBehaviorMetricType.Throughput, lastObservedOn);
                ThenTheLimitsAreUnchangedSince(teamId, ProcessBehaviorMetricType.Throughput, afterTheFirstVisit);
            }
        }
    }
}
