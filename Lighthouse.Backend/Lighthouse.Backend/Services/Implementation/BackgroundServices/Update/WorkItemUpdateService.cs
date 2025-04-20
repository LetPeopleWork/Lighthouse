using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.WorkTracking;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
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
            var forecastUpdateService = serviceProvider.GetRequiredService<IForecastService>();
            var workItemRepository = serviceProvider.GetRequiredService<IWorkItemRepository>();

            var project = projectRepository.GetById(id);
            if (project == null)
            {
                return;
            }

            await UpdateFeaturesForProject(projectRepository, featureRepository, workItemRepository, workItemServiceFactory, project);

            await forecastUpdateService.UpdateForecastsForProject(project);
        }

        private async Task UpdateFeaturesForProject(
            IRepository<Project> projectRepository, IRepository<Feature> featureRepository, IWorkItemRepository workItemRepository, IWorkItemServiceFactory workItemServiceFactory, Project project)
        {
            Logger.LogInformation("Updating Features for Project {ProjectName}", project.Name);
            defaultWorkItemsBasedOnPercentile.Remove(project.Id);

            await RefreshFeaturesForProject(featureRepository, workItemServiceFactory, project);

            await UpdateUnparentedItemsForProject(featureRepository, workItemRepository, workItemServiceFactory, project);
            UpdateRemainingWorkForFeatures(workItemRepository, project);

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

                AssignExtrapolatedWorkToTeams(project, feature, remainingWork);

                Logger.LogInformation("Added {RemainingWork} Items to Feature {FeatureName}", remainingWork, feature.Name);
            }
        }

        private void AssignExtrapolatedWorkToTeams(Project project, Feature feature, int remainingWork)
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

                var featureOwners = project.Teams.Where(t => feature.OwningTeam.Contains(t.Name)).ToList();

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

        private async Task<int> GetExtrapolatedRemainingWork(Project project, IWorkItemService workItemService, Feature feature)
        {
            if (feature.EstimatedSize > 0)
            {
                return feature.EstimatedSize;
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
                var historicalFeatureSize = await workItemService.GetHistoricalFeatureSize(project);

                Logger.LogInformation("Features had following number of child items: {ChildItems}", string.Join(",", historicalFeatureSize.Values));

                if (historicalFeatureSize.Count != 0)
                {
                    defaultItems = PercentileCalculator.CalculatePercentile(historicalFeatureSize.Values.ToList(), project.DefaultWorkItemPercentile);

                    Logger.LogInformation("{Percentile} Percentile Based on Query {Query} is {DefaultItems}", project.DefaultWorkItemPercentile, project.HistoricalFeaturesWorkItemQuery, defaultItems);
                }
            }

            defaultWorkItemsBasedOnPercentile.Add(project.Id, defaultItems);
            return defaultItems;
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

        private static void UpdateRemainingWorkForFeatures(IWorkItemRepository workItemRepository, Project project)
        {
            foreach (var feature in project.Features)
            {
                feature.ClearFeatureWork();
                feature.IsUsingDefaultFeatureSize = false;

                var allWorkForFeature = workItemRepository.GetAllByPredicate(wi => wi.ParentReferenceId == feature.ReferenceId).ToList();

                foreach (var team in project.Teams)
                {
                    var totalWorkForFeatureForTeam = allWorkForFeature.Where(f => f.TeamId == team.Id).ToList();
                    var remainingWorkForFeatureForTeam = totalWorkForFeatureForTeam.Where(x => x.StateCategory != StateCategories.Done).ToList();

                    feature.AddOrUpdateWorkForTeam(team, remainingWorkForFeatureForTeam.Count, totalWorkForFeatureForTeam.Count);
                }
            }

            foreach (var feature in project.Features)
            {
                feature.FeatureWork.RemoveAll(f => f.TotalWorkItems == 0);
            }
        }

        private async Task UpdateUnparentedItemsForProject(IRepository<Feature> featureRepository, IWorkItemRepository workItemRepository, IWorkItemServiceFactory workItemServiceFactory, Project project)
        {
            if (string.IsNullOrEmpty(project.UnparentedItemsQuery))
            {
                Logger.LogDebug("Skipping Unparented Items for Project {ProjectName} - No Query defined", project.Name);
                return;
            }

            Logger.LogInformation("Getting Unparented Items for Project {ProjectName}", project.Name);

            var unparentedFeature = GetOrAddUnparentedFeature(featureRepository, project);

            foreach (var team in project.Teams)
            {
                await UpdateUnparentedItemsForTeam(featureRepository, workItemRepository, workItemServiceFactory, project, unparentedFeature, team);
            }

            unparentedFeature.Order = GetWorkItemServiceForWorkTrackingSystem(workItemServiceFactory, project.WorkTrackingSystemConnection.WorkTrackingSystem).GetAdjacentOrderIndex(project.Features.Select(x => x.Order), RelativeOrder.Above);
            Logger.LogInformation("Setting order for {UnparentedFeatureName} to {UnparentedFeatureOrder}", unparentedFeature.Name, unparentedFeature.Order);

            await workItemRepository.Save();
        }

        private async Task UpdateUnparentedItemsForTeam(IRepository<Feature> featureRepository, IWorkItemRepository workItemRepository, IWorkItemServiceFactory workItemServiceFactory, Project project, Feature unparentedFeature, Team team)
        {
            if (string.IsNullOrEmpty(project.UnparentedItemsQuery))
            {
                return;
            }

            var workItemService = GetWorkItemServiceForWorkTrackingSystem(workItemServiceFactory, team.WorkTrackingSystemConnection.WorkTrackingSystem);
            var potentiallyUnparentedWorkItems = await workItemService.GetWorkItemsIdsForTeamWithAdditionalQuery(team, project.UnparentedItemsQuery);

            foreach (var potentiallyUnparentedWorkItem in potentiallyUnparentedWorkItems)
            {
                var workItem = workItemRepository.GetByPredicate(wi => wi.ReferenceId == potentiallyUnparentedWorkItem);
                AssignParentToWorkItem(featureRepository, workItemRepository, project, unparentedFeature, team, workItem);
            }
        }

        private void AssignParentToWorkItem(IRepository<Feature> featureRepository, IWorkItemRepository workItemRepository, Project project, Feature unparentedFeature, Team team, WorkItem? workItem)
        {
            if (workItem != null)
            {
                if (FeatureExists(featureRepository, workItem.ParentReferenceId))
                {
                    Logger.LogInformation("Work Item {ItemReference} of Team {TeamName} is already set to {FeatureReference} - skipping", workItem.ReferenceId, team.Name, workItem.ParentReferenceId);
                    return;
                }

                Logger.LogInformation("Work Item {ItemReference} of Team {TeamName} is unparented and matches the query - mark it to belong to Project {ProjectName}", workItem.ReferenceId, team.Name, project.Name);

                workItem.ParentReferenceId = unparentedFeature.ReferenceId;
                workItemRepository.Update(workItem);
            }
        }

        private static bool FeatureExists(IRepository<Feature> featureRepository, string featureReferenceId)
        {
            if (!string.IsNullOrEmpty(featureReferenceId))
            {
                var feature = featureRepository.GetByPredicate(f => f.ReferenceId == featureReferenceId);
                return feature != null;
            }

            return false;
        }

        private Feature GetOrAddUnparentedFeature(IRepository<Feature> featureRepository, Project project)
        {
            var referenceId = Guid.NewGuid().ToString();
            var unparentedFeature = new Feature() { Name = $"{project.Name} - Unparented", ReferenceId = referenceId, IsUnparentedFeature = true, State = "In Progress" };
            unparentedFeature.Projects.Add(project);

            var unparentedFeatureId = project.Features.Find(f => f.IsUnparentedFeature)?.Id;

            if (unparentedFeatureId != null)
            {
                unparentedFeature = featureRepository.GetById(unparentedFeatureId.Value) ?? unparentedFeature;

                // We need a unique referenceId for the unparented feature - previously all unparented features had the same referenceId.
                if (unparentedFeature.ReferenceId == $"{int.MaxValue - 1}")
                {
                    unparentedFeature.ReferenceId = referenceId;
                }
            }
            else
            {
                project.Features.Add(unparentedFeature);
            }

            return unparentedFeature;
        }

        private async Task RefreshFeaturesForProject(IRepository<Feature> featureRepository, IWorkItemServiceFactory workItemServiceFactory, Project project)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(workItemServiceFactory, project.WorkTrackingSystemConnection.WorkTrackingSystem);

            var features = new List<Feature>();

            foreach (var feature in await workItemService.GetFeaturesForProject(project))
            {
                var featureFromDatabase = featureRepository.GetByPredicate(f => f.ReferenceId == feature.ReferenceId);

                if (featureFromDatabase == null)
                {
                    featureRepository.Add(feature);
                    Logger.LogDebug("Found New Feature {FeatureName}", feature.Name);
                    featureFromDatabase = feature;
                }
                else
                {
                    featureFromDatabase.Update(feature);
                    featureRepository.Update(featureFromDatabase);
                    Logger.LogDebug("Updated Existing Feature {FeatureName}", feature.Name);
                }

                AddProjectToFeature(featureFromDatabase, project);
                features.Add(featureFromDatabase);
            }

            project.UpdateFeatures(features.OrderBy(f => f, new FeatureComparer()));

            await featureRepository.Save();
        }

        private static void AddProjectToFeature(Feature feature, Project project)
        {
            var featureIsAddedToProject = feature.Projects.Exists(p => p.Id == project.Id);
            if (!featureIsAddedToProject)
            {
                feature.Projects.Add(project);
            }
        }

        private static IWorkItemService GetWorkItemServiceForWorkTrackingSystem(IWorkItemServiceFactory workItemServiceFactory, WorkTrackingSystems workTrackingSystem)
        {
            return workItemServiceFactory.GetWorkItemServiceForWorkTrackingSystem(workTrackingSystem);
        }
    }
}
