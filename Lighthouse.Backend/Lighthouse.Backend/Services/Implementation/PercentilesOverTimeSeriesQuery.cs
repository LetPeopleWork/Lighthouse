using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class PercentilesOverTimeSeriesQuery(IPercentilesOverTimeSnapshotRepository repository) : IPercentilesOverTimeSeriesQuery
    {
        public IReadOnlyList<PercentilesOverTimeSnapshot> GetSeries(int ownerId, OwnerType ownerType, MetricType metricType, int? horizon)
            => repository.GetSeries(ownerId, ownerType, metricType, horizon);
    }
}
