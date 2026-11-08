using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public class ProcessBehaviorSeriesQuery(IProcessBehaviorSnapshotRepository repository) : IProcessBehaviorSeriesQuery
    {
        public IReadOnlyList<ProcessBehaviorSnapshot> GetSeries(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType metricType, DateOnly? from, DateOnly? to)
            => repository.GetSeries(ownerId, ownerType, metricType, from, to);
    }
}
