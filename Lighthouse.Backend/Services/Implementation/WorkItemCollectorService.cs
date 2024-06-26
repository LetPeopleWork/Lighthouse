﻿using Lighthouse.Backend.Models;
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
            
            var featuresForProject = await GetFeaturesForProject(project);


            project.UpdateFeatures(featuresForProject.OrderBy(f => f, new FeatureComparer()));

            await GetRemainingWorkForFeatures(project);

            RemoveUninvolvedTeams(project);
            await ExtrapolateNotBrokenDownFeaturesAsync(project);

            logger.LogInformation("Done Updating Features for Project {ProjectName}", project.Name);
        }

        private async Task ExtrapolateNotBrokenDownFeaturesAsync(Project project)
        {
            var involvedTeams = project.InvolvedTeams.Where(t => t.TotalThroughput > 0).ToList();

            if (involvedTeams.Count <= 0)
            {
                return;
            }

            logger.LogInformation("Extrapolating Not Broken Down Features for Project {ProjectName}", project.Name);

            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystem);

            foreach (var feature in project.Features.Where(feature => feature.RemainingWork.Sum(x => x.RemainingWorkItems) == 0))
            {
                logger.LogInformation("Checking Feature {FeatureName}", feature.Name);
                if (await workItemService.ItemHasChildren(feature.ReferenceId, project))
                {
                    logger.LogInformation("Feature has items already done - skipping");
                    continue;
                }

                var numberOfTeams = involvedTeams.Count;
                var buckets = SplitIntoBuckets(project.DefaultAmountOfWorkItemsPerFeature, numberOfTeams);
                for (var index = 0; index < numberOfTeams; index++)
                {
                    var team = involvedTeams[index];
                    feature.AddOrUpdateRemainingWorkForTeam(team, buckets[index]);
                }

                logger.LogInformation("Added {DefaultAmountOfWorkItemsPerFeature} Items to Feature {FeatureName}", project.DefaultAmountOfWorkItemsPerFeature, feature.Name);
            }
        }

        private void RemoveUninvolvedTeams(Project project)
        {
            logger.LogInformation("Removing uninvolved teams for Project {ProjectName}", project.Name);

            foreach (var feature in project.Features.ToList())
            {
                var uninvolvedTeams = feature.RemainingWork.Where(x => x.RemainingWorkItems == 0).Select(kvp => kvp.Team).ToList();
                foreach (var team in uninvolvedTeams)
                {
                    logger.LogInformation("Removing Team {TeamName} from Feature {FeatureName}", team.Name, feature.Name);
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
            var tasks = project.Features
                .Select(featureForProject => GetRemainingWorkForFeature(featureForProject, project.WorkTrackingSystem))
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

            foreach (var team in teamRepository.GetAll().Where(t => t.WorkTrackingSystem == project.WorkTrackingSystem))
            {
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem);
                var itemsMatchingUnparentedItemsQuery = await workItemService.GetOpenWorkItemsByQuery(team.WorkItemTypes, team, project.UnparentedItemsQuery);

                var unparentedItems = await GetItemsUnrelatedToFeatures(featureIds, team, itemsMatchingUnparentedItemsQuery);

                logger.LogInformation("Found {UnparentedItems} Unparented Items for Project {ProjectName}", unparentedItems.Count, project.Name);

                unparentedFeature.AddOrUpdateRemainingWorkForTeam(team, unparentedItems.Count);
            }

            unparentedFeature.Order = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystem).GetAdjacentOrderIndex(project.Features.Select(x => x.Order), RelativeOrder.Above);

            logger.LogInformation("Setting order for {UnparentedFeatureName} to {UnparentedFeatureOrder}", unparentedFeature.Name, unparentedFeature.Order);
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

        private async Task GetRemainingWorkForFeature(Feature featureForProject, WorkTrackingSystems workTrackingSystem)
        {
            var teams = teamRepository.GetAll().Where(t => t.WorkTrackingSystem == workTrackingSystem).ToList();

            var tasks = teams.Select(async team =>
            {
                var workItemService = GetWorkItemServiceForWorkTrackingSystem(team.WorkTrackingSystem);
                var remainingWork = await workItemService.GetRemainingRelatedWorkItems(featureForProject.ReferenceId, team);

                logger.LogInformation("Found {remainingWork} Work Item Remaining for Team {TeamName} for Feature {FeatureName}", remainingWork, team.Name, featureForProject.Name);

                featureForProject.AddOrUpdateRemainingWorkForTeam(team, remainingWork);
            }).ToList();

            await Task.WhenAll(tasks);
        }

        private async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystem);

            var features = new List<Feature>();
            var featureIds = await workItemService.GetOpenWorkItems(project.WorkItemTypes, project);

            var tasks = featureIds.Select(async featureId =>
            {
                var feature = GetOrCreateFeature(featureId, project);

                var (name, order) = await workItemService.GetWorkItemDetails(featureId, project);
                feature.Name = name;
                feature.Order = order;

                logger.LogInformation("Found Feature {Name}, Id {Id}, Order {Order}", feature.Name, featureId, feature.Order);

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
