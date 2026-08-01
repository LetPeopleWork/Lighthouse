using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors
{
    /// <summary>
    /// When a work item crossed <em>into</em> a category of states — the instant every connector dates
    /// started and finished work from.
    /// </summary>
    /// <remarks>
    /// Dates come from category boundaries and never from individual state changes: a move between two
    /// states the team maps to the same category has neither started nor finished anything, so
    /// <c>Resolved → Closed → Resolved</c> re-dates nothing while <c>Done → Doing → Done</c> does.
    /// Written once here because it had already been written twice — Jira and Azure DevOps — and a
    /// third connector reimplementing it from scratch is how the two halves of Bug #5621 F2 arrived.
    /// </remarks>
    public static class WorkItemCategoryCrossing
    {
        /// <summary>
        /// The instant of the last crossing into <paramref name="targetStates"/> from outside it, or
        /// <c>null</c> where no such crossing was observed.
        /// </summary>
        /// <param name="transitions">
        /// Every state change the item made. Order is irrelevant — the latest qualifying instant wins.
        /// A transition out of an empty <see cref="WorkItemStateTransition.FromState"/> counts, which is
        /// how an item first observed already inside the category is dated.
        /// </param>
        /// <param name="targetStates">The raw states of the category being entered.</param>
        /// <param name="statesToIgnore">
        /// Raw states an arrival must not have come from. Passing the Done states while asking about
        /// Doing is what stops a reopen from restarting the clock on work that had already begun.
        /// </param>
        public static DateTime? LastEntryInto(
            IEnumerable<WorkItemStateTransition> transitions,
            IEnumerable<string> targetStates,
            IEnumerable<string> statesToIgnore)
        {
            DateTime? lastEntry = null;

            foreach (var transition in transitions)
            {
                if (!IsAnEntry(transition, targetStates, statesToIgnore))
                {
                    continue;
                }

                if (lastEntry is null || transition.TransitionedAt > lastEntry)
                {
                    lastEntry = transition.TransitionedAt;
                }
            }

            return lastEntry is null ? null : DateTime.SpecifyKind(lastEntry.Value, DateTimeKind.Utc);
        }

        private static bool IsAnEntry(
            WorkItemStateTransition transition, IEnumerable<string> targetStates, IEnumerable<string> statesToIgnore)
        {
            return targetStates.IsItemInList(transition.ToState)
                && !targetStates.IsItemInList(transition.FromState)
                && !statesToIgnore.IsItemInList(transition.FromState);
        }
    }
}
