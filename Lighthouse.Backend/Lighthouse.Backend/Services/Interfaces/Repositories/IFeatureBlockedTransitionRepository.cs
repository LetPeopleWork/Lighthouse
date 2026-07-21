using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IFeatureBlockedTransitionRepository : IRepository<FeatureBlockedTransition>
    {
        /// <summary>
        /// The single open (LeftAt == null) blocked spell for (<paramref name="featureId"/>,
        /// <paramref name="portfolioId"/>), or null when the feature is not currently blocked in that
        /// portfolio. Portfolio-scoped per ADR-103 — spells are keyed (FeatureId, PortfolioId). Used by
        /// the close handler and by capture idempotency so a second open spell is never opened.
        /// </summary>
        FeatureBlockedTransition? GetOpenSpell(int portfolioId, int featureId);

        /// <summary>
        /// All currently-open (LeftAt == null) blocked spells in <paramref name="portfolioId"/>, keyed by
        /// FeatureId. Used by the wip live-read to populate blockedSince for each blocked feature.
        /// </summary>
        Dictionary<int, FeatureBlockedTransition> GetOpenSpellsForPortfolio(int portfolioId);
    }
}
