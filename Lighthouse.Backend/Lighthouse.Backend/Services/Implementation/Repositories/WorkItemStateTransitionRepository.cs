using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkItemStateTransitionRepository(LighthouseAppContext context, ILogger<WorkItemStateTransitionRepository> logger)
        : RepositoryBase<WorkItemStateTransition>(context, (lighthouseAppContext) => lighthouseAppContext.WorkItemStateTransitions, logger), IWorkItemStateTransitionRepository
    {
    }
}
