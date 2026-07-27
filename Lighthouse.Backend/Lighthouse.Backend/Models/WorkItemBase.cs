using Lighthouse.Backend.Services.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lighthouse.Backend.Models
{
    public class WorkItemBase : IEntity
    {
        public WorkItemBase()
        {
        }

        protected WorkItemBase(WorkItemBase workItemBase)
        {
            Update(workItemBase);
            SyncedTransitions = workItemBase.SyncedTransitions;
        }

        public int Id { get; set; }

        public string ReferenceId { get; set; } = string.Empty;

        public string ParentReferenceId { get; set; } = string.Empty;

        public string Name { get; set; }

        public string Type { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = [];

        public StateCategories StateCategory { get; set; } = StateCategories.Unknown;

        public string? Url { get; set; }

        public string Order { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? StartedDate { get; set; }

        public DateTime? ClosedDate { get; set; }

        public DateTime? CurrentStateEnteredAt { get; set; }

        public Dictionary<int, string?> AdditionalFieldValues { get; set; } = new();

        [NotMapped]
        public IReadOnlyList<WorkItemStateTransition> SyncedTransitions { get; init; } = [];

        /// <param name="zone">
        /// Bug #5567: both ends are stored instants, so both are reduced to a calendar day in the
        /// instance zone. Reducing only one relocates the off-by-one instead of removing it.
        /// </param>
        public int CycleTime(TimeZoneInfo zone)
        {
            if (StateCategory == StateCategories.Done)
            {
                var startingReferenceDate = StartedDate ?? CreatedDate;
                var startDay = InstanceDayOrNull(startingReferenceDate, zone);
                var closedDay = InstanceDayOrNull(ClosedDate, zone);

                if (closedDay >= startDay)
                {
                    return GetDateDifference(startDay.Value, closedDay.Value);
                }

                // Item is closed, but something is wrong with the Closed Date --> Default to 1
                return 1;
            }

            return 0;
        }

        /// <param name="zone">
        /// Bug #5567: reduces the stored start instant to the same calendar day
        /// <paramref name="today"/> already speaks.
        /// </param>
        /// <param name="today">
        /// Bug #5567 decision 3: the instance's calendar day, supplied by the caller. The inclusive
        /// +1 is unchanged - this is a zone shift, not an arithmetic change.
        /// </param>
        public int WorkItemAge(TimeZoneInfo zone, DateOnly today)
        {
            if (StateCategory == StateCategories.Doing)
            {
                var startDay = InstanceDayOrNull(StartedDate ?? CreatedDate, zone);
                if (startDay <= today)
                {
                    return GetDateDifference(startDay.Value, today);
                }

                // Item is in progress,  but started date is in the future or not set --> Default to 1
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// How old was this item on the given day?
        /// </summary>
        /// <remarks>
        /// Deliberately NOT the same function as the <see cref="WorkItemAge"/> property above
        /// (DESIGN D13): <see cref="WorkItemAge"/> is today-anchored and guarded on
        /// <see cref="StateCategories.Doing"/> because work-tracking write-back consumes it.
        /// AgeOnDay carries no state guard — its callers establish the population via
        /// WasItemProgressOnDay before projecting. Do not refactor one into the other; they encode
        /// different questions, and repointing the property would change what Lighthouse writes
        /// back into Jira/ADO.
        ///
        /// The arithmetic mirrors BaseMetricsService.GenerateTotalWorkItemAgeByDay, which is the
        /// already-trusted definition of "age on a day" and drives the over-time chart. Slice 03
        /// asserts parity against it so a second definition cannot drift into existence.
        /// </remarks>
        public int AgeOnDay(TimeZoneInfo zone, DateOnly day)
        {
            var startDay = InstanceDayOrNull(StartedDate ?? CreatedDate, zone);
            if (startDay is null || startDay > day)
            {
                return 0;
            }

            return GetDateDifference(startDay.Value, day);
        }

        private static DateOnly? InstanceDayOrNull(DateTime? instant, TimeZoneInfo zone)
        {
            return instant.HasValue ? InstanceCalendar.DayOf(instant.Value, zone) : null;
        }

        private static int GetDateDifference(DateOnly start, DateOnly end)
        {
            return (end.DayNumber - start.DayNumber) + 1;
        }

        internal void Update(WorkItemBase workItemBase)
        {
            ReferenceId = workItemBase.ReferenceId;
            ParentReferenceId = workItemBase.ParentReferenceId;
            Name = workItemBase.Name;
            Type = workItemBase.Type;
            State = workItemBase.State;
            StateCategory = workItemBase.StateCategory;
            Url = workItemBase.Url;
            Order = workItemBase.Order;
            CreatedDate = workItemBase.CreatedDate;
            StartedDate = workItemBase.StartedDate;
            ClosedDate = workItemBase.ClosedDate;
            Tags = workItemBase.Tags;

            // Copy additional field values
            AdditionalFieldValues.Clear();
            foreach (var kvp in workItemBase.AdditionalFieldValues)
            {
                AdditionalFieldValues[kvp.Key] = kvp.Value;
            }
        }
    }
}
