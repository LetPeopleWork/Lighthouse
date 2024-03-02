using CMFTAspNet.Models;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Services.Implementation
{
    public class WorkItemCollectorService : IWorkItemCollectorService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;
        private readonly IRepository<Feature> featureRepository;
        private readonly IRepository<Team> teamRepository;

        public WorkItemCollectorService(IWorkItemServiceFactory workItemServiceFactory, IRepository<Feature> featureRepository, IRepository<Team> teamRepository)
        {
            this.workItemServiceFactory = workItemServiceFactory;
            this.featureRepository = featureRepository;
            this.teamRepository = teamRepository;
        }

        public async Task UpdateFeaturesForProject(Project project)
        {
            var features = new List<Feature>();

            var featuresForProject = await GetFeaturesForProject(project);
            features.AddRange(featuresForProject.OrderBy(x => x.Order));

            await GetRemainingWorkForFeatures(project, features);

            ExtrapolateNotBrokenDownFeatures(features, project);
            RemoveUninvolvedTeams(features);

            project.UpdateFeatures(features);
        }

        private void ExtrapolateNotBrokenDownFeatures(List<Feature> features, Project project)
        {
            foreach (var feature in features.Where(feature => feature.RemainingWork.Sum(x => x.RemainingWorkItems) == 0))
            {
                var teamsWithThroughput = feature.RemainingWork.Where(rw => rw.Team.TotalThroughput > 0).ToList();

                var numberOfTeams = teamsWithThroughput.Count;
                var buckets = SplitIntoBuckets(project.DefaultAmountOfWorkItemsPerFeature, numberOfTeams);
                for (var index = 0; index < numberOfTeams; index++)
                {
                    teamsWithThroughput[index].RemainingWorkItems = buckets[index];
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
                await GetRemainingWorkForFeature(featureForProject);
            }

            if (project.IncludeUnparentedItems)
            {
                await GetUnparentedItemsForTeams(project, features);
            }
        }

        private async Task GetUnparentedItemsForTeams(Project project, List<Feature> features)
        {
            var featureIds = features.Select(x => x.ReferenceId);

            foreach (var team in teamRepository.GetAll())
            {
                var notClosedItems = await GetNotClosedItemsBySearchCriteria(project, team);
                var unparentedItems = await ExtractItemsRelatedToFeature(featureIds, team, notClosedItems);

                var unparentedFeature = new Feature() { Name = $"{project.Name} - Unparented", ReferenceId = int.MaxValue - 1, Order = int.MaxValue, IsUnparentedFeature = true, ProjectId = project.Id, Project = project };
                var unparentedFeatureId = project.Features.Find(f => f.IsUnparentedFeature)?.Id;                

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
                var isRelatedToFeature = await GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem).IsRelatedToFeature(itemId, featureIds, team);
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
                    unparentedItems = await GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem).GetNotClosedWorkItemsByTag(team.WorkItemTypes, project.SearchTerm, team);
                    break;
                case SearchBy.AreaPath:
                    unparentedItems = await GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem).GetNotClosedWorkItemsByAreaPath(team.WorkItemTypes, project.SearchTerm, team);
                    break;
                default:
                    throw new NotSupportedException($"Search by {project.SearchBy} is not supported!");
            }

            return unparentedItems;
        }

        private async Task GetRemainingWorkForFeature(Feature featureForProject)
        {
            if (featureForProject.IsUnparentedFeature)
            {
                return;
            }

            foreach (var team in teamRepository.GetAll())
            {
                var remainingWork = await GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem).GetRemainingRelatedWorkItems(featureForProject.ReferenceId, team);
                featureForProject.AddOrUpdateRemainingWorkForTeam(team, remainingWork);
            }
        }

        private async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystem);

            var features = new List<Feature>();
            var featureIds = new List<int>();

            switch (project.SearchBy)
            {
                case SearchBy.Tag:
                    featureIds = await workItemService.GetWorkItemsByTag(project.WorkItemTypes, project.SearchTerm, project);
                    break;
                case SearchBy.AreaPath:
                    featureIds = await workItemService.GetWorkItemsByArea(project.WorkItemTypes, project.SearchTerm, project);
                    break;
                default:
                    throw new NotSupportedException($"Search by {project.SearchBy} is not supported!");
            }

            foreach (var featureId in featureIds)
            {
                var feature = GetOrCreateFeature(featureId, project);

                var (name, order) = await workItemService.GetWorkItemDetails(featureId, project);
                feature.Name = name;
                feature.Order = order;

                features.Add(feature);
            }

            return features;
        }

        private Feature GetOrCreateFeature(int featureId, Project project)
        {
            var feature = featureRepository.GetByPredicate(f => f.ReferenceId == featureId);

            if (feature == null)
            {
                feature = new Feature() { ReferenceId = featureId, Project = project, ProjectId = project.Id };
            }

            return feature;
        }

        private IWorkItemService GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            return workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(workTrackingSystem);
        }
    }
}
