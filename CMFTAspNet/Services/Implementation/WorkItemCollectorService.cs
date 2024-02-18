using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation
{
    public class WorkItemCollectorService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;
        private readonly int UnparentedFeatureId = int.MaxValue - 1;

        public WorkItemCollectorService(IWorkItemServiceFactory workItemServiceFactory)
        {
            this.workItemServiceFactory = workItemServiceFactory;
        }

        public async Task CollectFeaturesForReleases(IEnumerable<ReleaseConfiguration> releases)
        {
            foreach (var release in releases)
            {
                release.Features.Clear();
                var featuresForRelease = await GetFeaturesForRelease(release);
                release.Features.AddRange(featuresForRelease.OrderBy(x => x.Order));

                await GetRemainingWorkForFeatures(release);

                foreach (var feature in release.Features.ToList())
                {
                    RemoveDoneFeaturesFromList(release.Features, feature);
                }
            }
        }

        private void RemoveDoneFeaturesFromList(List<Feature> features, Feature feature)
        {
            if (feature.RemainingWork.Sum(x => x.Value) == 0)
            {
                features.Remove(feature);
            }

            var uninvolvedTeams = feature.RemainingWork.Where(x => x.Value == 0).Select(kvp => kvp.Key);
            foreach (var team in uninvolvedTeams)
            {
                feature.RemainingWork.Remove(team);
            }
        }

        private async Task GetRemainingWorkForFeatures(ReleaseConfiguration releaseConfiguration)
        {
            foreach (var featureForRelease in releaseConfiguration.Features)
            {
                await GetRemainingWorkForFeature(featureForRelease, releaseConfiguration.InvolvedTeams);
            }

            if (releaseConfiguration.IncludeUnparentedItems)
            {
                await GetUnparentedItemsForTeams(releaseConfiguration);
            }
        }

        private async Task GetUnparentedItemsForTeams(ReleaseConfiguration releaseConfiguration)
        {
            var featureIds = releaseConfiguration.Features.Select(x => x.Id);

            foreach (var team in releaseConfiguration.InvolvedTeams)
            {
                var notClosedItems = await GetNotClosedItemsBySearchCriteria(releaseConfiguration, team.TeamConfiguration);
                var unparentedItems = await ExtractItemsRelatedToFeature(featureIds, team.TeamConfiguration, notClosedItems);

                var unparentedFeature = releaseConfiguration.Features.SingleOrDefault(f => f.Id == UnparentedFeatureId);

                if (unparentedFeature == null)
                {
                    unparentedFeature = new Feature() { Id = UnparentedFeatureId, Name = "Unparented" };
                    releaseConfiguration.Features.Add(unparentedFeature);
                }

                unparentedFeature.RemainingWork.Add(team, unparentedItems.Count);
            }
        }

        private async Task<List<int>> ExtractItemsRelatedToFeature(IEnumerable<int> featureIds, ITeamConfiguration teamConfiguration, List<int> notClosedItems)
        {
            var unparentedItems = new List<int>();

            foreach (var itemId in notClosedItems)
            {
                var isRelatedToFeature = await GetWorkItemServiceForTeam(teamConfiguration).IsRelatedToFeature(itemId, featureIds, teamConfiguration);
                if (!isRelatedToFeature)
                {
                    unparentedItems.Add(itemId);
                }
            }

            return unparentedItems;
        }

        private async Task<List<int>> GetNotClosedItemsBySearchCriteria(ReleaseConfiguration releaseConfiguration, ITeamConfiguration teamConfiguration)
        {
            List<int> unparentedItems;
            switch (releaseConfiguration.SearchBy)
            {
                case SearchBy.Tag:
                    unparentedItems = await GetWorkItemServiceForTeam(teamConfiguration).GetNotClosedWorkItemsByTag(teamConfiguration.WorkItemTypes, releaseConfiguration.SearchTerm, teamConfiguration);
                    break;
                case SearchBy.AreaPath:
                    unparentedItems = await GetWorkItemServiceForTeam(teamConfiguration).GetNotClosedWorkItemsByAreaPath(teamConfiguration.WorkItemTypes, releaseConfiguration.SearchTerm, teamConfiguration);
                    break;
                default:
                    throw new NotSupportedException($"Search by {releaseConfiguration.SearchBy} is not supported!");
            }

            return unparentedItems;
        }

        private async Task GetRemainingWorkForFeature(Feature featureForRelease, List<Team> involvedTeams)
        {
            if (featureForRelease.Id == UnparentedFeatureId)
            {
                return;
            }

            foreach (var team in involvedTeams)
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
                        async (workItemTypes, teamConfiguration) =>
                            await GetWorkItemServiceForTeam(teamConfiguration).GetWorkItemsByTag(workItemTypes, release.SearchTerm, teamConfiguration));
                case SearchBy.AreaPath:
                    return await GetFeaturesForReleaseConfiguration(
                        release,
                        async (workItemTypes, teamConfiguration) =>
                            await GetWorkItemServiceForTeam(teamConfiguration).GetWorkItemsByAreaPath(workItemTypes, release.SearchTerm, teamConfiguration));
                default:
                    throw new NotSupportedException($"Search by {release.SearchBy} is not supported!");
            }
        }

        private async Task<List<Feature>> GetFeaturesForReleaseConfiguration(ReleaseConfiguration release, Func<IEnumerable<string>, ITeamConfiguration, Task<List<int>>> getFeatureAction)
        {
            var featuresForRelease = new Dictionary<int, Feature>();

            foreach (var team in release.InvolvedTeams)
            {
                var foundFeatures = await getFeatureAction(release.WorkItemTypes, team.TeamConfiguration);

                foreach (var featureId in foundFeatures)
                {
                    if (AddOrExtendFeature(featuresForRelease, featureId))
                    {
                        await AddFeatureDetails(featuresForRelease, team, featureId);
                    }
                }
            }

            return [.. featuresForRelease.Values];
        }

        private async Task AddFeatureDetails(Dictionary<int, Feature> featuresForRelease, Team team, int featureId)
        {
            var (name, order) = await GetWorkItemServiceForTeam(team.TeamConfiguration).GetWorkItemDetails(featureId, team.TeamConfiguration);
            var featureToUpdate = featuresForRelease[featureId];

            featureToUpdate.Name = name;
            featureToUpdate.Order = order;
        }

        private bool AddOrExtendFeature(Dictionary<int, Feature> featuresForRelease, int featureId)
        {
            if (!featuresForRelease.ContainsKey(featureId))
            {
                featuresForRelease[featureId] = new Feature() { Id = featureId };
                return true;
            }

            return false;
        }

        private IWorkItemService GetWorkItemServiceForTeam(ITeamConfiguration teamConfiguration)
        {
            return workItemServiceFactory.CreateWorkItemServiceForTeam(teamConfiguration);
        }
    }
}
