using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class ProcessBehaviorSeriesQuery(IProcessBehaviorSnapshotRepository repository) : IProcessBehaviorSeriesQuery
    {
        public IReadOnlyList<ProcessBehaviorSnapshot> GetSeries(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType metricType)
        {
            return repository
                .GetAllByPredicate(snapshot =>
                    snapshot.OwnerId == ownerId &&
                    snapshot.OwnerType == ownerType &&
                    snapshot.MetricType == metricType)
                .OrderBy(snapshot => snapshot.RecordedAt)
                .ToList();
        }
    }
}
