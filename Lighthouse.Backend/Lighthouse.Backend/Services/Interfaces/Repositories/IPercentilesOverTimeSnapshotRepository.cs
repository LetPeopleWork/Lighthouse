using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IPercentilesOverTimeSnapshotRepository : IRepository<PercentilesOverTimeSnapshot>
    {
        IReadOnlyList<PercentilesOverTimeSnapshot> GetSeries(int ownerId, OwnerType ownerType, MetricType metricType, int? horizon);
    }
}
