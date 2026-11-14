using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for Slice 05 - the cycle-time trend a team's stored items already support.
    ///
    /// Everything enters through the shipped percentiles-over-time read endpoint. Nothing here calls a
    /// reconciler, a filler or a snapshot repository to make reconstruction happen: a chart load is the
    /// only thing a flow coach does, so a chart load is the only thing these scenarios do.
    ///
    /// The one place the scenarios reach past the endpoint is to read back what the chart now holds, and
    /// to wait for the background pass. Both are observations, not instructions.
    /// </summary>
    public partial class Slice05ReconstructCycleTimeHistoryTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        /// <summary>The horizon this slice works in. The other two arrive with slice 06.</summary>
        private const int ThirtyDays = 30;


        // --- Given ---

        private int GivenATeamStillBeingRefreshed() => SeedTeamObservedUntil(TodayDay);

        /// <summary>
        /// One item finished on each day of the span, each taking two days. A steady trickle is what
        /// makes a day computable: the thirty days before it have to contain something that closed.
        /// </summary>
        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedItemFinishedOn(teamId, $"{teamId}-{day:yyyyMMdd}", day.AddDays(-1), day);
            }
        }

        private RecordedPercentileDay GivenTheDayWasAlreadyRecordedAs(int teamId, DateOnly day, int p50, int p70, int p85, int p95)
        {
            SeedRecordedPercentileDay(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays, day, p50, p70, p85, p95);
            return new RecordedPercentileDay(day, p50, p70, p85, p95);
        }

        // --- When ---

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheCycleTimeTrend(int teamId, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.CycleTime, ThirtyDays, from, to);

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        private Task WhenTheTeamsRefreshRuns(int teamId) => TheTeamsRefreshCompletes(teamId);

        // --- Then ---

        private void ThenTheTrendCoversEveryDayFrom(int teamId, DateOnly from, DateOnly to)
        {
            var held = DaysHeldFor(teamId);
            var expected = EveryDayFrom(from, to);

            Assert.That(held, Is.EquivalentTo(expected),
                $"The chart must cover every day between {from:yyyy-MM-dd} and {to:yyyy-MM-dd} that the stored items support. " +
                $"Missing: {string.Join(", ", expected.Except(held))}. Unexpected: {string.Join(", ", held.Except(expected))}.");
        }

        private static void ThenTheTrendHeldOnlyTheDaysAlreadyWritten(SeriesResponse response, IReadOnlyList<DateOnly> alreadyWritten)
        {
            Assert.That(DatesIn(response), Is.EqualTo(alreadyWritten),
                "The load answers with what the chart holds at that moment. Filling in what is missing happens afterwards, so " +
                "no reader ever waits for it.");
        }

        private void ThenNothingWasWrittenWhileTheReaderWaited(int heldBefore)
        {
            Assert.That(TotalPercentileDaysHeld(), Is.EqualTo(heldBefore),
                "Opening a chart must not write to the database on the thread the reader is waiting on.");
        }

        private void ThenEveryDayIsHeldExactlyOnceAcross(int teamId, DateOnly from, DateOnly to)
        {
            var duplicated = DaysHeldFor(teamId)
                .Where(day => day >= from && day <= to)
                .GroupBy(day => day)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            Assert.That(duplicated, Is.Empty,
                $"A fill and a refresh writing at the same instant must not leave the same day twice. Duplicated: {string.Join(", ", duplicated)}.");
        }

        // --- Shared observation helpers ---

        private List<DateOnly> DaysHeldFor(int teamId)
            => [.. PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays).Select(day => day.RecordedAt)];

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
