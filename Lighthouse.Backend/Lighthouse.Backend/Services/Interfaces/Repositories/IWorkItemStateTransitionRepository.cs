using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IWorkItemStateTransitionRepository : IRepository<WorkItemStateTransition>
    {
        /// <summary>Every recorded state transition of these work items, tracked. One statement however many ids are asked for.</summary>
        IReadOnlyList<WorkItemStateTransition> TransitionsOfWorkItems(IReadOnlyCollection<int> workItemIds);
    }
}
