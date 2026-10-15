using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// At the charts: the over-time charts fill in the days they are missing on every instance,
    /// including one whose administrator had switched the fill off, and no stored setting can stop a fill
    /// that is waiting its turn. That the setting itself is gone from Behaviour Settings, the store, a
    /// restored backup and a cleared database is shown by the scenarios beside the other Behaviour Settings ones.
    ///
    /// These live on the fill's own harness, not beside the behaviour-settings scenarios, because watching a
    /// chart fill needs what only this harness has: a pinned calendar day, owners with a finished history,
    /// a pass that can be held part-way, and a way to wait for the background fill to finish.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6084-over-time-history-fill-always-on")]
    public partial class Story6084TheFillRunsEverywhereTest
    {
        // @driving_port @real-io @kpi @contract-shape:bounded-change
        // The instance this change is for: an admin had switched the fill off on the release before, and after
        // the upgrade both over-time charts fill in on the next open without anyone touching anything.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task After_the_upgrade_an_instance_whose_admin_had_switched_the_fill_off_fills_in_both_over_time_charts()
        {
            var teamId = GivenATeamThatFinishedOneItemADayForTheLastFourMonths();
            var portfolioId = GivenAPortfolioThatFinishedOneDeliveryADayForTheLastFourMonths();
            GivenTheLastReleaseLeftTheFillSwitchedOffByTheAdmin();
            WhenTheInstanceIsUpgraded();

            await WhenTheFlowCoachOpensPercentilesOverTime(teamId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheDeliveryLeadOpensPbcOverTime(portfolioId, TodayDay.AddDays(-30), TodayDay);
            await WhenTheChartsHaveFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenPercentilesOverTimeCoversEveryDayFrom(teamId, TodayDay.AddDays(-30), TodayDay);
                ThenPbcOverTimeCoversEveryDayFrom(portfolioId, TodayDay.AddDays(-30), TodayDay);
            }
        }

        // @driving_port @real-io @error @concurrency @contract-shape:bounded-change
        // A chart waits its turn while another fills in, and meanwhile a switched-off fill setting turns up,
        // put there by hand or left by an older copy of the application. Nothing reads it any more,
        // so the waiting chart is filled like the first: the only things that hold a fill back are the
        // maintenance gate, the per-visit budget and how far back the stored history reaches.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task A_chart_waiting_its_turn_still_fills_in_whatever_an_old_fill_setting_says()
        {
            var fillingTeamId = GivenATeamThatFinishedOneItemADayForTheLastFourMonths();
            var waitingTeamId = GivenATeamThatFinishedOneItemADayForTheLastFourMonths();
            GivenTheInstanceRunsThisRelease();
            await WhenTheFlowCoachOpensPercentilesOverTime(fillingTeamId, TodayDay.AddDays(-30), TodayDay);

            using (AReconstructionPassHeldInFlight())
            {
                await WhenTheFlowCoachOpensPercentilesOverTime(waitingTeamId, TodayDay.AddDays(-30), TodayDay);
                WhenAnOldSwitchedOffFillSettingTurnsUp();
            }

            await WhenTheChartsHaveFinishedFillingIn();

            using (Assert.EnterMultipleScope())
            {
                ThenPercentilesOverTimeCoversEveryDayFrom(fillingTeamId, TodayDay.AddDays(-30), TodayDay);
                ThenPercentilesOverTimeCoversEveryDayFrom(waitingTeamId, TodayDay.AddDays(-30), TodayDay);
            }
        }
    }
}
