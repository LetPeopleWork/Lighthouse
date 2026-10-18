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
            => OwnersFollowingAnyDefault()
                .GroupBy(owner => owner.ConnectionId)
                .ToDictionary(
                    connection => connection.Key,
                    connection => new ConnectionFollowers(
                        FollowersOf(connection, ConnectionDefaultSetting.ParentOverrideForTeams),
                        FollowersOf(connection, ConnectionDefaultSetting.ParentOverrideForPortfolios),
                        FollowersOf(connection, ConnectionDefaultSetting.DependencyFieldForPortfolios)));

        public IReadOnlyDictionary<int, IReadOnlyList<FollowerGroup>> FollowerGroupsByConnection()
            => OwnersFollowingAnyDefault()
                .GroupBy(owner => owner.ConnectionId)
                .ToDictionary(
                    connection => connection.Key,
                    connection => (IReadOnlyList<FollowerGroup>)[.. connection
                        .GroupBy(owner => string.Join(",", owner.Follows))
                        .Select(group => new FollowerGroup(group.First().Follows, group.Count()))]);

        private static int FollowersOf(IEnumerable<OwnerFollowing> owners, ConnectionDefaultSetting setting)
            => owners.Count(owner => owner.Follows.Contains(setting));

        // Where an owner's setting comes from is held on the loaded owner, so the owners are read in memory.
        private IEnumerable<OwnerFollowing> OwnersFollowingAnyDefault()
        {
            var teams = teamRepository.GetAllByPredicate(_ => true).AsEnumerable<WorkTrackingSystemOptionsOwner>();
            var portfolios = portfolioRepository.GetAllByPredicate(_ => true).AsEnumerable<WorkTrackingSystemOptionsOwner>();

            return teams.Concat(portfolios)
                .Select(owner => new OwnerFollowing(owner.WorkTrackingSystemConnectionId, DefaultsFollowedBy(owner)))
                .Where(owner => owner.Follows.Count > 0);
        }

        private static List<ConnectionDefaultSetting> DefaultsFollowedBy(WorkTrackingSystemOptionsOwner owner)
            => [.. ConnectionFieldDefaults.All.Select(fieldDefault => fieldDefault.Setting).Where(owner.FollowsConnectionDefault)];

        private static Dictionary<int, int> CountByConnection<TOwner>(IRepository<TOwner> repository) where TOwner : WorkTrackingSystemOptionsOwner
        {
            return repository.GetAllByPredicate(_ => true)
                .GroupBy(owner => owner.WorkTrackingSystemConnectionId)
                .Select(group => new { ConnectionId = group.Key, Count = group.Count() })
                .ToDictionary(entry => entry.ConnectionId, entry => entry.Count);
        }

        private sealed record OwnerFollowing(int ConnectionId, List<ConnectionDefaultSetting> Follows);
    }
}
