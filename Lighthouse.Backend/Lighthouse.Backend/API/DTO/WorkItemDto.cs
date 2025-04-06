using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkItemDto
    {
        private WorkItemDto()
        {
            Name = string.Empty;
            WorkItemReference = string.Empty;
            Url = string.Empty;
            Type = string.Empty;
            State = string.Empty;
        }

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
        }

        public string Name { get; set; }

        public int Id { get; set; }

        public string WorkItemReference { get; set; }

        public string Url { get; set; }

        public string Type { get; set; }

        public string State { get; set; }

        public DateTime? StartedDate { get; set; }

        public DateTime? ClosedDate { get; set; }

        public static WorkItemDto CreateUnknownWorkItemDto(string referenceId)
        {
            var workItemDto = new WorkItemDto
            {
                Id = -1,
                WorkItemReference = referenceId,
                Name = $"{referenceId} (Item not tracked by Lighthouse)",
                StartedDate = null,
                ClosedDate = null,
                Url = string.Empty
            };

            return workItemDto;
        }
    }
}
