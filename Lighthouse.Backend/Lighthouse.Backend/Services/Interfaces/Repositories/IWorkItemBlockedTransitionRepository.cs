using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IWorkItemBlockedTransitionRepository : IRepository<WorkItemBlockedTransition>
    {
        IReadOnlyList<int> GetBlockedWorkItemIdsAt(DateOnly date);

        /// <summary>
        /// The blocked spells that were open on <paramref name="date"/>, carrying their EnteredAt so
        /// callers can report how long an item had been blocked as of that day (UPSTREAM-7).
        /// </summary>
        IReadOnlyList<WorkItemBlockedTransition> GetBlockedTransitionsAt(DateOnly date);

        /// <summary>
        /// Which of <paramref name="workItemIds"/> have any blocked history at all. Items with none
        /// predate blocked-history capture, so a historic read has nothing to answer from and must
        /// fall back to evaluating the blocked rule live (UPSTREAM-7).
        /// </summary>
        IReadOnlyList<int> GetWorkItemIdsWithBlockedHistory(IReadOnlyCollection<int> workItemIds);
    }
}
