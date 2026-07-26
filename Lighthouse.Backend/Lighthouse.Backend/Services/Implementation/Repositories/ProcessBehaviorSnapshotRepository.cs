using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class ProcessBehaviorSnapshotRepository(
        LighthouseAppContext context,
        ILogger<ProcessBehaviorSnapshotRepository> logger)
        : RepositoryBase<ProcessBehaviorSnapshot>(context, (lighthouseAppContext) => lighthouseAppContext.ProcessBehaviorSnapshots, logger),
            IProcessBehaviorSnapshotRepository
    {
    }
}
