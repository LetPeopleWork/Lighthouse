using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class WorkItemBlockedTransition : IEntity
    {
        public int Id { get; set; }
        public int WorkItemId { get; set; }
        public DateTime EnteredAt { get; set; }
        public DateTime? LeftAt { get; set; }
    }
}
