using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class BlackoutPeriodRepository(LighthouseAppContext context, ILogger<BlackoutPeriodRepository> logger)
        : RepositoryBase<BlackoutPeriod>(context, lighthouseAppContext => lighthouseAppContext.BlackoutPeriods, logger);
}
