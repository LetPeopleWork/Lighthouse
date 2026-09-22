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
    /// All pending - the behaviour arrives with this story.
    ///
    /// Step definitions live in Slice07WhereTheLimitsActuallyMovedSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6053-reconstruct-over-time-history")]
    [Category("slice-07")]
    public partial class Slice07WhereTheLimitsActuallyMovedTest
    {
        private const string Pending = "Pending: reconstruction of missing over-time days is not built yet (story 6053, slice 03).";

        // @driving_port @us-03 @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
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
        [Ignore(Pending)]
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
        [Ignore(Pending)]
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
        /// </summary>
        // @us-03 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task A_period_with_nothing_to_draw_limits_from_reports_no_limits()
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
        [Ignore(Pending)]
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
        /// </summary>
        // @us-03 @driving_port @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task A_team_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_not_an_empty_chart()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-250), TodayDay);
            GivenTheTeamPinnedTheStretchItsLimitsAreDrawnFrom(teamId, TodayDay.AddDays(-240), TodayDay.AddDays(-150));

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
            await WhenTheChartHasFinishedFillingIn();

            ThenTheLimitsHoldSteadyAcross(teamId, OwnerType.Team, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
        }

        // @us-03 @driving_port @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task A_portfolio_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_too()
        {
            var portfolioId = GivenAPortfolioStillBeingRefreshed();
            GivenThePortfolioFinishedOneDeliveryADayFrom(portfolioId, TodayDay.AddDays(-250), TodayDay);
            GivenThePortfolioPinnedTheStretchItsLimitsAreDrawnFrom(portfolioId, TodayDay.AddDays(-240), TodayDay.AddDays(-150));

            await WhenTheDeliveryLeadOpensThePortfolioLimits(portfolioId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
            await WhenTheChartHasFinishedFillingIn();

            ThenTheLimitsHoldSteadyAcross(portfolioId, OwnerType.Portfolio, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
        }

        /// <summary>
        /// The other half. A team that has not fixed the stretch draws each day's limits from that day's
        /// own recent history, so the lines are free to move - which is what makes "did the limits move"
        /// answerable at all.
        /// </summary>
        // @us-03 @real-io @contract-shape:bounded-change
        [Test]
        [Ignore(Pending)]
        public async Task A_team_that_did_not_fix_the_stretch_reads_limits_drawn_from_each_days_own_history()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
            await WhenTheChartHasFinishedFillingIn();

            ThenTheLimitsAreFreeToMoveAcross(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-60), TodayDay.AddDays(-30));
        }

        // @us-03 @fidelity @real-io @contract-shape:pure-function
        [Test]
        [Ignore(Pending)]
        public async Task Limits_worked_out_afterwards_read_the_same_as_the_day_they_were_watched()
        {
            var teamId = GivenATeamStillBeingRefreshed();
            GivenTheTeamFinishedOneItemADayFrom(teamId, TodayDay.AddDays(-200), TodayDay);
            var asWatched = await GivenALimitDayTheRecorderWroteAndThenLost(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-15));

            await WhenTheDeliveryLeadOpensTheTeamLimits(teamId, ProcessBehaviorMetricType.Throughput, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartHasFinishedFillingIn();

            ThenTheLimitsCameBackTheSameAsWhenTheyWereWatched(teamId, ProcessBehaviorMetricType.Throughput, asWatched);
        }

        // @us-03 @error @real-io @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
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
