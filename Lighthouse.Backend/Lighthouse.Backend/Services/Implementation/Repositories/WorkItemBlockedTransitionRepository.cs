using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkItemBlockedTransitionRepository(LighthouseAppContext context, ILogger<WorkItemBlockedTransitionRepository> logger)
        : RepositoryBase<WorkItemBlockedTransition>(context, (lighthouseAppContext) => lighthouseAppContext.WorkItemBlockedTransitions, logger), IWorkItemBlockedTransitionRepository
    {
    }
}
