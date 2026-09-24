using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for Slice 08 - the states in which an over-time chart is still empty after this
    /// story, so that the words the widget uses can be true of each of them.
    ///
    /// This fixture covers the half that is observable behind the endpoint: which of those states leave
    /// the chart genuinely empty, and that nothing is invented to fill them. The words themselves are a
    /// browser concern and belong with the widget tests, which are written once slices 05-07 settle
    /// which states actually remain reachable.
    /// </summary>
    public partial class Slice08NothingToShowTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        private const int ThirtyDays = 30;

        // --- Given ---

        private int GivenATeamStillBeingRefreshed() => SeedTeamObservedUntil(TodayDay);

        private int GivenATeamLastObservedOn(DateOnly lastObservedOn) => SeedTeamObservedUntil(lastObservedOn);

        /// <summary>A team that has never been synced and holds nothing at all.</summary>
        private int GivenABrandNewTeamWithNothingInIt() => SeedTeamObservedUntil(TodayDay);

        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedItemFinishedOn(teamId, $"{teamId}-{day:yyyyMMdd}", day.AddDays(-1), day);
            }
        }

        /// <summary>
        /// An item started long before the period and never finished. Every day of the period then has an
        /// age and a count of work in progress to report, so the absence gates that keep a quiet day off
        /// the chart have nothing to refuse - and only the rule that no day before the team's first
        /// finished item is worked out keeps the period empty.
        /// </summary>
        private void GivenAnItemTheTeamHasHadInProgressSinceBeforeThePeriod(int teamId)
            => SeedItemStillInProgressSince(teamId, $"{teamId}-in-progress", TodayDay.AddDays(-800));

        // --- When ---

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheCycleTimeTrend(int teamId, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.CycleTime, ThirtyDays, from, to);

        private Task<SeriesResponse> WhenTheDeliveryLeadOpensTheLimits(int teamId, DateOnly from, DateOnly to)
            => ReadTeamLimitTrend(teamId, ProcessBehaviorMetricType.Throughput, from, to);

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        // --- Then ---

        private static void ThenTheChartComesBackEmpty(SeriesResponse response)
        {
            Assert.That(DatesIn(response), Is.Empty,
                "There is nothing to show for this period, and an empty chart is the honest answer. What the widget then says " +
                "about it has to be true of this state specifically, not of forward-only recording.");
        }

        private void ThenNothingWasInventedToFillIt(int heldBefore)
        {
            Assert.That(TotalPercentileDaysHeld() + TotalLimitDaysHeld(), Is.EqualTo(heldBefore),
                "A period the stored work cannot speak to must stay empty. Filling it would replace \"we have nothing for this " +
                "period\" with a confident line nobody observed.");
        }

        private void ThenTheChartStillCoversThePeriodItCan(int teamId, DateOnly from, DateOnly to)
        {
            var held = PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays)
                .Select(day => day.RecordedAt)
                .Where(day => day >= from && day <= to)
                .ToList();

            Assert.That(held, Is.Not.Empty,
                $"Asking about a period that is partly beyond reach must still return the part that is within it, " +
                $"between {from:yyyy-MM-dd} and {to:yyyy-MM-dd}.");
        }

        private int EverythingTheChartsHold() => TotalPercentileDaysHeld() + TotalLimitDaysHeld();
    }
}
