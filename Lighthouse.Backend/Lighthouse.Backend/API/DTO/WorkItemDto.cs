using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkItemDto
    {
        public WorkItemDto(WorkItemBase workItem, ILighthouseClock clock, bool isBlocked)
            : this(workItem, clock, isBlocked, [], null)
        {
        }

        public WorkItemDto(WorkItemBase workItem, ILighthouseClock clock, bool isBlocked, IReadOnlyList<NamedCycleTimeValue> namedCycleTimes)
            : this(workItem, clock, isBlocked, namedCycleTimes, null)
        {
        }

        /// <param name="asOf">
        /// D16: when supplied, <see cref="WorkItemAge"/> reports the age the item had on that day
        /// instead of today. Only the /wip endpoints pass it — they already receive an asOfDate from
        /// the caller and simply discarded it, which is why the aging chart's dot heights stayed
        /// today-anchored while the percentile card moved. Every other construction site is unchanged
        /// by omission, which bounds the blast radius to that one call path.
        /// </param>
        /// <param name="stateAsOf">
        /// UPSTREAM-7: the state the item held on <paramref name="asOf"/>, for callers that cannot
        /// hand over an already-projected entity. Teams project onto a WorkItem copy instead; a
        /// Feature cannot be copied without losing the forecast/work/portfolio data this DTO reads,
        /// and the entity is EF-tracked, so the portfolio path passes the projection in here.
        /// Omitted means "no history for that day" — the item's current state stands.
        /// </param>
        /// <param name="clock">
        /// Bug #5567: the instance calendar - the day <see cref="WorkItemAge"/> is measured against
        /// when no <paramref name="asOf"/> is supplied, and the zone both age and cycle time reduce
        /// their stored instants in. The entity no longer reads an ambient clock; this adapter-layer
        /// mapper hands it the day and the zone together so the two can never disagree.
        /// </param>
#pragma warning disable S107 // One more collaborator than the S107 threshold: WorkItemBase stopped reading the ambient clock (bug #5567) so the instance calendar has to arrive with the item. Grouping the flags into a record would hide that.
        public WorkItemDto(WorkItemBase workItem, ILighthouseClock clock, bool isBlocked, IReadOnlyList<NamedCycleTimeValue> namedCycleTimes, DateTime? blockedSince, DateTime? asOf = null, StateAsOf? stateAsOf = null)
#pragma warning restore S107
        {
            Name = workItem.Name;
            Id = workItem.Id;
            ReferenceId = workItem.ReferenceId;
            ParentWorkItemReference = workItem.ParentReferenceId;
            Url = workItem.Url;
            Type = workItem.Type;
            State = stateAsOf?.State ?? workItem.State;
            StateCategory = stateAsOf?.StateCategory ?? workItem.StateCategory;
            StartedDate = workItem.StartedDate;
            ClosedDate = workItem.ClosedDate;
            CycleTime = workItem.CycleTime(clock.Zone);
            NamedCycleTimes = namedCycleTimes;
            WorkItemAge = asOf.HasValue
                ? workItem.AgeOnDay(clock.Zone, DateOnly.FromDateTime(asOf.Value))
                : workItem.WorkItemAge(clock.Zone, clock.Today);
            IsBlocked = isBlocked;
            CurrentStateEnteredAt = stateAsOf?.EnteredAt ?? workItem.CurrentStateEnteredAt;
            BlockedSince = blockedSince;
            Approximate = false;
        }

        public string Name { get; }

        public int Id { get; }

        public string ReferenceId { get; }

        public string ParentWorkItemReference { get; }

        public string Url { get; }

        public string Type { get; }

        public string State { get; }

        public bool IsBlocked { get; }

        public DateTime? BlockedSince { get; }

        public StateCategories StateCategory { get; }

        public int CycleTime { get; }

        public IReadOnlyList<NamedCycleTimeValue> NamedCycleTimes { get; }

        public int WorkItemAge { get; }

        public DateTime? StartedDate { get; }

        public DateTime? ClosedDate { get; }

        public DateTime? CurrentStateEnteredAt { get; }

        public bool Approximate { get; }
    }
}
