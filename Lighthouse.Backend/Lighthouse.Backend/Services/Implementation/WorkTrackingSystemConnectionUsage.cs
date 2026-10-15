using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class WorkTrackingSystemConnectionUsage(IRepository<Team> teamRepository, IRepository<Portfolio> portfolioRepository) : IWorkTrackingSystemConnectionUsage
    {
        public IReadOnlyDictionary<int, ConnectionUsage> ByConnection()
        {
            var teams = CountByConnection(teamRepository);
            var portfolios = CountByConnection(portfolioRepository);

            return teams.Keys.Union(portfolios.Keys).ToDictionary(
                connectionId => connectionId,
                connectionId => new ConnectionUsage(teams.GetValueOrDefault(connectionId), portfolios.GetValueOrDefault(connectionId)));
        }

        private static Dictionary<int, int> CountByConnection<TOwner>(IRepository<TOwner> repository) where TOwner : WorkTrackingSystemOptionsOwner
        {
            return repository.GetAllByPredicate(_ => true)
                .GroupBy(owner => owner.WorkTrackingSystemConnectionId)
                .Select(group => new { ConnectionId = group.Key, Count = group.Count() })
                .ToDictionary(entry => entry.ConnectionId, entry => entry.Count);
        }
    }
}
