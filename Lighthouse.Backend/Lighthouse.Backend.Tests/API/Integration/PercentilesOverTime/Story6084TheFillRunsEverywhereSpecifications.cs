using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for the fill running on every instance. Charts are opened the way a flow coach and a
    /// delivery lead open them, through the two series endpoints, and what they hold afterwards is read from
    /// the store the charts are drawn from. The state an earlier release left behind is written straight
    /// into the store, because the behaviour-settings write that once changed it no longer accepts it.
    /// </summary>
    public partial class Story6084TheFillRunsEverywhereTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        private const string PendingDeliver = "Story #6084 — pending DELIVER";

        private const int ThirtyDays = 30;

        /// <summary>
        /// The key the setting was stored under, spelled out because the product's constant for it is on its
        /// way out, and an instance upgrading into this release still carries this exact string.
        /// </summary>
        private const string TheHistoryFillKey = "OverTimeHistoryFill";

        /// <summary>The record the release before kept of having switched the fill on once.</summary>
        private const string TheRecordOfTheOneTimeSwitchOn = "OptionalFeatures:HistoryFillSwitchedOnByDefault";

        // --- Given ---

        /// <summary>
        /// One item finished on each day of the span, each taking two days, so every day of a period inside
        /// it has something the thirty days before it finished and can be filled.
        /// </summary>
        private int GivenATeamThatFinishedOneItemADayForTheLastFourMonths()
        {
            var teamId = SeedTeamObservedUntil(TodayDay);
            SeedItemsFinishedOn(teamId, TodayDay.AddDays(-120), TodayDay, _ => 1);
            return teamId;
        }

        private int GivenAPortfolioThatFinishedOneDeliveryADayForTheLastFourMonths()
        {
            var portfolioId = SeedPortfolioObservedUntil(TodayDay);

            for (var day = TodayDay.AddDays(-120); day <= TodayDay; day = day.AddDays(1))
            {
                SeedDeliveryFinishedOn(portfolioId, $"{portfolioId}-{day:yyyyMMdd}", day.AddDays(-4), day);
            }

            return portfolioId;
        }

        /// <summary>
        /// What the release before left on an instance whose admin switched the fill off: the setting stored
        /// off, and the record that the one-time switch-on had already happened, so that release kept it off.
        /// </summary>
        private void GivenTheLastReleaseLeftTheFillSwitchedOffByTheAdmin()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            context.OptionalFeatures.RemoveRange(context.OptionalFeatures.Where(feature => feature.Key == TheHistoryFillKey));
            context.AppSettings.RemoveRange(context.AppSettings.Where(setting => setting.Key == TheRecordOfTheOneTimeSwitchOn));

            context.OptionalFeatures.Add(ASwitchedOffFillSetting());
            context.AppSettings.Add(new AppSetting { Id = 0, Key = TheRecordOfTheOneTimeSwitchOn, Value = "true" });

            context.SaveChanges();
        }

        /// <summary>
        /// The harness hands every scenario an empty database, without the settings a start-up seeds. Running
        /// the seeders gives the instance the settings this release starts with, so the old setting that turns
        /// up later lands on an instance as it really runs, not on one missing everything a start-up writes.
        /// </summary>
        private void GivenTheInstanceRunsThisRelease() => TheInstanceIsUpgraded();

        // --- When ---

        private void WhenTheInstanceIsUpgraded() => TheInstanceIsUpgraded();

        private Task<SeriesResponse> WhenTheFlowCoachOpensPercentilesOverTime(int teamId, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.CycleTime, ThirtyDays, from, to);

        private Task<SeriesResponse> WhenTheDeliveryLeadOpensPbcOverTime(int portfolioId, DateOnly from, DateOnly to)
            => ReadPortfolioLimitTrend(portfolioId, ProcessBehaviorMetricType.Throughput, from, to);

        private Task WhenTheChartsHaveFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        /// <summary>Written straight into the store: no port of this release writes this setting any more.</summary>
        private void WhenAnOldSwitchedOffFillSettingTurnsUp()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var stored = context.OptionalFeatures.SingleOrDefault(feature => feature.Key == TheHistoryFillKey);

            if (stored == null)
            {
                context.OptionalFeatures.Add(ASwitchedOffFillSetting());
            }
            else
            {
                stored.Enabled = false;
            }

            context.SaveChanges();
        }

        // --- Then ---

        private void ThenPercentilesOverTimeCoversEveryDayFrom(int teamId, DateOnly from, DateOnly to)
            => ThenTheChartHoldsEveryDayFrom(
                "Percentiles Over Time", teamId,
                [.. PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays).Select(day => day.RecordedAt)],
                from, to);

        private void ThenPbcOverTimeCoversEveryDayFrom(int portfolioId, DateOnly from, DateOnly to)
            => ThenTheChartHoldsEveryDayFrom(
                "PBC Over Time", portfolioId,
                [.. LimitDaysHeldFor(portfolioId, OwnerType.Portfolio, ProcessBehaviorMetricType.Throughput).Select(day => day.RecordedAt)],
                from, to);

        private static void ThenTheChartHoldsEveryDayFrom(string chart, int ownerId, List<DateOnly> held, DateOnly from, DateOnly to)
        {
            var expected = new List<DateOnly>();
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                expected.Add(day);
            }

            Assert.That(held, Is.SupersetOf(expected),
                $"{chart} for owner {ownerId} was opened and should have filled in every day from {from:yyyy-MM-dd} to " +
                $"{to:yyyy-MM-dd}. Missing: {string.Join(", ", expected.Except(held))}.");
        }

        private static OptionalFeature ASwitchedOffFillSetting()
            => new()
            {
                Id = 0,
                Key = TheHistoryFillKey,
                Name = "Fill in past days on over-time charts",
                Description = "A preview. While this is on, opening Percentiles Over Time or PBC Over Time fills in the days the chart is missing.",
                Enabled = false,
                IsPreview = true,
                IsPremium = false,
            };
    }
}
