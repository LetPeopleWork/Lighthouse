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

        /// <summary>
        /// The blocked spells in <paramref name="portfolioId"/> that were open on <paramref name="date"/>,
        /// carrying their EnteredAt so a historic read can report how long a feature had been blocked as of
        /// that day (US-03, the portfolio twin of the team read). Portfolio-scoped per ADR-103.
        /// </summary>
        IReadOnlyList<FeatureBlockedTransition> GetBlockedTransitionsAt(int portfolioId, DateOnly date);

        /// <summary>
        /// Which of <paramref name="featureIds"/> have any blocked history at all in
        /// <paramref name="portfolioId"/>. A feature with none predates blocked-history capture, so a
        /// historic read has nothing to answer from and falls back to the live rule; a feature WITH history
        /// but no covering spell reads not blocked — absence of a spell is evidence, not a gap (US-03 AC4).
        /// </summary>
        IReadOnlyList<int> GetFeatureIdsWithBlockedHistory(int portfolioId, IReadOnlyCollection<int> featureIds);
    }
}
