using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Read-only port (Epic 5427). Serves the persisted percentiles-over-time series for an owner and
    /// horizon, ordered by RecordedAt. Side-effect-free — reads only the snapshot table, never a live
    /// recompute of historical days.
    /// </summary>
    public interface IPercentilesOverTimeSeriesQuery
    {
        IReadOnlyList<PercentilesOverTimeSnapshot> GetSeries(int ownerId, OwnerType ownerType, MetricType metricType, int? horizon);
    }
}
