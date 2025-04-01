using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class WorkItemBase : IEntity
    {
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
