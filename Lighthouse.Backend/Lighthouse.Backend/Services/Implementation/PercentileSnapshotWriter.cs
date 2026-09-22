using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation
{
    public class PercentileSnapshotWriter : IPercentileSnapshotWriter
    {
        private static readonly int[] CycleTimeHorizons = [30, 60, 90];

        // Work item age is measured as of a single day rather than over a trailing window, so the
        // family has no horizon dimension and produces exactly one row per day, under the sentinel.
        private static readonly int[] WorkItemAgeHorizons = [PercentilesOverTimeSnapshot.NoHorizon];

        private readonly IPercentilesOverTimeSnapshotRepository snapshotRepository;
        private readonly ILighthouseClock clock;

        public PercentileSnapshotWriter(
            IPercentilesOverTimeSnapshotRepository snapshotRepository,
            ILighthouseClock clock)
        {
            this.snapshotRepository = snapshotRepository;
            this.clock = clock;
        }

        public void RecordToday(
            int ownerId,
            OwnerType ownerType,
            MetricType metricType,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles)
        {
            // The day comes from the clock seam rather than from reducing an instant here: a derived
            // reduction is the same time-zone defect one call deeper.
            var day = clock.Today;

            foreach (var horizon in HorizonsFor(metricType))
            {
                WriteUnlessThereWasNothingToReport(
                    ownerId, ownerType, metricType, horizon, day,
                    ComputeDay(day, horizon, readPercentiles),
                    StoredRow(ownerId, ownerType, metricType, horizon, day));
            }
        }

        public void FillDayIfAbsent(
            int ownerId,
            OwnerType ownerType,
            MetricType metricType,
            DateOnly day,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles)
        {
            foreach (var horizon in HorizonsFor(metricType))
            {
                // A day that already carries a value was measured while that day was current, against
                // the data as it stood then. Recomputing it now would replace what was observed with a
                // guess assembled from what the tracker reports today, which is the worse of the two
                // answers, so a day that already has a value is left exactly as it is.
                if (StoredRow(ownerId, ownerType, metricType, horizon, day) != null)
                {
                    continue;
                }

                WriteUnlessThereWasNothingToReport(
                    ownerId, ownerType, metricType, horizon, day,
                    ComputeDay(day, horizon, readPercentiles),
                    stored: null);
            }
        }

        // Both the day written as it happens and a day worked out afterwards pass through here. The two
        // must not be able to disagree about what a quiet day is worth: a day that already carries a row
        // is never rewritten, so whichever of them reached it first would settle it permanently.
        private void WriteUnlessThereWasNothingToReport(
            int ownerId, OwnerType ownerType, MetricType metricType, int horizon, DateOnly day,
            PercentileReadings readings, PercentilesOverTimeSnapshot? stored)
        {
            // A day on which nothing finished has no cycle time to report, and four zeros are not a
            // measurement of a quiet period - they are a floor the team never stood on. Worked out
            // across a thin stretch of history they line up into one, which is a more confident
            // falsehood than the gap it would be replacing. The day is left with no row.
            if (readings.AreEmpty)
            {
                return;
            }

            if (stored == null)
            {
                snapshotRepository.Add(NewRow(ownerId, ownerType, metricType, horizon, day, readings));
                return;
            }

            Apply(readings, stored);
        }

        public async Task SaveFilledDay()
        {
            // Every iteration that absorbs a refusal drops at least one staged row, and there are
            // finitely many, so the loop runs at most once per row this day carries.
            while (true)
            {
                try
                {
                    await snapshotRepository.Save();
                    return;
                }
                catch (DbUpdateException refused)
                {
                    var somebodyElseGotThereFirst = EveryRefusedRowIsAlreadyStored(refused);

                    // Dropped either way. A refused row left staged would be retried on the next
                    // day's save and fail again, so one bad day would take the rest of the walk.
                    DropFromTheStagingArea(refused);

                    if (!somebodyElseGotThereFirst)
                    {
                        throw;
                    }
                }
            }
        }

        private bool EveryRefusedRowIsAlreadyStored(DbUpdateException refused)
        {
            return refused.Entries.Count > 0 && refused.Entries.All(entry =>
                entry.State == EntityState.Added &&
                entry.Entity is PercentilesOverTimeSnapshot row &&
                StoredRow(row.OwnerId, row.OwnerType, row.MetricType, row.Horizon ?? PercentilesOverTimeSnapshot.NoHorizon, row.RecordedAt) != null);
        }

        private static void DropFromTheStagingArea(DbUpdateException refused)
        {
            foreach (var entry in refused.Entries)
            {
                entry.State = EntityState.Detached;
            }
        }

        private static int[] HorizonsFor(MetricType metricType)
        {
            return metricType switch
            {
                MetricType.CycleTime => CycleTimeHorizons,
                MetricType.WorkItemAge => WorkItemAgeHorizons,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(metricType), metricType, "This percentile family has no horizon list."),
            };
        }

        private static PercentileReadings ComputeDay(
            DateOnly day,
            int horizon,
            Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles)
        {
            var windowEnd = InstanceCalendar.AsUtcMidnight(day);
            var percentiles = readPercentiles(windowEnd.AddDays(-horizon), windowEnd).ToList();

            return new PercentileReadings(
                ValueFor(percentiles, 50),
                ValueFor(percentiles, 70),
                ValueFor(percentiles, 85),
                ValueFor(percentiles, 95));
        }

        private PercentilesOverTimeSnapshot? StoredRow(
            int ownerId, OwnerType ownerType, MetricType metricType, int horizon, DateOnly day)
        {
            return snapshotRepository.GetByPredicate(
                snapshot => snapshot.OwnerId == ownerId &&
                            snapshot.OwnerType == ownerType &&
                            snapshot.MetricType == metricType &&
                            snapshot.Horizon == horizon &&
                            snapshot.RecordedAt == day);
        }

        private static PercentilesOverTimeSnapshot NewRow(
            int ownerId, OwnerType ownerType, MetricType metricType, int horizon, DateOnly day,
            PercentileReadings readings)
        {
            var row = new PercentilesOverTimeSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                MetricType = metricType,
                Horizon = horizon,
                RecordedAt = day,
            };

            Apply(readings, row);

            return row;
        }

        private static void Apply(PercentileReadings readings, PercentilesOverTimeSnapshot row)
        {
            row.P50 = readings.P50;
            row.P70 = readings.P70;
            row.P85 = readings.P85;
            row.P95 = readings.P95;
        }

        private static int ValueFor(List<PercentileValue> percentiles, int percentile)
        {
            return percentiles.FirstOrDefault(value => value.Percentile == percentile)?.Value ?? 0;
        }

        private sealed record PercentileReadings(int P50, int P70, int P85, int P95)
        {
            public bool AreEmpty => P50 == 0 && P70 == 0 && P85 == 0 && P95 == 0;
        }
    }
}
