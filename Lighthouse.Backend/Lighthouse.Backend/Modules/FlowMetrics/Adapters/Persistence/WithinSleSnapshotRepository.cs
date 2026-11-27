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
            throw new NotImplementedException("Reading the stored Within SLE record is not built yet.");
        }
    }
}
