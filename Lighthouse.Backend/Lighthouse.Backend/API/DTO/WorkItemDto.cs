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
            StartedDate = workItem.StartedDate;
            ClosedDate = workItem.ClosedDate;
        }

        public string Name { get; set; }

        public int Id { get; set; }

        public string WorkItemReference { get; set; }

        public string Url { get; set; }

        public DateTime? StartedDate { get; set; }

        public DateTime? ClosedDate { get; set; }
    }
}
