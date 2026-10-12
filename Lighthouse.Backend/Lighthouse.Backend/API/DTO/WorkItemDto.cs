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
        /// When supplied, <see cref="WorkItemAge"/> reports the age the item had on that day
        /// instead of today. The /wip endpoints pass the day the caller asked about, so the aging
        /// chart's dot heights move with the chosen range the way its percentile card does.
        /// </param>
        /// <param name="stateAsOf">
        /// The state the item held on <paramref name="asOf"/>, for callers that cannot
        /// hand over an already-projected entity. Teams project onto a WorkItem copy instead; a
        /// Feature cannot be copied without losing the forecast/work/portfolio data this DTO reads,
        /// and the entity is EF-tracked, so the portfolio path passes the projection in here.
        /// Omitted means "no history for that day" — the item's current state stands.
        /// </param>
        /// <param name="clock">
        /// Bug #5567: the instance calendar. The entity no longer reads an ambient clock, so this
        /// mapper hands it the day and the zone together and the two can never disagree.
        /// </param>
#pragma warning disable S107 // Bug #5567: the instance calendar has to arrive with the item, taking this one past the threshold. Grouping the projection flags into a record would hide which ones the aging surfaces depend on.
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

        /// <summary>
        /// Null when the owner has no estimation field, or the row has no single owner to ask.
        /// </summary>
        public WorkItemEstimateDto? Estimate { get; init; }
    }
}
