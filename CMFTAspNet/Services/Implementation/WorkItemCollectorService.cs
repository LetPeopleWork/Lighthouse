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
            var featuresForProject = await GetFeaturesForProject(project);
            project.UpdateFeatures(featuresForProject.OrderBy(x => x.Order));

            await GetRemainingWorkForFeatures(project);

            RemoveUninvolvedTeams(project);
            ExtrapolateNotBrokenDownFeatures(project);
        }

        private void ExtrapolateNotBrokenDownFeatures(Project project)
        {
            var involvedTeams = project.InvolvedTeams.Where(t => t.TotalThroughput > 0).ToList();

            if (involvedTeams.Count <= 0)
            {
                return;
            }

            foreach (var feature in project.Features.Where(feature => feature.RemainingWork.Sum(x => x.RemainingWorkItems) == 0))
            {
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

            if (project.IncludeUnparentedItems)
            {
                await GetUnparentedItemsForTeams(project);
            }
        }

        private async Task GetUnparentedItemsForTeams(Project project)
        {
            var featureIds = project.Features.Select(x => x.ReferenceId);

            foreach (var team in teamRepository.GetAll())
            {
                var notClosedItems = await GetNotClosedItemsBySearchCriteria(project, team);
                var unparentedItems = await ExtractItemsRelatedToFeature(featureIds, team, notClosedItems);

                var unparentedFeature = new Feature() { Name = $"{project.Name} - Unparented", ReferenceId = (int.MaxValue - 1).ToString(), Order = int.MaxValue, IsUnparentedFeature = true, ProjectId = project.Id, Project = project };
                var unparentedFeatureId = project.Features.Find(f => f.IsUnparentedFeature)?.Id;                

                if (unparentedFeatureId != null)
                {
                    unparentedFeature = featureRepository.GetById(unparentedFeatureId.Value) ?? unparentedFeature;
                }

                project.Features.Add(unparentedFeature);

                unparentedFeature.AddOrUpdateRemainingWorkForTeam(team, unparentedItems.Count);
            }
        }

        private async Task<List<string>> ExtractItemsRelatedToFeature(IEnumerable<string> featureIds, Team team, List<string> notClosedItems)
        {
            var unparentedItems = new List<string>();

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

        private async Task<List<string>> GetNotClosedItemsBySearchCriteria(Project project, Team team)
        {
            List<string> unparentedItems;
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

                if (remainingWork > 0)
                {
                    featureForProject.AddOrUpdateRemainingWorkForTeam(team, remainingWork);
                }
            }
        }

        private async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystem);

            var features = new List<Feature>();
            var featureIds = new List<string>();

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
