using System.Security.Claims;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Which Features each Team of a list is working on today, by reference id. A Team the reader may not read is
    /// left out rather than refused, because the list itself is readable.
    /// </summary>
    public class FeatureListActiveWork(
        ITeamMetricsService teamMetricsService,
        IRbacAdministrationService rbacAdministrationService,
        ILighthouseClock clock)
    {
        public async Task<List<TeamAtWork>> Of(
            ClaimsPrincipal user, IReadOnlyCollection<Feature> features, CancellationToken cancellationToken)
        {
            var involved = features
                .SelectMany(feature => feature.Teams)
                .DistinctBy(team => team.Id)
                .ToList();

            var readable = await rbacAdministrationService
                .GetReadableTeamIdsAsync(user, involved.Select(team => team.Id).ToList(), cancellationToken)
                .ConfigureAwait(false);

            var readableTeamIds = readable.ToHashSet();

            return [.. involved
                .Where(team => readableTeamIds.Contains(team.Id))
                .Select(team => new TeamAtWork(team.Id, WorkedOnToday(team)))];
        }

        private List<string> WorkedOnToday(Team team)
            => [.. teamMetricsService.GetInProgressFeatureReferences(team, clock.TodayAsUtcMidnight)
                .OfType<string>()
                .Where(reference => reference.Length > 0)];
    }
}
