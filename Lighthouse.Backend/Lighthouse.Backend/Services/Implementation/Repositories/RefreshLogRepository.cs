using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class RefreshLogRepository(LighthouseAppContext context, ILogger<RefreshLogRepository> logger)
        : RepositoryBase<RefreshLog>(context, ctx => ctx.RefreshLogs, logger);
}
