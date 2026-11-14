using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    public partial class Slice07WhereTheLimitsActuallyMovedTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        // --- Given ---

        private int GivenATeamLastObservedOn(DateOnly lastObservedOn) => SeedTeamObservedUntil(lastObservedOn);

        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedItemFinishedOn(teamId, $"{teamId}-{day:yyyyMMdd}", day.AddDays(-1), day);
            }
        }

        // --- When ---

        private Task<SeriesResponse> WhenTheDeliveryLeadOpensTheTeamLimits(int teamId, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
            => ReadTeamLimitTrend(teamId, behaviour, from, to);

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        // --- Then ---

        private void ThenTheLimitsStopOn(int teamId, ProcessBehaviorMetricType behaviour, DateOnly lastObservedOn)
        {
            var beyond = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Select(day => day.RecordedAt)
                .Where(day => day > lastObservedOn)
                .ToList();

            Assert.That(beyond, Is.Empty,
                $"Nothing has been observed since {lastObservedOn:yyyy-MM-dd}, so limits past it would describe a period nobody " +
                $"watched. Written anyway: {string.Join(", ", beyond)}.");
        }

        private void ThenTheLimitsAreUnchangedSince(int teamId, ProcessBehaviorMetricType behaviour, IReadOnlyList<RecordedLimitDay> before)
        {
            Assert.That(LimitDaysHeldFor(teamId, OwnerType.Team, behaviour), Is.EqualTo(before),
                "Everything in the period already had limits, so a second look must cost nothing and change nothing.");
        }

        private IReadOnlyList<RecordedLimitDay> LimitsReportedFor(int teamId, ProcessBehaviorMetricType behaviour)
            => LimitDaysHeldFor(teamId, OwnerType.Team, behaviour);
    }
}
