using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class WorkItemBase : IEntity
    {
        public WorkItemBase()
        {
        }

        public WorkItemBase(WorkItemBase workItemBase)
        {
            Update(workItemBase);
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

        public virtual bool IsBlocked { get; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? StartedDate { get; set; }

        public DateTime? ClosedDate { get; set; }

        public int CycleTime
        {
            get
            {
                if (StateCategory == StateCategories.Done)
                {
                    var startingReferenceDate = StartedDate ?? CreatedDate;
                    if (ClosedDate?.Date >= startingReferenceDate?.Date)
                    {
                        return GetDateDifference(startingReferenceDate.Value, ClosedDate.Value);
                    }

                    // Item is closed, but something is wrong with the Closed Date --> Default to 1
                    return 1;
                }

                return 0;
            }
        }

        public int WorkItemAge
        {
            get
            {
                if (StateCategory == StateCategories.Doing)
                {
                    var referencedDate = StartedDate ?? CreatedDate;
                    if (referencedDate?.Date <= DateTime.UtcNow.Date)
                    {
                        return GetDateDifference(referencedDate.Value, DateTime.UtcNow);
                    }

                    // Item is in progress,  but started date is in the future or not set --> Default to 1
                    return 1;
                }

                return 0;
            }
        }

        private static int GetDateDifference(DateTime start, DateTime end)
        {
            return ((int)(end.Date - start.Date).TotalDays) + 1;
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
        }
    }
}
