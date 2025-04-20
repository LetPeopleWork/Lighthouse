using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkItems
{
    public class WorkItemService : IWorkItemService
    {
        private readonly Dictionary<int, int> defaultWorkItemsBasedOnPercentile = new Dictionary<int, int>();
        private readonly ILogger<WorkItemService> logger;
        private readonly IWorkTrackingConnectorFactory workTrackingConnectorFactory;
        private readonly IRepository<Feature> featureRepository;
        private readonly IWorkItemRepository workItemRepository;

        public WorkItemService(ILogger<WorkItemService> logger, IWorkTrackingConnectorFactory workTrackingConnectorFactory, IRepository<Feature> featureRepository, IWorkItemRepository workItemRepository)
        {
            this.logger = logger;
            this.workTrackingConnectorFactory = workTrackingConnectorFactory;
            this.featureRepository = featureRepository;
            this.workItemRepository = workItemRepository;
        }

        public async Task UpdateWorkItemsForProject(Project project)        
        {
            logger.LogInformation("Updating Features for Project {ProjectName}", project.Name);
            defaultWorkItemsBasedOnPercentile.Remove(project.Id);

            await RefreshFeaturesForProject(project);

            await UpdateUnparentedItemsForProject(project);
            UpdateRemainingWorkForFeatures(project);

            await ExtrapolateNotBrokenDownFeatures(project);

            await featureRepository.Save();

            logger.LogInformation("Done Updating Features for Project {ProjectName}", project.Name);
        }

        private async Task ExtrapolateNotBrokenDownFeatures(Project project)
        {
            foreach (var feature in project.GetFeaturesToOverrideWithDefaultSize())
            {
                feature.ClearFeatureWork();
            }

            logger.LogInformation("Extrapolating Not Broken Down Features for Project {ProjectName}", project.Name);

            var workItemService = GetWorkItemServiceForWorkTrackingSystem(workTrackingConnectorFactory, project.WorkTrackingSystemConnection.WorkTrackingSystem);

            foreach (var feature in project.GetFeaturesToExtrapolate())
            {
                logger.LogInformation("Feature {FeatureName} has no Work - Extrapolating", feature.Name);
                feature.IsUsingDefaultFeatureSize = true;

                var remainingWork = await GetExtrapolatedRemainingWork(project, workItemService, feature);

                AssignExtrapolatedWorkToTeams(project, feature, remainingWork);

                logger.LogInformation("Added {RemainingWork} Items to Feature {FeatureName}", remainingWork, feature.Name);
            }
        }

        private void AssignExtrapolatedWorkToTeams(Project project, Feature feature, int remainingWork)
        {
            var owningTeams = project.Teams.ToList();

            if (project.OwningTeam != null)
            {
                logger.LogInformation("Owning Team for Project is {TeamName} - using this for Default Work Assignment", project.OwningTeam.Name);
                owningTeams = new List<Team> { project.OwningTeam };
            }

            if (!string.IsNullOrEmpty(project.FeatureOwnerField))
            {
                logger.LogInformation("Feature Owner Field for Project is {FeatureOwnerField} - Getting value for Feature {FeatureName}", project.FeatureOwnerField, feature.Name);

                var featureOwners = project.Teams.Where(t => feature.OwningTeam.Contains(t.Name)).ToList();

                logger.LogInformation("Found following teams defined in {FeatureOwnerField}: {Owners}", project.FeatureOwnerField, string.Join(",", featureOwners));
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

                logger.LogInformation("Added {TotalWork} Items for Feature {FeatureName} to Team {TeamName}", totalWork, feature.Name, team.Name);
            }
        }

        private async Task<int> GetExtrapolatedRemainingWork(Project project, IWorkTrackingConnector workItemService, Feature feature)
        {
            if (feature.EstimatedSize > 0)
            {
                return feature.EstimatedSize;
            }

            return await GetDefaultRemainingWork(project, workItemService);
        }

        private async Task<int> GetDefaultRemainingWork(Project project, IWorkTrackingConnector workItemService)
        {
            if (defaultWorkItemsBasedOnPercentile.TryGetValue(project.Id, out var defaultItems))
            {
                return defaultItems;
            }

            defaultItems = project.DefaultAmountOfWorkItemsPerFeature;

            if (project.UsePercentileToCalculateDefaultAmountOfWorkItems)
            {
                logger.LogInformation("Using Percentile to Calculate Default Amount of Work Items for Project {Project}", project.Name);
                var historicalFeatureSize = await workItemService.GetHistoricalFeatureSize(project);

                logger.LogInformation("Features had following number of child items: {ChildItems}", string.Join(",", historicalFeatureSize.Values));

                if (historicalFeatureSize.Count != 0)
                {
                    defaultItems = PercentileCalculator.CalculatePercentile(historicalFeatureSize.Values.ToList(), project.DefaultWorkItemPercentile);

                    logger.LogInformation("{Percentile} Percentile Based on Query {Query} is {DefaultItems}", project.DefaultWorkItemPercentile, project.HistoricalFeaturesWorkItemQuery, defaultItems);
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

        private void UpdateRemainingWorkForFeatures(Project project)
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

        private async Task UpdateUnparentedItemsForProject(Project project)
        {
            if (string.IsNullOrEmpty(project.UnparentedItemsQuery))
            {
                logger.LogDebug("Skipping Unparented Items for Project {ProjectName} - No Query defined", project.Name);
                return;
            }

            logger.LogInformation("Getting Unparented Items for Project {ProjectName}", project.Name);

            var unparentedFeature = GetOrAddUnparentedFeature(featureRepository, project);

            foreach (var team in project.Teams)
            {
                await UpdateUnparentedItemsForTeam(featureRepository, workItemRepository, workTrackingConnectorFactory, project, unparentedFeature, team);
            }

            unparentedFeature.Order = GetWorkItemServiceForWorkTrackingSystem(workTrackingConnectorFactory, project.WorkTrackingSystemConnection.WorkTrackingSystem).GetAdjacentOrderIndex(project.Features.Select(x => x.Order), RelativeOrder.Above);
            logger.LogInformation("Setting order for {UnparentedFeatureName} to {UnparentedFeatureOrder}", unparentedFeature.Name, unparentedFeature.Order);

            await workItemRepository.Save();
        }

        private async Task UpdateUnparentedItemsForTeam(IRepository<Feature> featureRepository, IWorkItemRepository workItemRepository, IWorkTrackingConnectorFactory workTrackingConnectorFactory, Project project, Feature unparentedFeature, Team team)
        {
            if (string.IsNullOrEmpty(project.UnparentedItemsQuery))
            {
                return;
            }

            var workItemService = GetWorkItemServiceForWorkTrackingSystem(workTrackingConnectorFactory, team.WorkTrackingSystemConnection.WorkTrackingSystem);
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
                    logger.LogInformation("Work Item {ItemReference} of Team {TeamName} is already set to {FeatureReference} - skipping", workItem.ReferenceId, team.Name, workItem.ParentReferenceId);
                    return;
                }

                logger.LogInformation("Work Item {ItemReference} of Team {TeamName} is unparented and matches the query - mark it to belong to Project {ProjectName}", workItem.ReferenceId, team.Name, project.Name);

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

        private async Task RefreshFeaturesForProject(Project project)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(workTrackingConnectorFactory, project.WorkTrackingSystemConnection.WorkTrackingSystem);

            var features = new List<Feature>();

            foreach (var feature in await workItemService.GetFeaturesForProject(project))
            {
                var featureFromDatabase = featureRepository.GetByPredicate(f => f.ReferenceId == feature.ReferenceId);

                if (featureFromDatabase == null)
                {
                    featureRepository.Add(feature);
                    logger.LogDebug("Found New Feature {FeatureName}", feature.Name);
                    featureFromDatabase = feature;
                }
                else
                {
                    featureFromDatabase.Update(feature);
                    featureRepository.Update(featureFromDatabase);
                    logger.LogDebug("Updated Existing Feature {FeatureName}", feature.Name);
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

        private static IWorkTrackingConnector GetWorkItemServiceForWorkTrackingSystem(IWorkTrackingConnectorFactory workTrackingConnectorFactory, WorkTrackingSystems workTrackingSystem)
        {
            return workTrackingConnectorFactory.GetWorkTrackingConnector(workTrackingSystem);
        }
    }
}
