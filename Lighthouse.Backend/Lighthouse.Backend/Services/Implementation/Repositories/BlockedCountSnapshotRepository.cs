using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class BlockedCountSnapshotRepository(
        LighthouseAppContext context,
        ILogger<BlockedCountSnapshotRepository> logger)
        : RepositoryBase<BlockedCountSnapshot>(context, (lighthouseAppContext) => lighthouseAppContext.BlockedCountSnapshots, logger),
            IBlockedCountSnapshotRepository
    {
        public BlockedCountSnapshot? GetLatestAtOrBefore(int ownerId, OwnerType ownerType, DateOnly date)
        {
            return GetAllByPredicate(s => s.OwnerId == ownerId && s.OwnerType == ownerType && s.RecordedAt <= date)
                .OrderByDescending(s => s.RecordedAt)
                .FirstOrDefault();
        }
    }
}
