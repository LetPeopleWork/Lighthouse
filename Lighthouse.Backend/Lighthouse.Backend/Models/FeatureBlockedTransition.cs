using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class FeatureBlockedTransition : IEntity
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }
        public int PortfolioId { get; set; }
        public DateTime EnteredAt { get; set; }
        public DateTime? LeftAt { get; set; }
    }
}
