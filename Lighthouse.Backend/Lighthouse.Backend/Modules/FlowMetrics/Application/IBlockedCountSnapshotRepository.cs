using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public interface IBlockedCountSnapshotRepository : IRepository<BlockedCountSnapshot>
    {
        BlockedCountSnapshot? GetLatestAtOrBefore(int ownerId, OwnerType ownerType, DateOnly date);
    }
}
