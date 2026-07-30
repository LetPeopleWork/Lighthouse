using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // SCAFFOLD (DISTILL slice 04, Story #5577)
    //
    // The pure core of slice 04 (ADR-118, ADR-114's shape): spans in, transitions out, no HTTP.
    public static class ServiceNowStateSpanMapper
    {
        private const string ScaffoldSentinel = "__scaffold__";

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
            return
            [
                new WorkItemStateTransition
                {
                    FromState = ScaffoldSentinel,
                    ToState = ScaffoldSentinel,
                    TransitionedAt = DateTime.UnixEpoch,
                },
            ];
        }

        /// <summary>
        /// When work actually began: the start of the earliest span whose label the team maps to Doing
        /// (ADR-118 decision 7). Null when no such span exists — a record that never reached Doing has
        /// not started, and saying otherwise would put unstarted work into Cycle Time.
        /// </summary>
        public static DateTime? WhenWorkStarted(IReadOnlyList<ServiceNowStateSpan> spans, IWorkItemQueryOwner owner)
        {
            return DateTime.UnixEpoch;
        }
    }
}
