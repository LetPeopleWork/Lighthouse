using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Services.Implementation
{
    public class WorkItemCollectorService : IWorkItemCollectorService
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;
        private readonly IRepository<Feature> featureRepository;
        private readonly IRepository<Team> teamRepository;
        private readonly ILogger<WorkItemCollectorService> logger;

        private readonly Dictionary<int, int> defaultWorkItemsBasedOnPercentile = new Dictionary<int, int>();

        public WorkItemCollectorService(IWorkItemServiceFactory workItemServiceFactory, IRepository<Feature> featureRepository, IRepository<Team> teamRepository, ILogger<WorkItemCollectorService> logger)
        {
            this.workItemServiceFactory = workItemServiceFactory;
            this.featureRepository = featureRepository;
            this.teamRepository = teamRepository;
            this.logger = logger;
        }

        public async Task UpdateFeaturesForProject(Project project)
        {
            logger.LogInformation("Updating Features for Project {ProjectName}", project.Name);

            defaultWorkItemsBasedOnPercentile.Remove(project.Id);

            var featuresForProject = await GetFeaturesForProject(project);

            project.UpdateFeatures(featuresForProject.OrderBy(f => f, new FeatureComparer()));

            await GetWorkForFeatures(project);

            RemoveUninvolvedTeams(project);
            await ExtrapolateNotBrokenDownFeatures(project);

            logger.LogInformation("Done Updating Features for Project {ProjectName}", project.Name);
        }

        private async Task ExtrapolateNotBrokenDownFeatures(Project project)
        {
            var involvedTeams = project.InvolvedTeams.Where(t => t.TotalThroughput > 0).ToList();

            if (involvedTeams.Count <= 0)
            {
                return;
            }

            logger.LogInformation("Extrapolating Not Broken Down Features for Project {ProjectName}", project.Name);

            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystemConnection.WorkTrackingSystem);

            foreach (var feature in project.Features.Where(feature => feature.FeatureWork.Sum(x => x.TotalWorkItems) == 0))
            {
                logger.LogInformation("Feature {FeatureName} has no Work - Extrapolating", feature.Name);
                feature.IsUsingDefaultFeatureSize = true;

                var numberOfTeams = involvedTeams.Count;
                var remainingWork = await GetExtrapolatedRemainingWork(project, workItemService, feature);

                var buckets = SplitIntoBuckets(remainingWork, numberOfTeams);
                for (var index = 0; index < numberOfTeams; index++)
                {
                    var team = involvedTeams[index];
                    feature.AddOrUpdateWorkForTeam(team, buckets[index], buckets[index]);
                }

                logger.LogInformation("Added {remainingWork} Items to Feature {FeatureName}", remainingWork, feature.Name);
            }
        }

        private async Task<int> GetExtrapolatedRemainingWork(Project project, IWorkItemService workItemService, Feature? feature)
        {
            if (!string.IsNullOrEmpty(project.SizeEstimateField))
            {
                var estimatedSize = await workItemService.GetEstimatedSizeForItem(feature.ReferenceId, project);

                if (estimatedSize > 0)
                {
                    return estimatedSize;
                }
            }

            return await GetDefaultRemainingWork(project, workItemService);
        }

        private async Task<int> GetDefaultRemainingWork(Project project, IWorkItemService workItemService)
        {
            if (defaultWorkItemsBasedOnPercentile.TryGetValue(project.Id, out var defaultItems))
            {
                return defaultItems;
            }

            defaultItems = project.DefaultAmountOfWorkItemsPerFeature;

            if (project.UsePercentileToCalculateDefaultAmountOfWorkItems)
            {
                logger.LogInformation("Using Percentile to Calculate Default Amount of Work Items for Project {Project}", project.Name);
                var childItems = await workItemService.GetChildItemsForFeaturesInProject(project);

                logger.LogInformation("Features had following number of child items: {ChildItems}", string.Join(",", childItems));

                if (childItems.Any())
                {
                    defaultItems = CalculatePercentile(childItems.ToList(), project.DefaultWorkItemPercentile);

                    logger.LogInformation("{Percentile} Percentile Based on Query {Query} is {DefaultItems}", project.DefaultWorkItemPercentile, project.HistoricalFeaturesWorkItemQuery, defaultItems);
                }
            }

            defaultWorkItemsBasedOnPercentile.Add(project.Id, defaultItems);
            return defaultItems;
        }

        private void RemoveUninvolvedTeams(Project project)
        {
            logger.LogInformation("Removing uninvolved teams for Project {ProjectName}", project.Name);

            foreach (var feature in project.Features.ToList())
            {
                var uninvolvedTeams = feature.FeatureWork.Where(x => x.TotalWorkItems == 0).Select(kvp => kvp.Team).ToList();
                foreach (var team in uninvolvedTeams)
                {
                    logger.LogInformation("Removing Team {TeamName} from Feature {FeatureName}", team.Name, feature.Name);
                    feature.RemoveTeamFromFeature(team);
                }
            }
        }

        private static int CalculatePercentile(List<int> items, int percentile)
        {
            items.Sort();
            var index = (int)Math.Floor((percentile / 100.0) * items.Count) - 1;

            index = Math.Min(Math.Max(index, 0), items.Count - 1);

            return items[index];
        }

        private static int[] SplitIntoBuckets(int itemCount, int numBuckets)
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

        private async Task GetWorkForFeatures(Project project)
        {
            var tasks = project.Features
                .Where(f => !project.OverrideRealChildCountStates.Contains(f.State))
                .Select(featureForProject => GetRemainingWorkForFeature(featureForProject, project.WorkTrackingSystemConnection.WorkTrackingSystem))
                .ToList();

            await Task.WhenAll(tasks);

            await GetUnparentedItems(project);
        }

        private async Task GetUnparentedItems(Project project)
        {
            if (string.IsNullOrEmpty(project.UnparentedItemsQuery))
            {
                return;
            }

            logger.LogInformation("Getting Unparented Items for Project {ProjectName}", project.Name);

            var featureIds = project.Features.Select(x => x.ReferenceId);

            var unparentedFeature = GetOrAddUnparentedFeature(project);

            foreach (var team in teamRepository.GetAll().Where(t => t.WorkTrackingSystemConnection.WorkTrackingSystem == project.WorkTrackingSystemConnection.WorkTrackingSystem))
            {
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);
                var (remainingWorkItems, totalWorkItems) = await workItemService.GetWorkItemsByQuery(team.WorkItemTypes, team, project.UnparentedItemsQuery);

                var itemsMatchingUnparentedItemsQuery = remainingWorkItems.Union(totalWorkItems).ToList();
                var unparentedItems = await GetItemsUnrelatedToFeatures(featureIds, team, itemsMatchingUnparentedItemsQuery);

                logger.LogInformation("Found {UnparentedItems} Unparented Items for Project {ProjectName}", unparentedItems.Count, project.Name);

                var remainingCount = unparentedItems.Intersect(remainingWorkItems).Count();
                var totalCount = unparentedItems.Intersect(totalWorkItems).Count();

                unparentedFeature.AddOrUpdateWorkForTeam(team, remainingCount, totalCount);
            }

            unparentedFeature.Order = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystemConnection.WorkTrackingSystem).GetAdjacentOrderIndex(project.Features.Select(x => x.Order), RelativeOrder.Above);

            logger.LogInformation("Setting order for {UnparentedFeatureName} to {UnparentedFeatureOrder}", unparentedFeature.Name, unparentedFeature.Order);
        }

        private Feature GetOrAddUnparentedFeature(Project project)
        {
            var unparentedFeature = new Feature() { Name = $"{project.Name} - Unparented", ReferenceId = $"{int.MaxValue - 1}", IsUnparentedFeature = true };
            unparentedFeature.Projects.Add(project);

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
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);
                var isRelatedToFeature = await workItemService.IsRelatedToFeature(itemId, featureIds, team);
                if (!isRelatedToFeature)
                {
                    unrelatedItems.Add(itemId);
                }
            }

            return unrelatedItems;
        }

        private async Task GetRemainingWorkForFeature(Feature featureForProject, WorkTrackingSystems workTrackingSystem)
        {
            var teams = teamRepository.GetAll().Where(t => t.WorkTrackingSystemConnection.WorkTrackingSystem == workTrackingSystem).ToList();

            var tasks = teams.Select(async team =>
            {
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystemConnection.WorkTrackingSystem);
                var (remainingWork, totalWork) = await workItemService.GetRelatedWorkItems(featureForProject.ReferenceId, team);

                logger.LogInformation("Found {remainingWork} Work Item Remaining for Team {TeamName} for Feature {FeatureName}", remainingWork, team.Name, featureForProject.Name);

                featureForProject.AddOrUpdateWorkForTeam(team, remainingWork, totalWork);
            }).ToList();

            await Task.WhenAll(tasks);
        }

        private async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystemConnection.WorkTrackingSystem);

            var features = new List<Feature>();
            var featureIds = await workItemService.GetOpenWorkItems(project.WorkItemTypes, project);

            var tasks = featureIds.Select(async featureId =>
            {
                var feature = GetOrCreateFeature(featureId, project);

                var (name, order, url, state) = await workItemService.GetWorkItemDetails(featureId, project);
                feature.Name = name;
                feature.Order = order;
                feature.Url = url;
                feature.State = state;
                feature.IsUsingDefaultFeatureSize = false;

                logger.LogInformation("Found Feature {Name}, Id {Id}, Order {Order}, State {State}", feature.Name, featureId, feature.Order, feature.State);

                return feature;
            }).ToList();

            var featuresArray = await Task.WhenAll(tasks);

            features.AddRange(featuresArray);

            return features;
        }

        private Feature GetOrCreateFeature(string featureId, Project project)
        {
            var feature = featureRepository.GetByPredicate(f => f.ReferenceId == featureId);

            if (feature == null)
            {
                feature = new Feature() { ReferenceId = featureId };
            }

            var featureIsAddedToProject = feature.Projects.Exists(p => p.Id == project.Id);
            if (!featureIsAddedToProject)
            {
                feature.Projects.Add(project);
            }

            return feature;
        }

        private IWorkItemService GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            return workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(workTrackingSystem);
        }
    }
}
