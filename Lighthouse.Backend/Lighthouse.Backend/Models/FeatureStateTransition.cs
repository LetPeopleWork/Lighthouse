using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class FeatureStateTransition : IEntity
    {
        public int Id { get; set; }

        public int FeatureId { get; set; }

        public string FromState { get; set; } = string.Empty;

        public string ToState { get; set; } = string.Empty;

        public DateTime TransitionedAt { get; set; }
    }
}
