using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IBlockedCountSnapshotRepository : IRepository<BlockedCountSnapshot>
    {
        BlockedCountSnapshot? GetLatestAtOrBefore(int ownerId, OwnerType ownerType, DateOnly date);
    }
}
