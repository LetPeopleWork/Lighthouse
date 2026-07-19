using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkItemDto
    {
        public WorkItemDto(WorkItemBase workItem, bool isBlocked)
            : this(workItem, isBlocked, [], null)
        {
        }

        public WorkItemDto(WorkItemBase workItem, bool isBlocked, IReadOnlyList<NamedCycleTimeValue> namedCycleTimes)
            : this(workItem, isBlocked, namedCycleTimes, null)
        {
        }

        /// <param name="asOf">
        /// D16: when supplied, <see cref="WorkItemAge"/> reports the age the item had on that day
        /// instead of today. Only the /wip endpoints pass it — they already receive an asOfDate from
        /// the caller and simply discarded it, which is why the aging chart's dot heights stayed
        /// today-anchored while the percentile card moved. Every other construction site is unchanged
        /// by omission, which bounds the blast radius to that one call path.
        /// </param>
        public WorkItemDto(WorkItemBase workItem, bool isBlocked, IReadOnlyList<NamedCycleTimeValue> namedCycleTimes, DateTime? blockedSince, DateTime? asOf = null)
        {
            Name = workItem.Name;
            Id = workItem.Id;
            ReferenceId = workItem.ReferenceId;
            ParentWorkItemReference = workItem.ParentReferenceId;
            Url = workItem.Url;
            Type = workItem.Type;
            State = workItem.State;
            StateCategory = workItem.StateCategory;
            StartedDate = workItem.StartedDate;
            ClosedDate = workItem.ClosedDate;
            CycleTime = workItem.CycleTime;
            NamedCycleTimes = namedCycleTimes;
            WorkItemAge = asOf.HasValue ? workItem.AgeOnDay(asOf.Value) : workItem.WorkItemAge;
            IsBlocked = isBlocked;
            CurrentStateEnteredAt = workItem.CurrentStateEnteredAt;
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
