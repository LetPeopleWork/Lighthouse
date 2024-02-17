using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation
{
    public class WorkItemCollectorService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;

        public WorkItemCollectorService(IWorkItemServiceFactory workItemServiceFactory)
        {
            this.workItemServiceFactory = workItemServiceFactory;
        }

        public async Task<IEnumerable<Feature>> CollectFeaturesForReleases(IEnumerable<ReleaseConfiguration> releases)
        {
            var features = new List<Feature>();

            foreach (var release in releases)
            {
                var featuresForRelease = await GetFeaturesForRelease(release);
                features.AddRange(featuresForRelease);

                await GetRemainingWorkForFeatures(features);
            }

            foreach (var feature in features.ToList())
            {
                RemoveDoneFeaturesFromList(features, feature);
            }

            return features;
        }

        private static void RemoveDoneFeaturesFromList(List<Feature> features, Feature feature)
        {
            if (feature.RemainingWork.Sum(x => x.Value) == 0)
            {
                features.Remove(feature);
            }
        }

        private async Task GetRemainingWorkForFeatures(List<Feature> featuresForRelease)
        {
            foreach (var featureForRelease in featuresForRelease)
            {
                await GetRemainingWorkForFeature(featureForRelease);
            }
        }

        private async Task GetRemainingWorkForFeature(Feature featureForRelease)
        {
            foreach (var team in featureForRelease.RemainingWork.Keys)
            {
                var remainingWork = await GetWorkItemServiceForTeam(team.TeamConfiguration).GetRemainingRelatedWorkItems(featureForRelease.Id, team.TeamConfiguration);
                featureForRelease.RemainingWork[team] = remainingWork;
            }
        }

        private async Task<List<Feature>> GetFeaturesForRelease(ReleaseConfiguration release)
        {
            switch (release.SearchBy)
            {
                case SearchBy.Tag:
                    return await GetFeaturesForReleaseConfiguration(
                        release,
                        async (workItemType, tag, teamConfiguration) =>
                            await GetWorkItemServiceForTeam(teamConfiguration).GetWorkItemsByTag(release.WorkItemType, release.SearchTerm, teamConfiguration));
                case SearchBy.AreaPath:
                    return await GetFeaturesForReleaseConfiguration(
                        release,
                        async (workItemType, tag, teamConfiguration) =>
                            await GetWorkItemServiceForTeam(teamConfiguration).GetWorkItemsByAreaPath(release.WorkItemType, release.SearchTerm, teamConfiguration));
                default:
                    throw new NotSupportedException($"Search by {release.SearchBy} is not supported!");
            }
        }

        private async Task<List<Feature>> GetFeaturesForReleaseConfiguration(ReleaseConfiguration release, Func<string, string, ITeamConfiguration, Task<List<int>>> getFeatureAction)
        {
            var featuresForRelease = new Dictionary<int, Feature>();

            foreach (var team in release.InvolvedTeams)
            {
                var foundFeatures = await getFeatureAction(release.WorkItemType, release.SearchTerm, team.TeamConfiguration);

                foreach (var featureId in foundFeatures)
                {
                    if (!featuresForRelease.ContainsKey(featureId))
                    {
                        featuresForRelease[featureId] = new Feature() { Id = featureId };
                    }

                    featuresForRelease[featureId].RemainingWork.Add(team, 0);
                }
            }

            return [.. featuresForRelease.Values];
        }

        private IWorkItemService GetWorkItemServiceForTeam(ITeamConfiguration teamConfiguration)
        {
            return workItemServiceFactory.CreateWorkItemServiceForTeam(teamConfiguration);
        }
    }
}
