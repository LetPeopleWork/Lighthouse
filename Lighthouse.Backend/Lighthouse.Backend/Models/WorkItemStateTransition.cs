using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class WorkItemStateTransition : IEntity
    {
        public int Id { get; set; }

        public int WorkItemId { get; set; }

        public string FromState { get; set; } = string.Empty;

        public string ToState { get; set; } = string.Empty;

        public DateTime TransitionedAt { get; set; }
    }
}
