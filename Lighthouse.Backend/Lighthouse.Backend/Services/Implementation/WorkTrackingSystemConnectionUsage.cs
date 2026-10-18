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

        public IReadOnlyDictionary<int, ConnectionFollowers> FollowersByConnection()
        {
            var teams = FollowersOfTheParentOverrideDefault(teamRepository);
            var portfolios = FollowersOfTheParentOverrideDefault(portfolioRepository);

            return teams.Keys.Union(portfolios.Keys).ToDictionary(
                connectionId => connectionId,
                connectionId => new ConnectionFollowers(teams.GetValueOrDefault(connectionId), portfolios.GetValueOrDefault(connectionId)));
        }

        // Where an owner's setting comes from is held on the loaded owner, so the owners are counted in memory.
        private static Dictionary<int, int> FollowersOfTheParentOverrideDefault<TOwner>(IRepository<TOwner> repository) where TOwner : WorkTrackingSystemOptionsOwner
        {
            return repository.GetAllByPredicate(_ => true).AsEnumerable()
                .Where(owner => owner.FollowsParentOverrideDefault)
                .GroupBy(owner => owner.WorkTrackingSystemConnectionId)
                .ToDictionary(group => group.Key, group => group.Count());
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
