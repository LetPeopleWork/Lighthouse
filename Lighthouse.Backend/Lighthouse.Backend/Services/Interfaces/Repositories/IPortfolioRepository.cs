using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IPortfolioRepository : IRepository<Portfolio>
    {
        /// <summary>
        /// The ids of the portfolios the given team does work for, and nothing else about them. Reading a
        /// portfolio brings every feature, forecast and simulation result with it, which is far more reading
        /// than a refresh trigger needs to decide which portfolios its team just made stale.
        /// </summary>
        IReadOnlyCollection<int> GetPortfolioIdsForTeam(int teamId);
    }
}
