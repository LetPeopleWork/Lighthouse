using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Services.Implementation.Update
{
    public class WorkItemUpdateService : UpdateServiceBase<Project>, IWorkItemUpdateService
    {
        private readonly Dictionary<int, int> defaultWorkItemsBasedOnPercentile = new Dictionary<int, int>();

        public WorkItemUpdateService(ILogger<WorkItemUpdateService> logger, IServiceScopeFactory serviceScopeFactory, IUpdateQueueService updateQueueService) : base(logger, serviceScopeFactory, updateQueueService, UpdateType.Features)
        {
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            using (var scope = CreateServiceScope())
            {
                return GetServiceFromServiceScope<IAppSettingService>(scope).GetFeaturRefreshSettings();
            }
        }

        protected override bool ShouldUpdateEntity(Project entity, RefreshSettings refreshSettings)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - entity.ProjectUpdateTime).TotalMinutes;

            Logger.LogInformation("Last Refresh of Work Items for Project {ProjectName} was {MinutesSinceLastUpdate} Minutes ago - Update should happen after {RefreshAfter} Minutes", entity.Name, minutesSinceLastUpdate, refreshSettings.RefreshAfter);

            return minutesSinceLastUpdate >= refreshSettings.RefreshAfter;
        }

        protected override async Task Update(int id, IServiceProvider serviceProvider)
        {
            var workItemServiceFactory = serviceProvider.GetRequiredService<IWorkItemServiceFactory>();
            var featureRepository = serviceProvider.GetRequiredService<IRepository<Feature>>();
            var projectRepository = serviceProvider.GetRequiredService<IRepository<Project>>();
            var forecastUpdateService = serviceProvider.GetRequiredService<IForecastUpdateService>();

            var project = projectRepository.GetById(id);
            if (project == null)
            {
                return;
            }

            await UpdateFeaturesForProject(projectRepository, featureRepository, workItemServiceFactory, project);

            // Update Forecasts after Features have been updated
            await forecastUpdateService.UpdateForecastsForProject(project.Id);
        }

        private async Task UpdateFeaturesForProject(IRepository<Project> projectRepository, IRepository<Feature> featureRepository, IWorkItemServiceFactory workItemServiceFactory, Project project)
        {
            Logger.LogInformation("Updating Features for Project {ProjectName}", project.Name);
            defaultWorkItemsBasedOnPercentile.Remove(project.Id);

            var featuresForProject = await GetFeaturesForProject(featureRepository, workItemServiceFactory, project);

            project.UpdateFeatures(featuresForProject.OrderBy(f => f, new FeatureComparer()));

            await GetWorkForFeatures(featureRepository, workItemServiceFactory, project);

            await ExtrapolateNotBrokenDownFeatures(workItemServiceFactory, project);

            await projectRepository.Save();
            Logger.LogInformation("Done Updating Features for Project {ProjectName}", project.Name);
        }

        private async Task ExtrapolateNotBrokenDownFeatures(IWorkItemServiceFactory workItemServiceFactory, Project project)
        {
            foreach (var feature in project.GetFeaturesToOverrideWithDefaultSize())
            {
                feature.ClearFeatureWork();
            }

            Logger.LogInformation("Extrapolating Not Broken Down Features for Project {ProjectName}", project.Name);

            var workItemService = GetWorkItemServiceForWorkTrackingSystem(workItemServiceFactory, project.WorkTrackingSystemConnection.WorkTrackingSystem);

            foreach (var feature in project.GetFeaturesToExtrapolate())
            {
                Logger.LogInformation("Feature {FeatureName} has no Work - Extrapolating", feature.Name);
                feature.IsUsingDefaultFeatureSize = true;

                var remainingWork = await GetExtrapolatedRemainingWork(project, workItemService, feature);

                await AssignExtrapolatedWorkToTeams(project, feature, remainingWork, workItemService);

                Logger.LogInformation("Added {RemainingWork} Items to Feature {FeatureName}", remainingWork, feature.Name);
            }
        }

        private async Task AssignExtrapolatedWorkToTeams(Project project, Feature feature, int remainingWork, IWorkItemService workItemService)
        {
            var owningTeams = project.Teams.ToList();

            if (project.OwningTeam != null)
            {
                Logger.LogInformation("Owning Team for Project is {TeamName} - using this for Default Work Assignment", project.OwningTeam.Name);
                owningTeams = new List<Team> { project.OwningTeam };
            }

            if (!string.IsNullOrEmpty(project.FeatureOwnerField))
            {
                Logger.LogInformation("Feature Owner Field for Project is {FeatureOwnerField} - Getting value for Feature {FeatureName}", project.FeatureOwnerField, feature.Name);

                var featureOwnerFieldValue = await workItemService.GetFeatureOwnerByField(feature.ReferenceId, project);
                var featureOwners = project.Teams.Where(t => featureOwnerFieldValue.Contains(t.Name)).ToList();

                Logger.LogInformation("Found following teams defined in {FeatureOwnerField}: {Owners}", project.FeatureOwnerField, string.Join(",", featureOwners));
                if (featureOwners.Count > 0)
                {
                    owningTeams = featureOwners;
                }
            }

            var numberOfTeams = owningTeams.Count;
            var buckets = SplitIntoBuckets(remainingWork, numberOfTeams);
            for (var index = 0; index < numberOfTeams; index++)
            {
                var team = owningTeams[index];
                var totalWork = buckets[index];
                feature.AddOrUpdateWorkForTeam(team, totalWork, totalWork);

                Logger.LogInformation("Added {TotalWork} Items for Feature {FeatureName} to Team {TeamName}", totalWork, feature.Name, team.Name);
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
                Logger.LogInformation("Using Percentile to Calculate Default Amount of Work Items for Project {Project}", project.Name);
                var childItems = await workItemService.GetChildItemsForFeaturesInProject(project);

                Logger.LogInformation("Features had following number of child items: {ChildItems}", string.Join(",", childItems));

                if (childItems.Any())
                {
                    defaultItems = CalculatePercentile(childItems.ToList(), project.DefaultWorkItemPercentile);

                    Logger.LogInformation("{Percentile} Percentile Based on Query {Query} is {DefaultItems}", project.DefaultWorkItemPercentile, project.HistoricalFeaturesWorkItemQuery, defaultItems);
                }
            }

            defaultWorkItemsBasedOnPercentile.Add(project.Id, defaultItems);
            return defaultItems;
        }

        private static int CalculatePercentile(List<int> items, int percentile)
        {
            items.Sort();
            var index = (int)Math.Floor(percentile / 100.0 * items.Count) - 1;

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

        private async Task GetWorkForFeatures(IRepository<Feature> featureRepository, IWorkItemServiceFactory workItemServiceFactory, Project project)
        {
            var tasks = project.Features
                .Select(featureForProject => GetRemainingWorkForFeature(workItemServiceFactory, featureForProject, project.Teams))
                .ToList();

            await Task.WhenAll(tasks);

            await GetUnparentedItems(featureRepository, workItemServiceFactory, project);
        }

        private async Task GetUnparentedItems(IRepository<Feature> featureRepository, IWorkItemServiceFactory workItemServiceFactory, Project project)
        {
            if (string.IsNullOrEmpty(project.UnparentedItemsQuery))
            {
                return;
            }

            Logger.LogInformation("Getting Unparented Items for Project {ProjectName}", project.Name);

            var featureIds = project.Features.Select(x => x.ReferenceId);

            var unparentedFeature = GetOrAddUnparentedFeature(featureRepository, project);

            foreach (var team in project.Teams)
            {
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(workItemServiceFactory, team.WorkTrackingSystemConnection.WorkTrackingSystem);
                var (remainingWorkItems, totalWorkItems) = await workItemService.GetWorkItemsByQuery(team.WorkItemTypes, team, project.UnparentedItemsQuery);

                var itemsMatchingUnparentedItemsQuery = remainingWorkItems.Union(totalWorkItems).ToList();
                var unparentedItems = await GetItemsUnrelatedToFeatures(workItemServiceFactory, featureIds, team, itemsMatchingUnparentedItemsQuery);

                Logger.LogInformation("Found {UnparentedItems} Unparented Items for Project {ProjectName}", unparentedItems.Count, project.Name);

                var remainingCount = unparentedItems.Intersect(remainingWorkItems).Count();
                var totalCount = unparentedItems.Intersect(totalWorkItems).Count();

                unparentedFeature.AddOrUpdateWorkForTeam(team, remainingCount, totalCount);
            }

            unparentedFeature.Order = GetWorkItemServiceForWorkTrackingSystem(workItemServiceFactory, project.WorkTrackingSystemConnection.WorkTrackingSystem).GetAdjacentOrderIndex(project.Features.Select(x => x.Order), RelativeOrder.Above);

            Logger.LogInformation("Setting order for {UnparentedFeatureName} to {UnparentedFeatureOrder}", unparentedFeature.Name, unparentedFeature.Order);
        }

        private Feature GetOrAddUnparentedFeature(IRepository<Feature> featureRepository, Project project)
        {
            var unparentedFeature = new Feature() { Name = $"{project.Name} - Unparented", ReferenceId = $"{int.MaxValue - 1}", IsUnparentedFeature = true, State = "In Progress" };
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

        private static async Task<List<string>> GetItemsUnrelatedToFeatures(IWorkItemServiceFactory workItemServiceFactory, IEnumerable<string> featureIds, Team team, List<string> itemIds)
        {
            var unrelatedItems = new List<string>();

            foreach (var itemId in itemIds)
            {
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(workItemServiceFactory, team.WorkTrackingSystemConnection.WorkTrackingSystem);
                var isRelatedToFeature = await workItemService.IsRelatedToFeature(itemId, featureIds, team);
                if (!isRelatedToFeature)
                {
                    unrelatedItems.Add(itemId);
                }
            }

            return unrelatedItems;
        }

        private async Task GetRemainingWorkForFeature(IWorkItemServiceFactory workItemServiceFactory, Feature featureForProject, IEnumerable<Team> involvedTeams)
        {
            var tasks = involvedTeams.Select(async team =>
            {
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(workItemServiceFactory, team.WorkTrackingSystemConnection.WorkTrackingSystem);
                var (remainingWork, totalWork) = await workItemService.GetRelatedWorkItems(featureForProject.ReferenceId, team);

                Logger.LogInformation("Found {RemainingWork} Work Item Remaining for Team {TeamName} for Feature {FeatureName}", remainingWork, team.Name, featureForProject.Name);

                return (team, remainingWork, totalWork);
            }).ToList();

            await Task.WhenAll(tasks);

            foreach (var (team, remainingWork, totalWork) in tasks.Select(t => t.Result))
            {
                featureForProject.AddOrUpdateWorkForTeam(team, remainingWork, totalWork);
            }
        }

        private async Task<List<Feature>> GetFeaturesForProject(IRepository<Feature> featureRepository, IWorkItemServiceFactory workItemServiceFactory, Project project)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(workItemServiceFactory, project.WorkTrackingSystemConnection.WorkTrackingSystem);

            var features = new List<Feature>();
            var featureIds = await workItemService.GetFeaturesForProject(project);

            var tasks = featureIds.Select(async featureId =>
            {
                var feature = GetOrCreateFeature(featureRepository, featureId, project);

                var (name, order, url, state, startedDate, closedDate) = await workItemService.GetWorkItemDetails(featureId, project);
                feature.Name = name;
                feature.Order = order;
                feature.Url = url;
                feature.State = state;
                feature.StartedDate = startedDate;
                feature.ClosedDate = closedDate;
                feature.IsUsingDefaultFeatureSize = false;

                feature.StateCategory = project.MapStateToStateCategory(state);

                Logger.LogInformation("Found Feature {Name}, Id {Id}, Order {Order}, State {State} (Category: {Category})", feature.Name, featureId, feature.Order, feature.State, feature.StateCategory);

                return feature;
            }).ToList();

            var featuresArray = await Task.WhenAll(tasks);

            features.AddRange(featuresArray);

            return features;
        }

        private Feature GetOrCreateFeature(IRepository<Feature> featureRepository, string featureId, Project project)
        {
            var feature = featureRepository.GetByPredicate(f => f.ReferenceId == featureId);

            feature ??= new Feature() { ReferenceId = featureId };

            var featureIsAddedToProject = feature.Projects.Exists(p => p.Id == project.Id);
            if (!featureIsAddedToProject)
            {
                feature.Projects.Add(project);
            }

            return feature;
        }

        private static IWorkItemService GetWorkItemServiceForWorkTrackingSystem(IWorkItemServiceFactory workItemServiceFactory, WorkTrackingSystems workTrackingSystem)
        {
            return workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(workTrackingSystem);
        }
    }
}
