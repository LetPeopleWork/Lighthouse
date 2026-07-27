using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkItemDto
    {
        public WorkItemDto(WorkItemBase workItem, DateOnly today, bool isBlocked)
            : this(workItem, today, isBlocked, [], null)
        {
        }

        public WorkItemDto(WorkItemBase workItem, DateOnly today, bool isBlocked, IReadOnlyList<NamedCycleTimeValue> namedCycleTimes)
            : this(workItem, today, isBlocked, namedCycleTimes, null)
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
        /// <param name="today">
        /// Bug #5567: the calendar day <see cref="WorkItemAge"/> is measured against when no
        /// <paramref name="asOf"/> is supplied. The entity no longer reads an ambient clock.
        /// </param>
#pragma warning disable S107 // One more collaborator than the S107 threshold, and it is a value, not a dependency: WorkItemBase.WorkItemAge stopped reading the ambient clock (bug #5567) so the day has to arrive with the item. Grouping the flags into a record would hide that.
        public WorkItemDto(WorkItemBase workItem, DateOnly today, bool isBlocked, IReadOnlyList<NamedCycleTimeValue> namedCycleTimes, DateTime? blockedSince, DateTime? asOf = null, StateAsOf? stateAsOf = null)
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
            CycleTime = workItem.CycleTime;
            NamedCycleTimes = namedCycleTimes;
            WorkItemAge = asOf.HasValue ? workItem.AgeOnDay(asOf.Value) : workItem.WorkItemAge(today);
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
