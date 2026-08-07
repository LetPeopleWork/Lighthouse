using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using System.Security.Claims;

namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    public class FeatureMoveAuthorization(IRbacAdministrationService rbacAdministrationService) : IFeatureMoveAuthorization
    {
        public async Task<IReadOnlyDictionary<int, FeatureMoveVerdict>> GetVerdictsAsync(
            ClaimsPrincipal user,
            IReadOnlyCollection<Feature> features,
            ISet<int> readablePortfolioIds,
            CancellationToken cancellationToken = default)
        {
            var requestedPortfolioIds = features
                .SelectMany(feature => feature.Portfolios)
                .Select(portfolio => portfolio.Id)
                .Distinct()
                .ToArray();

            // One lookup for the whole page, not one per row (OQ-1). At five hundred Features the per-row
            // form is a thousand permission checks for a list nobody asked to be authorized.
            var writablePortfolioIds = await rbacAdministrationService
                .GetWritablePortfolioIdsAsync(user, requestedPortfolioIds, cancellationToken)
                .ConfigureAwait(false);

            var writable = writablePortfolioIds is { } ? writablePortfolioIds.ToHashSet() : requestedPortfolioIds.ToHashSet();

            return features.ToDictionary(
                feature => feature.Id,
                feature => VerdictFor(feature, writable, readablePortfolioIds));
        }

        private static FeatureMoveVerdict VerdictFor(Feature feature, HashSet<int> writablePortfolioIds, ISet<int> readablePortfolioIds)
        {
            if (feature.Portfolios.Count == 0)
            {
                return new FeatureMoveVerdict(false, FeatureMoveVerdict.NotInAnyPortfolio, []);
            }

            var blocking = feature.Portfolios
                .Where(portfolio => !writablePortfolioIds.Contains(portfolio.Id))
                .ToList();

            if (blocking.Count == 0)
            {
                return FeatureMoveVerdict.Allowed;
            }

            // Symmetric with what the row already discloses (FeatureDto hides unreadable Portfolios): the
            // refusal stays true without telling the caller a Portfolio they cannot see exists.
            var nameable = blocking
                .Where(portfolio => readablePortfolioIds.Contains(portfolio.Id))
                .ToList();

            return new FeatureMoveVerdict(false, FeatureMoveVerdict.NoWriteOnEveryPortfolio, nameable);
        }
    }
}
