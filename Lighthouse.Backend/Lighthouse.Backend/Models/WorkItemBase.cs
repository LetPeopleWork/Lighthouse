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
            Id = workItemBase.Id;
            ReferenceId = workItemBase.ReferenceId;
            Name = workItemBase.Name;
            Type = workItemBase.Type;
            State = workItemBase.State;
            StateCategory = workItemBase.StateCategory;
            Url = workItemBase.Url;
            Order = workItemBase.Order;
            StartedDate = workItemBase.StartedDate;
            ClosedDate = workItemBase.ClosedDate;
        }

        public int Id { get; set; }

        public string ReferenceId { get; set; } = string.Empty;

        public string Name { get; set; }

        public string Type { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public StateCategories StateCategory { get; set; } = StateCategories.Unknown;

        public string? Url { get; set; }

        public string Order { get; set; }

        public DateTime? StartedDate { get; set; }

        public DateTime? ClosedDate { get; set; }
    }
}
