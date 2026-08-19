using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// Whether a reader may see a Feature at all. It lives on its own because more than one surface asks
    /// - the row, the list of what a Feature waits on, the warning about that list - and a second copy of
    /// the rule is how one of those surfaces ends up disclosing what another one withholds.
    /// </summary>
    public static class FeatureReadability
    {
        // A Feature in no Portfolio is visible to everyone; otherwise one readable Portfolio is enough.
        public static bool IsReadableBy(Feature feature, HashSet<int> readablePortfolioIds)
        {
            return feature.Portfolios.Count == 0
                || feature.Portfolios.Any(portfolio => readablePortfolioIds.Contains(portfolio.Id));
        }
    }
}
