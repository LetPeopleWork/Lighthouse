using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class PercentilesOverTimeSnapshotRepository(
        LighthouseAppContext context,
        ILogger<PercentilesOverTimeSnapshotRepository> logger)
        : RepositoryBase<PercentilesOverTimeSnapshot>(context, (lighthouseAppContext) => lighthouseAppContext.PercentilesOverTimeSnapshots, logger),
            IPercentilesOverTimeSnapshotRepository
    {
        public IReadOnlyList<PercentilesOverTimeSnapshot> GetSeries(int ownerId, OwnerType ownerType, MetricType metricType, int? horizon)
        {
            return GetAllByPredicate(s =>
                    s.OwnerId == ownerId &&
                    s.OwnerType == ownerType &&
                    s.MetricType == metricType &&
                    s.Horizon == horizon)
                .OrderBy(s => s.RecordedAt)
                .ToList();
        }
    }
}
