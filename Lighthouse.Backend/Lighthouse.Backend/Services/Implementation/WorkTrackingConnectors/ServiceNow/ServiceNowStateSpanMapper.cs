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
            return WorkItemStateTransitionMapper.MapToMappedStates(
                PairConsecutive(InStartOrder(TheTeamRecognisesAsState(spans, owner))), owner);
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

        /// <summary>
        /// When work finished: the start of the latest span whose label the team maps to Done
        /// (ADR-117 decision 1, amended 2026-07-31). Null when no such span exists.
        /// </summary>
        /// <remarks>
        /// The latest arrival, where <see cref="WhenWorkStarted"/> takes the earliest. A record
        /// resolved, reopened and resolved again finished on the second arrival — the first one was
        /// undone — while the work itself began the first time it reached Doing, and rework must not
        /// restart that clock.
        /// </remarks>
        public static DateTime? WhenWorkFinished(IReadOnlyList<ServiceNowStateSpan> spans, IWorkItemQueryOwner owner)
        {
            var arrivalInDone = InStartOrder(spans)
                .FindLast(span => owner.MapStateToStateCategory(span.Label) == StateCategories.Done);

            return arrivalInDone?.Start;
        }

        // A `field_value_duration` definition is not necessarily a definition on the state field:
        // the stock incident table also measures `active` and `assignment_group` that way, which
        // would pair `true` against a group name and report it as a move. A label the team never
        // mapped is not a state. Amends ADR-118 D2, which discriminated on the definition type alone.
        private static List<ServiceNowStateSpan> TheTeamRecognisesAsState(
            IReadOnlyList<ServiceNowStateSpan> spans, IWorkItemQueryOwner owner)
        {
            return [.. spans.Where(span => owner.MapStateToStateCategory(span.Label) != StateCategories.Unknown)];
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
