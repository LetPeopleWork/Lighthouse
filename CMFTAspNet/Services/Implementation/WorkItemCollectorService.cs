using CMFTAspNet.Models;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation
{
    public class WorkItemCollectorService : IWorkItemCollectorService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;
        private readonly IRepository<Feature> featureRepository;

        public WorkItemCollectorService(IWorkItemServiceFactory workItemServiceFactory, IRepository<Feature> featureRepository)
        {
            this.workItemServiceFactory = workItemServiceFactory;
            this.featureRepository = featureRepository;
        }

        public async Task UpdateFeaturesForProject(Project project)
        {
            var features = new List<Feature>();

            var featuresForProject = await GetFeaturesForProject(project);
            features.AddRange(featuresForProject.OrderBy(x => x.Order));

            await GetRemainingWorkForFeatures(project, features);

            ExtrapolateNotBrokenDownFeatures(features, project);
            RemoveUninvolvedTeams(features);

            project.Features.Clear();
            project.Features.AddRange(features);
        }

        private void ExtrapolateNotBrokenDownFeatures(List<Feature> features, Project project)
        {
            foreach (var feature in features)
            {
                if (feature.RemainingWork.Sum(x => x.RemainingWorkItems) == 0)
                {
                    var numberOfTeams = feature.RemainingWork.Count;
                    var buckets = SplitIntoBuckets(project.DefaultAmountOfWorkItemsPerFeature, numberOfTeams);

                    for (var index = 0; index < numberOfTeams; index++)
                    {
                        feature.RemainingWork[index].RemainingWorkItems = buckets[index];
                    }
                }
            }
        }

        private void RemoveUninvolvedTeams(List<Feature> features)
        {
            foreach (var feature in features.ToList())
            {
                var uninvolvedTeams = feature.RemainingWork.Where(x => x.RemainingWorkItems == 0).Select(kvp => kvp.Team).ToList();
                foreach (var team in uninvolvedTeams)
                {
                    feature.RemoveTeamFromFeature(team);
                }
            }
        }

        private int[] SplitIntoBuckets(int itemCount, int numBuckets)
        {
            var buckets = new int[numBuckets];
            int quotient = itemCount / numBuckets;
            int remainder = itemCount % numBuckets;

            for (int i = 0; i < numBuckets; i++)
            {
                buckets[i] = quotient;
            }

            for (int i = 0; i < remainder; i++)
            {
                buckets[i]++;
            }

            return buckets;
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
            var featureIds = features.Select(x => x.ReferenceId);

            foreach (var team in project.InvolvedTeams)
            {
                var notClosedItems = await GetNotClosedItemsBySearchCriteria(project, team);
                var unparentedItems = await ExtractItemsRelatedToFeature(featureIds, team, notClosedItems);

                var unparentedFeature = new Feature() { Name = $"{project.Name} - Unparented", ReferenceId = int.MaxValue - 1, Order = int.MaxValue, IsUnparentedFeature = true, ProjectId = project.Id, Project = project };
                var unparentedFeatureId = project.Features.FirstOrDefault(f => f.IsUnparentedFeature)?.Id;                

                if (unparentedFeatureId != null)
                {
                    unparentedFeature = featureRepository.GetById(unparentedFeatureId.Value) ?? unparentedFeature;
                }

                features.Add(unparentedFeature);

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
            if (featureForProject.IsUnparentedFeature)
            {
                return;
            }

            foreach (var team in involvedTeams)
            {
                var remainingWork = await GetWorkItemServiceForTeam(team).GetRemainingRelatedWorkItems(featureForProject.ReferenceId, team);
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

                foreach (var featureId in foundFeatures.Where(f => AddOrExtendFeature(featuresForProject, f, project)))
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

        private bool AddOrExtendFeature(Dictionary<int, Feature> featuresForProject, int featureId, Project project)
        {
            if (!featuresForProject.ContainsKey(featureId))
            {
                var newFeature = featureRepository.GetByPredicate(f => f.ReferenceId == featureId);

                if (newFeature == null)
                {
                    newFeature = new Feature() { ReferenceId = featureId, Project = project, ProjectId = project.Id };
                }

                featuresForProject[featureId] = newFeature;
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
