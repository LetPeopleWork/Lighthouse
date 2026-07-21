using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class FeatureBlockedTransitionRepository(LighthouseAppContext context, ILogger<FeatureBlockedTransitionRepository> logger)
        : RepositoryBase<FeatureBlockedTransition>(context, (lighthouseAppContext) => lighthouseAppContext.FeatureBlockedTransitions, logger), IFeatureBlockedTransitionRepository
    {
        public FeatureBlockedTransition? GetOpenSpell(int portfolioId, int featureId)
        {
            return GetAllByPredicate(transition =>
                    transition.PortfolioId == portfolioId &&
                    transition.FeatureId == featureId &&
                    transition.LeftAt == null)
                .SingleOrDefault();
        }

        public Dictionary<int, FeatureBlockedTransition> GetOpenSpellsForPortfolio(int portfolioId)
        {
            return GetAllByPredicate(transition =>
                    transition.PortfolioId == portfolioId &&
                    transition.LeftAt == null)
                .ToDictionary(transition => transition.FeatureId);
        }
    }
}
