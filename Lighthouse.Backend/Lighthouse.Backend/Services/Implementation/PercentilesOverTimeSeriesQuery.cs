using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class PercentilesOverTimeSeriesQuery(IPercentilesOverTimeSnapshotRepository repository) : IPercentilesOverTimeSeriesQuery
    {
        public IReadOnlyList<PercentilesOverTimeSnapshot> GetSeries(int ownerId, OwnerType ownerType, MetricType metricType, int? horizon)
            => repository.GetSeries(ownerId, ownerType, metricType, ResolveHorizon(metricType, horizon));

        /// <summary>
        /// Work Item Age is measured as-of-today, so it has no horizon dimension: the recording pipeline
        /// persists it under the <see cref="PercentilesOverTimeSnapshot.NoHorizon"/> sentinel. Any horizon
        /// a caller carries over from a cycle-time tab is ignored here rather than matched literally —
        /// otherwise a stale <c>?horizon=30</c> would silently serve an empty age series.
        /// </summary>
        private static int? ResolveHorizon(MetricType metricType, int? horizon)
            => metricType == MetricType.WorkItemAge ? PercentilesOverTimeSnapshot.NoHorizon : horizon;
    }
}
