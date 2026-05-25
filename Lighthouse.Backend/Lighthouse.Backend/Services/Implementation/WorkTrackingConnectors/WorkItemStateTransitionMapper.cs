using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors
{
    public static class WorkItemStateTransitionMapper
    {
        public static IReadOnlyList<WorkItemStateTransition> MapToMappedStates(
            IReadOnlyList<WorkItemStateTransition> rawTransitions, IWorkItemQueryOwner workItemQueryOwner)
        {
            return rawTransitions
                .Select(transition => new WorkItemStateTransition
                {
                    FromState = workItemQueryOwner.MapRawStateToMappedName(transition.FromState),
                    ToState = workItemQueryOwner.MapRawStateToMappedName(transition.ToState),
                    TransitionedAt = transition.TransitionedAt,
                })
                .Where(transition => !string.Equals(transition.FromState, transition.ToState, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
