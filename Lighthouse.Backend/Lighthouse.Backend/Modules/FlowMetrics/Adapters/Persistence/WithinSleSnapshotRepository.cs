using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence
{
    public class WithinSleSnapshotRepository(
        LighthouseAppContext context,
        ILogger<WithinSleSnapshotRepository> logger)
        : RepositoryBase<WithinSleSnapshot>(context, (lighthouseAppContext) => lighthouseAppContext.WithinSleSnapshots, logger),
            IWithinSleSnapshotRepository
    {
        public IReadOnlyList<WithinSleSnapshot> GetSeries(int ownerId, OwnerType ownerType, int horizon, DateOnly? from, DateOnly? to)
        {
            var series = GetAllByPredicate(s =>
                s.OwnerId == ownerId &&
                s.OwnerType == ownerType &&
                s.Horizon == horizon);

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
