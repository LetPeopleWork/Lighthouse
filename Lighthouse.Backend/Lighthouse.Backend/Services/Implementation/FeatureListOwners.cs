using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>Whether the owner of a Features list is there at all.</summary>
    public class FeatureListOwners(IRepository<Portfolio> portfolioRepository, IRepository<Team> teamRepository)
    {
        public bool PortfolioExists(int portfolioId) => portfolioRepository.Exists(portfolioId);

        public bool TeamExists(int teamId) => teamRepository.Exists(teamId);
    }
}
