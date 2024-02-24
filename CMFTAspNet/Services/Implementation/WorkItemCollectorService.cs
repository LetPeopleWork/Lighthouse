using CMFTAspNet.Models;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation
{
    public class WorkItemCollectorService : IWorkItemCollectorService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;
        private readonly int UnparentedFeatureId = int.MaxValue - 1;

        public WorkItemCollectorService(IWorkItemServiceFactory workItemServiceFactory)
        {
            this.workItemServiceFactory = workItemServiceFactory;
        }

        public async Task<IEnumerable<Feature>> CollectFeaturesForProject(IEnumerable<Project> projects)
        {
            var features = new List<Feature>();

            foreach (var project in projects)
            {
                var featuresForProject = await GetFeaturesForProject(project);
                features.AddRange(featuresForProject.OrderBy(x => x.Order));

                await GetRemainingWorkForFeatures(project, features);

                foreach (var feature in features.ToList())
                {
                    RemoveDoneFeaturesFromList(features, feature);
                }
            }

            return features;
        }

        private void RemoveDoneFeaturesFromList(List<Feature> features, Feature feature)
        {
            if (feature.RemainingWork.Sum(x => x.RemainingWorkItems) == 0)
            {
                features.Remove(feature);
            }

            var uninvolvedTeams = feature.RemainingWork.Where(x => x.RemainingWorkItems == 0).Select(kvp => kvp.Team).ToList();
            foreach (var team in uninvolvedTeams)
            {
                feature.RemoveTeamFromFeature(team);
            }
        }

        private async Task GetRemainingWorkForFeatures(Project project, List<Feature> features)
        {
            foreach (var featureForProject in features)
            {
                await GetRemainingWorkForFeature(featureForProject, project.InvolvedTeams);
            }

            if (project.IncludeUnparentedItems)
            {
                await GetUnparentedItemsForTeams(project, features);
            }
        }

        private async Task GetUnparentedItemsForTeams(Project project, List<Feature> features)
        {
            var featureIds = features.Select(x => x.Id);

            foreach (var team in project.InvolvedTeams)
            {
                var notClosedItems = await GetNotClosedItemsBySearchCriteria(project, team);
                var unparentedItems = await ExtractItemsRelatedToFeature(featureIds, team, notClosedItems);

                var unparentedFeature = features.SingleOrDefault(f => f.Id == UnparentedFeatureId);

                if (unparentedFeature == null)
                {
                    unparentedFeature = new Feature() { Id = UnparentedFeatureId, Name = "Unparented" };
                    features.Add(unparentedFeature);
                }

                unparentedFeature.AddOrUpdateRemainingWorkForTeam(team, unparentedItems.Count);
            }
        }

        private async Task<List<int>> ExtractItemsRelatedToFeature(IEnumerable<int> featureIds, Team team, List<int> notClosedItems)
        {
            var unparentedItems = new List<int>();

            foreach (var itemId in notClosedItems)
            {
                var isRelatedToFeature = await GetWorkItemServiceForTeam(team).IsRelatedToFeature(itemId, featureIds, team);
                if (!isRelatedToFeature)
                {
                    unparentedItems.Add(itemId);
                }
            }

            return unparentedItems;
        }

        private async Task<List<int>> GetNotClosedItemsBySearchCriteria(Project project, Team team)
        {
            List<int> unparentedItems;
            switch (project.SearchBy)
            {
                case SearchBy.Tag:
                    unparentedItems = await GetWorkItemServiceForTeam(team).GetNotClosedWorkItemsByTag(team.WorkItemTypes, project.SearchTerm, team);
                    break;
                case SearchBy.AreaPath:
                    unparentedItems = await GetWorkItemServiceForTeam(team).GetNotClosedWorkItemsByAreaPath(team.WorkItemTypes, project.SearchTerm, team);
                    break;
                default:
                    throw new NotSupportedException($"Search by {project.SearchBy} is not supported!");
            }

            return unparentedItems;
        }

        private async Task GetRemainingWorkForFeature(Feature featureForProject, List<Team> involvedTeams)
        {
            if (featureForProject.Id == UnparentedFeatureId)
            {
                return;
            }

            foreach (var team in involvedTeams)
            {
                var remainingWork = await GetWorkItemServiceForTeam(team).GetRemainingRelatedWorkItems(featureForProject.Id, team);
                featureForProject.AddOrUpdateRemainingWorkForTeam(team, remainingWork);
            }
        }

        private async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            switch (project.SearchBy)
            {
                case SearchBy.Tag:
                    return await GetFeaturesForProject(
                        project,
                        async (workItemTypes, teamConfiguration) =>
                            await GetWorkItemServiceForTeam(teamConfiguration).GetWorkItemsByTag(workItemTypes, project.SearchTerm, teamConfiguration));
                case SearchBy.AreaPath:
                    return await GetFeaturesForProject(
                        project,
                        async (workItemTypes, teamConfiguration) =>
                            await GetWorkItemServiceForTeam(teamConfiguration).GetWorkItemsByAreaPath(workItemTypes, project.SearchTerm, teamConfiguration));
                default:
                    throw new NotSupportedException($"Search by {project.SearchBy} is not supported!");
            }
        }

        private async Task<List<Feature>> GetFeaturesForProject(Project project, Func<IEnumerable<string>, Team, Task<List<int>>> getFeatureAction)
        {
            var featuresForProject = new Dictionary<int, Feature>();

            foreach (var team in project.InvolvedTeams)
            {
                var foundFeatures = await getFeatureAction(project.WorkItemTypes, team);

                foreach (var featureId in foundFeatures.Where(f => AddOrExtendFeature(featuresForProject, f)))
                {
                    await AddFeatureDetails(featuresForProject, team, featureId);
                }
            }

            return [.. featuresForProject.Values];
        }

        private async Task AddFeatureDetails(Dictionary<int, Feature> featuresForProject, Team team, int featureId)
        {
            var (name, order) = await GetWorkItemServiceForTeam(team).GetWorkItemDetails(featureId, team);
            var featureToUpdate = featuresForProject[featureId];

            featureToUpdate.Name = name;
            featureToUpdate.Order = order;
        }

        private bool AddOrExtendFeature(Dictionary<int, Feature> featuresForProject, int featureId)
        {
            if (!featuresForProject.ContainsKey(featureId))
            {
                featuresForProject[featureId] = new Feature() { Id = featureId };
                return true;
            }

            return false;
        }

        private IWorkItemService GetWorkItemServiceForTeam(Team team)
        {
            return workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem);
        }
    }
}
