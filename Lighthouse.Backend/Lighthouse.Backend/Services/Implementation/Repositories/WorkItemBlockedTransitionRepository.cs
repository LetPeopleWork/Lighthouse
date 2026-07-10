using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkItemBlockedTransitionRepository(LighthouseAppContext context, ILogger<WorkItemBlockedTransitionRepository> logger)
        : RepositoryBase<WorkItemBlockedTransition>(context, (lighthouseAppContext) => lighthouseAppContext.WorkItemBlockedTransitions, logger), IWorkItemBlockedTransitionRepository
    {
        public IReadOnlyList<int> GetBlockedWorkItemIdsAt(DateOnly date)
        {
            var startOfDate = date.ToDateTime(TimeOnly.MinValue);
            var startOfNextDate = startOfDate.AddDays(1);

            return GetAllByPredicate(transition =>
                    transition.EnteredAt < startOfNextDate &&
                    (transition.LeftAt == null || transition.LeftAt >= startOfDate))
                .Select(transition => transition.WorkItemId)
                .Distinct()
                .ToList();
        }
    }
}
