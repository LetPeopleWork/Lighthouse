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
        /// When work actually began: the last time the record crossed into Doing from outside it,
        /// ignoring arrivals from Done (ADR-118 decision 7). Null when it never crossed — a record
        /// that never reached Doing has not started, and saying otherwise would put unstarted work
        /// into Cycle Time.
        /// </summary>
        /// <remarks>
        /// Ignoring arrivals from Done is what stops a reopen from restarting the clock on work that
        /// had already begun. A return to the QUEUE is the opposite case and deliberately does
        /// re-date: that work was un-started, so the attempt that counts is the one that stuck.
        /// </remarks>
        public static DateTime? WhenWorkStarted(IReadOnlyList<ServiceNowStateSpan> spans, IWorkItemQueryOwner owner)
        {
            return LastCrossingInto(spans, owner, owner.DoingStates, owner.DoneStates);
        }

        /// <summary>
        /// When work finished: the last time the record crossed into Done from outside it (ADR-117
        /// decision 1, amended 2026-07-31). Null when it never crossed.
        /// </summary>
        /// <remarks>
        /// From <em>outside</em> Done, which is the whole of Bug #5621 F2. A desk mapping both
        /// Resolved and Closed to Done finishes the work when someone resolves it; the instance's own
        /// close-resolved job moving it a week later crossed no boundary and undid nothing. Where a
        /// reopen genuinely did undo it, the return trip through Doing puts a real crossing after it.
        /// </remarks>
        public static DateTime? WhenWorkFinished(IReadOnlyList<ServiceNowStateSpan> spans, IWorkItemQueryOwner owner)
        {
            return LastCrossingInto(spans, owner, owner.DoneStates, []);
        }

        /// <summary>
        /// When the record was last pushed back into the queue, ignoring arrivals from Done. Null
        /// where it never was.
        /// </summary>
        /// <remarks>
        /// The caller compares this against <see cref="WhenWorkStarted"/>: work returned to the queue
        /// after it started has not started, and a start date older than the return would report
        /// queue time as work. A reopen passes back through Doing on its way and is rework rather
        /// than a return, which is why arrivals from Done are ignored here too.
        /// </remarks>
        public static DateTime? WhenWorkWasQueued(IReadOnlyList<ServiceNowStateSpan> spans, IWorkItemQueryOwner owner)
        {
            return LastCrossingInto(spans, owner, owner.ToDoStates, owner.DoneStates);
        }

        private static DateTime? LastCrossingInto(
            IReadOnlyList<ServiceNowStateSpan> spans,
            IWorkItemQueryOwner owner,
            List<string> category,
            List<string> categoryToIgnoreArrivalsFrom)
        {
            return WorkItemCategoryCrossing.LastEntryInto(
                ArrivalsIn(InStartOrder(TheTeamRecognisesAsState(spans, owner))),
                owner.GetRawStatesForCategory(category),
                owner.GetRawStatesForCategory(categoryToIgnoreArrivalsFrom));
        }

        // Every arrival the spans record, the earliest coming from nowhere — the same shape Azure
        // DevOps' revision walk produces. Not PairConsecutive, which drops the first span rather than
        // assert a predecessor the record may never have held; for dating, that arrival was still
        // witnessed, and a record whose spans begin after the definition was activated would
        // otherwise lose every date it has.
        //
        // Unmapped spans are dropped BEFORE pairing, so a detour through a state the team never
        // mapped joins the spans either side of it and re-dates nothing. Deliberate: an unmapped
        // state does not exist to Lighthouse, so the occupancy either side reads as continuous.
        private static List<WorkItemStateTransition> ArrivalsIn(List<ServiceNowStateSpan> spansInStartOrder)
        {
            return [.. spansInStartOrder.Select((span, arrival) => new WorkItemStateTransition
            {
                FromState = arrival == 0 ? string.Empty : spansInStartOrder[arrival - 1].Label,
                ToState = span.Label,
                TransitionedAt = span.Start,
            })];
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
