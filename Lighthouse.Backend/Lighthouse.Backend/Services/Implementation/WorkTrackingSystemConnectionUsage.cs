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
            var teams = teamRepository.GetAllByPredicate(_ => true).AsEnumerable()
                .Select(team => new OwnerFollowing(team.WorkTrackingSystemConnectionId, DefaultsFollowedBy(team)));
            var portfolios = portfolioRepository.GetAllByPredicate(_ => true).AsEnumerable()
                .Select(portfolio => new OwnerFollowing(portfolio.WorkTrackingSystemConnectionId, DefaultsFollowedBy(portfolio)));

            return teams.Concat(portfolios).Where(owner => owner.Follows.Count > 0);
        }

        private static List<ConnectionDefaultSetting> DefaultsFollowedBy(Team team)
            => team.FollowsParentOverrideDefault ? [ConnectionDefaultSetting.ParentOverrideForTeams] : [];

        private static List<ConnectionDefaultSetting> DefaultsFollowedBy(Portfolio portfolio)
        {
            var follows = new List<ConnectionDefaultSetting>();
            if (portfolio.FollowsParentOverrideDefault)
            {
                follows.Add(ConnectionDefaultSetting.ParentOverrideForPortfolios);
            }

            if (portfolio.FollowsDependencyFieldDefault)
            {
                follows.Add(ConnectionDefaultSetting.DependencyFieldForPortfolios);
            }

            return follows;
        }

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
