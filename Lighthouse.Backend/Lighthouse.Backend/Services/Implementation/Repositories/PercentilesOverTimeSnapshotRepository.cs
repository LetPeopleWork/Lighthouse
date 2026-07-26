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
        public IReadOnlyList<PercentilesOverTimeSnapshot> GetSeries(int ownerId, OwnerType ownerType, MetricType metricType, int? horizon, DateOnly? from, DateOnly? to)
        {
            var series = GetAllByPredicate(s =>
                s.OwnerId == ownerId &&
                s.OwnerType == ownerType &&
                s.MetricType == metricType &&
                s.Horizon == horizon);

            // Composed onto the IQueryable rather than folded into the predicate above, so an omitted
            // bound adds no SQL at all and a supplied one is applied by the database, never in memory.
            if (from.HasValue)
            {
                series = series.Where(s => s.RecordedAt >= from.Value);
            }

            if (to.HasValue)
            {
                series = series.Where(s => s.RecordedAt <= to.Value);
            }

            return series
                .OrderBy(s => s.RecordedAt)
                .ToList();
        }
    }
}
