using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetrics.Domain;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Platform.Persistence;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public sealed class WithinSleSnapshotWriter(IWithinSleSnapshotRepository snapshots, ILighthouseClock clock) : IWithinSleSnapshotWriter
    {
        /// <summary>
        /// The trailing windows of the Within SLE record. Kept with the one writer of both the day recorded today and
        /// the day filled in later, so the two always cover the same windows.
        /// </summary>
        public static readonly IReadOnlyList<int> Horizons = [30, 60, 90];

        public async Task RecordToday(int ownerId, OwnerType ownerType, WithinSleFamilyReader family)
        {
            // Without an SLE a day has nothing to be measured against, so nothing is written.
            var sle = family.CurrentSle();
            if (sle is null)
            {
                return;
            }

            var day = clock.Today;

            foreach (var horizon in Horizons)
            {
                var cycleTimesInDays = ReadWindow(family, horizon, day);
                if (cycleTimesInDays is null)
                {
                    continue;
                }

                var row = StoredRow(ownerId, ownerType, horizon, day);
                if (row is null)
                {
                    row = NewRow(ownerId, ownerType, horizon, day);
                    snapshots.Add(row);
                }

                Measure(row, cycleTimesInDays, sle, WithinSleSnapshotOrigin.Recorded);
            }

            await snapshots.Save();
        }

        public void FillDayIfAbsent(int ownerId, OwnerType ownerType, DateOnly day, WithinSleFamilyReader family)
        {
            var sle = family.CurrentSle();
            if (sle is null)
            {
                return;
            }

            foreach (var horizon in Horizons)
            {
                // A stored day was measured while it was current, against the SLE then in force; working it out
                // again now would replace what was observed with a guess, so it is left exactly as it is.
                if (StoredRow(ownerId, ownerType, horizon, day) is not null)
                {
                    continue;
                }

                // A window reaching back past the work the owner still keeps would count partly deleted work and read
                // lower than the owner ever was, so that window stays a gap while the shorter ones are still written.
                var cycleTimesInDays = ReadWindow(family, horizon, day);
                if (cycleTimesInDays is null)
                {
                    continue;
                }

                var row = NewRow(ownerId, ownerType, horizon, day);
                Measure(row, cycleTimesInDays, sle, WithinSleSnapshotOrigin.FilledIn);
                snapshots.Add(row);
            }
        }

        public Task SaveFilledDay()
        {
            return LostRaceTolerantSave.SaveAsync(snapshots.Save, IsAlreadyStored);
        }

        private static WithinSleSnapshot NewRow(int ownerId, OwnerType ownerType, int horizon, DateOnly day)
            => new() { OwnerId = ownerId, OwnerType = ownerType, Horizon = horizon, RecordedAt = day };

        private static IReadOnlyList<int>? ReadWindow(WithinSleFamilyReader family, int horizon, DateOnly day)
        {
            var windowEnd = InstanceCalendar.AsUtcMidnight(day);
            var windowStart = windowEnd.AddDays(-(horizon - 1));

            return family.ReadClosedCycleTimes(windowStart, windowEnd);
        }

        private static void Measure(WithinSleSnapshot row, IReadOnlyList<int> cycleTimesInDays, ServiceLevelExpectation sle, WithinSleSnapshotOrigin origin)
        {
            row.ClosedCount = cycleTimesInDays.Count;
            row.WithinCount = sle.CountWithin(cycleTimesInDays);
            row.SlePercentile = sle.Percentile;
            row.SleDays = sle.Days;
            row.Origin = origin;
        }

        private bool IsAlreadyStored(object refusedEntity)
        {
            return refusedEntity is WithinSleSnapshot row &&
                   StoredRow(row.OwnerId, row.OwnerType, row.Horizon, row.RecordedAt) is not null;
        }

        private WithinSleSnapshot? StoredRow(int ownerId, OwnerType ownerType, int horizon, DateOnly day)
        {
            return snapshots.GetByPredicate(
                snapshot => snapshot.OwnerId == ownerId &&
                            snapshot.OwnerType == ownerType &&
                            snapshot.Horizon == horizon &&
                            snapshot.RecordedAt == day);
        }
    }
}
