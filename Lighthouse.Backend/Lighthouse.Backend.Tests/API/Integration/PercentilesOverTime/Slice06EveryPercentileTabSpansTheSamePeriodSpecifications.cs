using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for the scenario proving that every tab of the Percentiles Over Time widget
    /// covers the same period.
    /// </summary>
    public partial class Slice06EveryPercentileTabSpansTheSamePeriodTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        /// <summary>
        /// Work item age has no horizon to look back over - it is read as of the day itself - so it is
        /// filed under the horizon-less sentinel rather than being given an arbitrary one.
        /// </summary>
        private const int NoHorizon = PercentilesOverTimeSnapshot.NoHorizon;

        private static readonly int[] EveryCycleTimeHorizon = [30, 60, 90];

        // --- Given ---

        private int GivenATeamStillBeingRefreshed() => SeedTeamObservedUntil(TodayDay);

        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedItemFinishedOn(teamId, $"{teamId}-{day:yyyyMMdd}", day.AddDays(-1), day);
            }
        }

        private void GivenAnItemStillInFlightSince(int teamId, string referenceId, DateOnly startedOn)
            => SeedItemStillInProgressSince(teamId, referenceId, startedOn);

        // --- When ---

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheCycleTimeTab(int teamId, int horizon, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.CycleTime, horizon, from, to);

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheWorkItemAgeTab(int teamId, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.WorkItemAge, NoHorizon, from, to);

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        // --- Then ---

        private void ThenEveryTabCoversTheSamePeriod(int teamId, DateOnly from, DateOnly to)
        {
            var spans = EveryCycleTimeHorizon
                .Select(horizon => (Tab: $"cycle time over {horizon} days", Days: DaysOn(teamId, MetricType.CycleTime, horizon)))
                .Append((Tab: "work item age", Days: DaysOn(teamId, MetricType.WorkItemAge, NoHorizon)))
                .ToList();

            var expected = EveryDayFrom(from, to);

            using (Assert.EnterMultipleScope())
            {
                foreach (var span in spans)
                {
                    Assert.That(span.Days, Is.EquivalentTo(expected),
                        $"Comparing age against finished cycle time only works when both tabs cover the same period. " +
                        $"The {span.Tab} tab does not. Missing: {string.Join(", ", expected.Except(span.Days))}.");
                }
            }
        }

        // --- Shared observation helpers ---

        private List<DateOnly> DaysOn(int teamId, MetricType metricType, int horizon)
            => [.. PercentileDaysHeldFor(teamId, OwnerType.Team, metricType, horizon).Select(day => day.RecordedAt)];

        private static List<DateOnly> EveryDayFrom(DateOnly from, DateOnly to)
        {
            var days = new List<DateOnly>();
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                days.Add(day);
            }

            return days;
        }
    }
}
