using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkItems
{
    public class WorkItemService(
        ILogger<WorkItemService> logger,
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        IRepository<Feature> featureRepository,
        IWorkItemRepository workItemRepository,
        IPortfolioMetricsService portfolioMetricsService,
        IRepository<Team> teamRepository)
        : IWorkItemService
    {
        private readonly Dictionary<int, int> defaultWorkItemsBasedOnPercentile = new();

        public async Task UpdateFeaturesForPortfolio(Portfolio portfolio)
        {
            logger.LogInformation("Updating Features for Portfolio {PortfolioName}", portfolio.Name);

            await RefreshFeatures(portfolio);
            await RefreshParentFeatures(portfolio);

            await UpdateRemainingWorkForPortfolio(portfolio);

            logger.LogInformation("Done Updating Features for Portfolio {PortfolioName}", portfolio.Name);
        }

        public async Task UpdateWorkItemsForTeam(Team team)
        {
            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            await RefreshWorkItems(team);

            foreach (var portfolio in team.Portfolios.ToList())
            {
                await UpdateRemainingWorkForPortfolio(portfolio);
            }

            logger.LogInformation("Done Updating Work Items for Team {TeamName}", team.Name);
        }

        private async Task RefreshWorkItems(Team team)
        {
            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var workItemService = workTrackingConnectorFactory.GetWorkTrackingConnector(team.WorkTrackingSystemConnection.WorkTrackingSystem);

            var storedWorkItems = workItemRepository.GetAllByPredicate(wi => wi.TeamId == team.Id).ToList();
            var actualWorkItems = await workItemService.GetWorkItemsForTeam(team);

            foreach (var item in actualWorkItems)
            {
                var existingItem = storedWorkItems.SingleOrDefault(wi => wi.ReferenceId == item.ReferenceId);
                if (existingItem == null)
                {
                    workItemRepository.Add(item);
                    logger.LogDebug("Added Work Item {WorkItemId}", item.ReferenceId);
                }
                else
                {
                    existingItem.Update(item);
                    workItemRepository.Update(existingItem);

                    storedWorkItems.Remove(existingItem);

                    logger.LogDebug("Updated Work Item {WorkItemId}", item.ReferenceId);
                }
            }

            foreach (var itemToRemove in storedWorkItems)
            {
                workItemRepository.Remove(itemToRemove.Id);
                logger.LogDebug("Removed Work Item {WorkItemId}", itemToRemove.ReferenceId);
            }

            await workItemRepository.Save();
        }

        private async Task UpdateRemainingWorkForPortfolio(Portfolio portfolio)
        {
            logger.LogInformation("Updating Remaining Work for Portfolio {PortfolioName}", portfolio.Name);
            defaultWorkItemsBasedOnPercentile.Remove(portfolio.Id);

            RefreshRemainingWork(portfolio);
            AutoUpdateInvolvedTeams(portfolio);
            ExtrapolateNotBrokenDownFeatures(portfolio);

            await featureRepository.Save();

            logger.LogInformation("Done Updating Remaining Work for Portfolio {PortfolioName}", portfolio.Name);
        }

        private void RefreshRemainingWork(Portfolio project)
        {
            foreach (var feature in project.Features)
            {
                feature.ClearFeatureWork();
                feature.IsUsingDefaultFeatureSize = false;

                var allWorkForFeature = workItemRepository.GetAllByPredicate(wi => wi.ParentReferenceId == feature.ReferenceId).ToList();

                var teamsWithWork = allWorkForFeature
                    .Where(w => w.Team != null)
                    .Select(w => w.Team)
                    .DistinctBy(t => t.Id)
                    .ToList();

                foreach (var team in teamsWithWork)
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

        private static void AutoUpdateInvolvedTeams(Portfolio portfolio)
        {
            var involvedTeams = portfolio.Features
                .SelectMany(f => f.FeatureWork)
                .Select(fw => fw.Team)
                .DistinctBy(t => t.Id)
                .ToList();

            portfolio.UpdateTeams(involvedTeams);
        }

        private void ExtrapolateNotBrokenDownFeatures(Portfolio portfolio)
        {
            foreach (var feature in portfolio.GetFeaturesToOverrideWithDefaultSize())
            {
                feature.ClearFeatureWork();
            }

            logger.LogInformation("Extrapolating Not Broken Down Features for Portfolio {PortfolioName}", portfolio.Name);

            foreach (var feature in portfolio.GetFeaturesToExtrapolate())
            {
                logger.LogInformation("Feature {FeatureName} has no Work - Extrapolating", feature.Name);
                feature.IsUsingDefaultFeatureSize = true;

                var remainingWork = GetExtrapolatedRemainingWork(portfolio, feature);

                AssignExtrapolatedWorkToTeams(portfolio, feature, remainingWork);

                logger.LogInformation("Added {RemainingWork} Items to Feature {FeatureName}", remainingWork, feature.Name);
            }
        }

        private void AssignExtrapolatedWorkToTeams(Portfolio portfolio, Feature feature, int remainingWork)
        {
            var owningTeams = portfolio.Teams.Count > 0
                ? portfolio.Teams.ToList()
                : teamRepository.GetAll().ToList();

            if (portfolio.OwningTeam != null)
            {
                logger.LogInformation("Owning Team for Portfolio is {TeamName} - using this for Default Work Assignment", portfolio.OwningTeam.Name);
                owningTeams = [portfolio.OwningTeam];
            }

            var featureOwnerValue =
                feature.GetAdditionalFieldValue(portfolio.FeatureOwnerAdditionalFieldDefinitionId);

            if (!string.IsNullOrEmpty(featureOwnerValue))
            {
                logger.LogInformation("Feature Owner Field for Project is configured - Getting value for Feature {FeatureName}: {OwnerValue}", feature.Name, featureOwnerValue);

                var featureOwners = teamRepository.GetAll().Where(t => featureOwnerValue.Contains(t.Name)).ToList();

                logger.LogInformation("Found following teams defined in Feature Owner field: {Owners}", string.Join(",", featureOwners.Select(t => t.Name)));
                if (featureOwners.Count > 0)
                {
                    owningTeams = featureOwners;
                }
            }

            var numberOfTeams = owningTeams.Count;
            if (numberOfTeams == 0)
            {
                logger.LogWarning("No teams available for extrapolation of feature {FeatureName} in portfolio {PortfolioName}", feature.Name, portfolio.Name);
                return;
            }

            var buckets = SplitIntoBuckets(remainingWork, numberOfTeams);
            for (var index = 0; index < numberOfTeams; index++)
            {
                var team = owningTeams[index];
                var totalWork = buckets[index];
                feature.AddOrUpdateWorkForTeam(team, totalWork, totalWork);

                logger.LogInformation("Added {TotalWork} Items for Feature {FeatureName} to Team {TeamName}", totalWork, feature.Name, team.Name);
            }
        }

        private int GetExtrapolatedRemainingWork(Portfolio project, Feature feature)
        {
            if (feature.EstimatedSize > 0)
            {
                return feature.EstimatedSize;
            }

            return GetDefaultRemainingWork(project);
        }

        private int GetDefaultRemainingWork(Portfolio project)
        {
            if (defaultWorkItemsBasedOnPercentile.TryGetValue(project.Id, out var defaultItems))
            {
                return defaultItems;
            }

            defaultItems = project.DefaultAmountOfWorkItemsPerFeature;

            if (project.UsePercentileToCalculateDefaultAmountOfWorkItems)
            {
                logger.LogInformation("Using Percentile to Calculate Default Amount of Work Items for Project {Project}", project.Name);

                /* Use ProjectMetricsService to Get Values */
                var endDate = DateTime.UtcNow;

                var historyInDays = project.PercentileHistoryInDays ?? 90;
                var startDate = DateTime.UtcNow.AddDays(-historyInDays);
                var closedFeatures = portfolioMetricsService.GetCycleTimeDataForPortfolio(project, startDate, endDate);

                var historicalFeatureSize = closedFeatures.Where(f => f.Size > 0).Select(f => f.Size);

                logger.LogInformation("Features had following number of child items: {ChildItems}", string.Join(",", historicalFeatureSize));

                if (historicalFeatureSize.Any())
                {
                    defaultItems = PercentileCalculator.CalculatePercentile(historicalFeatureSize.ToList(), project.DefaultWorkItemPercentile);

                    logger.LogInformation("{Percentile} Percentile Based on Last {Days} days is {DefaultItems}", project.DefaultWorkItemPercentile, project.PercentileHistoryInDays, defaultItems);
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

        private async Task RefreshFeatures(Portfolio project)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystemConnection.WorkTrackingSystem);

            var features = new List<Feature>();

            foreach (var feature in await workItemService.GetFeaturesForProject(project))
            {
                var featureFromDatabase = AddOrUpdateFeature(feature);

                AddProjectToFeature(featureFromDatabase, project);
                features.Add(featureFromDatabase);
            }

            project.UpdateFeatures(features.OrderBy(f => f, new FeatureComparer()));

            await featureRepository.Save();
        }

        private Feature AddOrUpdateFeature(Feature feature)
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
                logger.LogDebug("Updated Existing Feature {FeatureName}", feature.Name);
            }

            return featureFromDatabase;
        }

        private async Task RefreshParentFeatures(Portfolio project)
        {
            var workItemService = GetWorkItemServiceForWorkTrackingSystem(project.WorkTrackingSystemConnection.WorkTrackingSystem);
            var parentFeatureIds = project.Features.Where(f => !string.IsNullOrEmpty(f.ParentReferenceId)).Select(f => f.ParentReferenceId).Distinct().ToList();

            if (parentFeatureIds.Count == 0)
            {
                logger.LogDebug("No Parent Features found for Project {ProjectName}", project.Name);
                return;
            }

            var parentFeatures = await workItemService.GetParentFeaturesDetails(project, parentFeatureIds);

            foreach (var parentFeature in parentFeatures)
            {
                parentFeature.IsParentFeature = true;

                AddOrUpdateFeature(parentFeature);
            }

            await featureRepository.Save();
        }

        private static void AddProjectToFeature(Feature feature, Portfolio project)
        {
            var featureIsAddedToProject = feature.Portfolios.Exists(p => p.Id == project.Id);
            if (!featureIsAddedToProject)
            {
                feature.Portfolios.Add(project);
            }
        }

        private IWorkTrackingConnector GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            return workTrackingConnectorFactory.GetWorkTrackingConnector(workTrackingSystem);
        }
    }
}
