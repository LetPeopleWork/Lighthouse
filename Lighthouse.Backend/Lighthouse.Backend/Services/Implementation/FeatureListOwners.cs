using System.Security.Claims;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>Whether the owner of a Features list is there at all and, if it is, whether a caller may open it.</summary>
    public class FeatureListOwners(
        IRepository<Portfolio> portfolioRepository,
        IRepository<Team> teamRepository,
        IRbacAdministrationService rbacAdministrationService)
    {
        public async Task<FeatureListAccess> AccessToPortfolioAsync(ClaimsPrincipal user, int portfolioId, CancellationToken cancellationToken)
        {
            if (!portfolioRepository.Exists(portfolioId))
            {
                return FeatureListAccess.OwnerMissing;
            }

            return await rbacAdministrationService.CanReadPortfolioAsync(user, portfolioId, cancellationToken).ConfigureAwait(false)
                ? FeatureListAccess.Granted
                : FeatureListAccess.Refused;
        }

        public async Task<FeatureListAccess> AccessToTeamAsync(ClaimsPrincipal user, int teamId, CancellationToken cancellationToken)
        {
            if (!teamRepository.Exists(teamId))
            {
                return FeatureListAccess.OwnerMissing;
            }

            return await rbacAdministrationService.CanReadTeamAsync(user, teamId, cancellationToken).ConfigureAwait(false)
                ? FeatureListAccess.Granted
                : FeatureListAccess.Refused;
        }
    }
}
