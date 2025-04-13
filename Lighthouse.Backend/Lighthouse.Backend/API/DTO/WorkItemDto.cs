using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkItemDto
    {
        public WorkItemDto(WorkItemBase workItem)
        {
            Name = workItem.Name;
            Id = workItem.Id;
            WorkItemReference = workItem.ReferenceId;
            ParentWorkItemReference = workItem.ParentReferenceId;
            Url = workItem.Url;
            Type = workItem.Type;
            State = workItem.State;
            StateCategory = workItem.StateCategory;
            StartedDate = workItem.StartedDate;
            ClosedDate = workItem.ClosedDate;
            CycleTime = workItem.CycleTime;
            WorkItemAge = workItem.WorkItemAge;
        }

        public string Name { get; }

        public int Id { get; }

        public string WorkItemReference { get; }

        public string ParentWorkItemReference { get; }

        public string Url { get; }

        public string Type { get; }

        public string State { get; }

        public StateCategories StateCategory { get; }

        public int CycleTime { get; }

        public int WorkItemAge { get; }

        public DateTime? StartedDate { get; }

        public DateTime? ClosedDate { get; }
    }
}
