using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    // SCAFFOLD: true — DISTILL (ADR-025), feature portfolio-blocked-history. Data-shape scaffold per
    // ADR-102 so the acceptance suite compiles RED; DELIVER (slice 02) owns the migration, repository,
    // handlers and capture seam. Behaviour is not implemented here — only the entity shape.
    public class FeatureBlockedTransition : IEntity
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }
        public int PortfolioId { get; set; }
        public DateTime EnteredAt { get; set; }
        public DateTime? LeftAt { get; set; }
    }
}
