using Lighthouse.Models;
using Lighthouse.Services.Factories;
using Lighthouse.Services.Interfaces;
using Lighthouse.WorkTracking;

namespace Lighthouse.Services.Implementation
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
            var featuresForProject = await GetFeaturesForProject(project);
            project.UpdateFeatures(featuresForProject.OrderBy(f => f, new FeatureComparer()));

            await GetRemainingWorkForFeatures(project);

            RemoveUninvolvedTeams(project);
            await ExtrapolateNotBrokenDownFeaturesAsync(project);
        }

        private async Task ExtrapolateNotBrokenDownFeaturesAsync(Project project)
        {
            var involvedTeams = project.InvolvedTeams.Where(t => t.TotalThroughput > 0).ToList();

            if (involvedTeams.Count <= 0)
            {
                return;
            }

            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystem);

            foreach (var feature in project.Features.Where(feature => feature.RemainingWork.Sum(x => x.RemainingWorkItems) == 0))
            {
                if (await workItemService.ItemHasChildren(feature.ReferenceId, project))
                {
                    continue;
                }

                var numberOfTeams = involvedTeams.Count;
                var buckets = SplitIntoBuckets(project.DefaultAmountOfWorkItemsPerFeature, numberOfTeams);
                for (var index = 0; index < numberOfTeams; index++)
                {
                    var team = involvedTeams[index];
                    feature.AddOrUpdateRemainingWorkForTeam(team, buckets[index]);
                }
            }
        }

        private void RemoveUninvolvedTeams(Project project)
        {
            foreach (var feature in project.Features.ToList())
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

        private async Task GetRemainingWorkForFeatures(Project project)
        {
            foreach (var featureForProject in project.Features)
            {
                await GetRemainingWorkForFeature(featureForProject);
            }

            await GetUnparentedItems(project);
        }

        private async Task GetUnparentedItems(Project project)
        {
            if (string.IsNullOrEmpty(project.UnparentedItemsQuery))
            {
                return;
            }

            var featureIds = project.Features.Select(x => x.ReferenceId);

            var unparentedFeature = GetOrAddUnparentedFeature(project);

            foreach (var team in teamRepository.GetAll().Where(t => t.WorkTrackingSystem == project.WorkTrackingSystem))
            {
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem);
                var itemsMatchingUnparentedItemsQuery = await workItemService.GetOpenWorkItemsByQuery(team.WorkItemTypes, team, project.UnparentedItemsQuery);

                var unparentedItems = await GetItemsUnrelatedToFeatures(featureIds, team, itemsMatchingUnparentedItemsQuery);

                unparentedFeature.AddOrUpdateRemainingWorkForTeam(team, unparentedItems.Count);
            }

            unparentedFeature.Order = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystem).GetAdjacentOrderIndex(project.Features.Select(x => x.Order), RelativeOrder.Above);
        }

        private Feature GetOrAddUnparentedFeature(Project project)
        {
            var unparentedFeature = new Feature() { Name = $"{project.Name} - Unparented", ReferenceId = $"{int.MaxValue - 1}", IsUnparentedFeature = true, ProjectId = project.Id, Project = project };
            var unparentedFeatureId = project.Features.Find(f => f.IsUnparentedFeature)?.Id;

            if (unparentedFeatureId != null)
            {
                unparentedFeature = featureRepository.GetById(unparentedFeatureId.Value) ?? unparentedFeature;
            }
            else
            {                
                project.Features.Add(unparentedFeature);
            }

            return unparentedFeature;
        }

        private async Task<List<string>> GetItemsUnrelatedToFeatures(IEnumerable<string> featureIds, Team team, List<string> itemIds)
        {
            var unrelatedItems = new List<string>();

            foreach (var itemId in itemIds)
            {
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem);
                var isRelatedToFeature = await workItemService.IsRelatedToFeature(itemId, featureIds, team);
                if (!isRelatedToFeature)
                {
                    unrelatedItems.Add(itemId);
                }
            }

            return unrelatedItems;
        }

        private async Task GetRemainingWorkForFeature(Feature featureForProject)
        {
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
            var featureIds = await workItemService.GetOpenWorkItems(project.WorkItemTypes, project);

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

        private Feature GetOrCreateFeature(string featureId, Project project)
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
