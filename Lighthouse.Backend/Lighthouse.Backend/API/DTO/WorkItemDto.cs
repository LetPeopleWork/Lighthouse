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
            Url = workItem.Url;
            Type = workItem.Type;
            State = workItem.State;
            StartedDate = workItem.StartedDate;
            ClosedDate = workItem.ClosedDate;
            CycleTime = workItem.CycleTime;
        }

        public string Name { get; }

        public int Id { get; }

        public string WorkItemReference { get; }

        public string Url { get; }

        public string Type { get; }

        public string State { get; }

        public int CycleTime { get; }

        public DateTime? StartedDate { get; }

        public DateTime? ClosedDate { get; }
    }
}
