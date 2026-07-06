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
    }
}
