using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Builds a continuous daily blocked-count series over [start, end] from sparse BlockedCountSnapshot
    /// rows. Bug 5522: days with no recorded snapshot (Lighthouse offline, standalone-mode weekends)
    /// surfaced as gaps in the blocked-over-time chart and as a fabricated zero in the trend widget;
    /// each such day now carries the last known count forward, seeded from the latest snapshot BEFORE
    /// the window. Days preceding the first-ever snapshot emit nothing — the forward-only honest empty
    /// state (slice-03) is preserved and no value is fabricated ahead of recording history.
    /// Shared by TeamMetricsController and PortfolioMetricsController so both read the same knowledge.
    /// </summary>
    public static class BlockedCountSeriesBuilder
    {
        public static List<BlockedCountSnapshot> BuildDailySeries(
            IBlockedCountSnapshotRepository snapshotRepository,
            int ownerId,
            OwnerType ownerType,
            DateOnly start,
            DateOnly end)
        {
            var recordedByDay = snapshotRepository
                .GetAllByPredicate(s => s.OwnerId == ownerId && s.OwnerType == ownerType
                                        && s.RecordedAt >= start && s.RecordedAt <= end)
                .OrderBy(s => s.RecordedAt)
                .AsEnumerable()
                .ToDictionary(s => s.RecordedAt, s => s.BlockedCount);

            var lastKnown = snapshotRepository
                .GetLatestAtOrBefore(ownerId, ownerType, start.AddDays(-1))
                ?.BlockedCount;

            var series = new List<BlockedCountSnapshot>();
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                if (recordedByDay.TryGetValue(day, out var recorded))
                {
                    lastKnown = recorded;
                }

                if (lastKnown.HasValue)
                {
                    series.Add(new BlockedCountSnapshot
                    {
                        OwnerId = ownerId,
                        OwnerType = ownerType,
                        RecordedAt = day,
                        BlockedCount = lastKnown.Value,
                    });
                }
            }

            return series;
        }
    }
}
