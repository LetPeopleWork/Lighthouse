using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class RecurringBlackoutRuleRepository(LighthouseAppContext context, ILogger<RecurringBlackoutRuleRepository> logger)
        : RepositoryBase<RecurringBlackoutRule>(context, lighthouseAppContext => lighthouseAppContext.RecurringBlackoutRules, logger);
}
