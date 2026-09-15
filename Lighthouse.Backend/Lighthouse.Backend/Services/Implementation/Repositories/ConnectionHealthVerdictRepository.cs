using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.ConnectionHealth;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class ConnectionHealthVerdictRepository(LighthouseAppContext context, ILogger<ConnectionHealthVerdictRepository> logger)
        : RepositoryBase<ConnectionHealthVerdict>(context, ctx => ctx.ConnectionHealthVerdicts, logger);
}
