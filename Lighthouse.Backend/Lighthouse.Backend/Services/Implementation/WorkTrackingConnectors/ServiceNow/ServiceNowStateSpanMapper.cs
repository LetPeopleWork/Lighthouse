using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // The pure core of slice 04 (ADR-118, ADR-114's shape): spans in, transitions out, no HTTP.
    public static class ServiceNowStateSpanMapper
    {
        /// <summary>
        /// Pairs a record's consecutive spans into the transitions Lighthouse models, mapped through
        /// the same <see cref="WorkItemStateTransitionMapper"/> every other connector uses (US-04 AC2).
        /// </summary>
        /// <remarks>
        /// The earliest span yields no transition. Spans begin only when the metric definition was
        /// activated, so a record that predates it carries partial history and its first observed
        /// label is not necessarily the state it was created in. Inventing a transition from creation
        /// into that label would assert a state the record may never have held — so the first span is
        /// an arrival Lighthouse did not witness, and it stays unreported.
        /// </remarks>
        public static IReadOnlyList<WorkItemStateTransition> ToTransitions(
            IReadOnlyList<ServiceNowStateSpan> spans, IWorkItemQueryOwner owner)
        {
            return WorkItemStateTransitionMapper.MapToMappedStates(PairConsecutive(InStartOrder(spans)), owner);
        }

        /// <summary>
        /// When work actually began: the start of the earliest span whose label the team maps to Doing
        /// (ADR-118 decision 7). Null when no such span exists — a record that never reached Doing has
        /// not started, and saying otherwise would put unstarted work into Cycle Time.
        /// </summary>
        public static DateTime? WhenWorkStarted(IReadOnlyList<ServiceNowStateSpan> spans, IWorkItemQueryOwner owner)
        {
            var arrivalInDoing = InStartOrder(spans)
                .Find(span => owner.MapStateToStateCategory(span.Label) == StateCategories.Doing);

            return arrivalInDoing?.Start;
        }

        private static List<ServiceNowStateSpan> InStartOrder(IReadOnlyList<ServiceNowStateSpan> spans)
        {
            return [.. spans.OrderBy(span => span.Start)];
        }

        private static List<WorkItemStateTransition> PairConsecutive(List<ServiceNowStateSpan> spansInStartOrder)
        {
            var transitions = new List<WorkItemStateTransition>();

            for (var arrival = 1; arrival < spansInStartOrder.Count; arrival++)
            {
                transitions.Add(new WorkItemStateTransition
                {
                    FromState = spansInStartOrder[arrival - 1].Label,
                    ToState = spansInStartOrder[arrival].Label,

                    // ADR-118 decision 1: dated by the arrival, never the departure.
                    TransitionedAt = spansInStartOrder[arrival].Start,
                });
            }

            return transitions;
        }
    }
}
