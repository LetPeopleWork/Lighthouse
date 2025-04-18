﻿using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking.AzureDevOps;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using LighthouseWorkItem = Lighthouse.Backend.Models.WorkItem;

namespace Lighthouse.Backend.Services.Implementation.WorkItemServices
{
    public class AzureDevOpsWorkItemService : IWorkItemService
    {
        private const int maxChunkSize = 200;

        private readonly ILogger<AzureDevOpsWorkItemService> logger;
        private readonly ICryptoService cryptoService;

        public AzureDevOpsWorkItemService(ILogger<AzureDevOpsWorkItemService> logger, ICryptoService cryptoService)
        {
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        public async Task<IEnumerable<LighthouseWorkItem>> GetChangedWorkItemsSinceLastTeamUpdate(Team team)
        {
            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var lastUpdatedFilter = PrepareLastUpdatedQuery(team.TeamUpdateTime);
            var workItemQuery = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery, team.AdditionalRelatedField ?? string.Empty)} {lastUpdatedFilter}";

            var adoWorkItems = await FetchAdoWorkItemsByQuery(team, workItemQuery, team.AdditionalRelatedField ?? string.Empty);
            var parentReferencesTask = GetParentReferenceForWorkItems(adoWorkItems, team);
            var workItems = await ConvertAdoWorkItemToLighthouseWorkItemBase(adoWorkItems, team);

            var parentReferences = await parentReferencesTask;
            foreach (var workItem in workItems)
            {
                workItem.ParentReferenceId = parentReferences[workItem.ReferenceId];
            }

            return workItems.Select(workItem =>
            {
                return new LighthouseWorkItem(workItem, team);
            });
        }

        public async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            logger.LogInformation("Getting Features of Type {WorkItemTypes} and Query '{Query}'", string.Join(", ", project.WorkItemTypes), project.WorkItemQuery);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.WorkItemQuery);
            var adoWorkItems = await FetchAdoWorkItemsByQuery(project, query, project.SizeEstimateField ?? string.Empty, project.FeatureOwnerField ?? string.Empty);
            var workItemBase = await ConvertAdoWorkItemToLighthouseWorkItemBase(adoWorkItems, project);

            var estimatedSizes = ExtractFieldValue(adoWorkItems, project.SizeEstimateField ?? string.Empty);
            var featureOwners = ExtractFieldValue(adoWorkItems, project.FeatureOwnerField ?? string.Empty);

            var features = new List<Feature>();

            foreach (var workItem in workItemBase)
            {
                var estimatedSize = GetEstimatedSizeForItem(estimatedSizes[workItem.ReferenceId]);

                var feature = new Feature(workItem)
                {
                    EstimatedSize = estimatedSize,
                    OwningTeam = featureOwners[workItem.ReferenceId]
                };

                features.Add(feature);
            }

            logger.LogInformation("Found Features with IDs {FeatureIds}", string.Join(", ", features.Select(f => f.ReferenceId)));

            return features;
        }

        public async Task<Dictionary<string, int>> GetHistoricalFeatureSize(Project project)
        {
            var historicalFeatureSize = new Dictionary<string, int>();

            logger.LogInformation("Getting Child Items for Features in Project {Project} for Work Item Types {WorkItemTypes} and Query '{Query}'", project.Name, string.Join(", ", project.WorkItemTypes), project.HistoricalFeaturesWorkItemQuery);

            var witClient = GetClientService(project.WorkTrackingSystemConnection);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.HistoricalFeaturesWorkItemQuery);
            var features = await GetWorkItemReferencesByQuery(witClient, query);

            foreach (var featureId in features.Select(f => f.Id.ToString()))
            {
                historicalFeatureSize.Add(featureId, 0);

                foreach (var team in project.Teams)
                {
                    var totalItems = await GetRelatedWorkItems(team, featureId);
                    historicalFeatureSize[featureId] += totalItems;
                }
            }

            var emptyFeatures = historicalFeatureSize.Where(kvp => kvp.Value <= 0).Select(kvp => kvp.Key).ToList();
            foreach (var featureId in emptyFeatures)
            {
                historicalFeatureSize.Remove(featureId);
            }

            return historicalFeatureSize;
        }

        public async Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Additional Items Query '{Query}'", team.Name, string.Join(", ", team.WorkItemTypes), additionalQuery);

            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var workItemsQuery = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, additionalQuery, team.AdditionalRelatedField ?? string.Empty)} AND {team.WorkItemQuery}";

            var matchingWorkItems = await GetWorkItemReferencesByQuery(witClient, workItemsQuery);

            var matchingWorkItemsIds = matchingWorkItems.Select(x => x.Id.ToString()).ToList();
            logger.LogDebug("Found following Work Items {MatchingWorkItems}", string.Join(", ", matchingWorkItemsIds));

            return matchingWorkItemsIds;
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            logger.LogInformation("Getting Adjacent Order Index for items {ExistingItemsOrder} in order {RelativeOrder}", string.Join(", ", existingItemsOrder), relativeOrder);

            string? result;
            if (!existingItemsOrder.Any())
            {
                result = "0";
            }
            else
            {
                var orderAsInt = ConvertToIntegers(existingItemsOrder);

                if (relativeOrder == RelativeOrder.Above)
                {
                    var highestOrder = orderAsInt.Max();
                    result = $"{highestOrder + 1}";
                }
                else
                {
                    var lowestOrder = orderAsInt.Min();
                    result = $"{lowestOrder - 1}";
                }
            }

            logger.LogInformation("Adjacent Order Index for items {ExistingItemsOrder} in order {RelativeOrder}: {Result}", string.Join(", ", existingItemsOrder), relativeOrder, result);

            return result;
        }

        public async Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            try
            {
                var witClient = GetClientService(connection);
                var query = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.Id}] = 12";

                await witClient.QueryByWiqlAsync(new Wiql() { Query = query });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateTeamSettings(Team team)
        {
            try
            {
                logger.LogInformation("Validating Team Settings for Team {TeamName} and Query {Query}", team.Name, team.WorkItemQuery);

                var query = PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery);
                var workItems = await FetchAdoWorkItemsByQuery(team, query, team.AdditionalRelatedField ?? string.Empty);

                var workItemCount = workItems.Count();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Work Items with specified Query", workItemCount);

                return workItemCount > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Team Settings for Team {TeamName}", team.Name);
                return false;
            }
        }

        public async Task<bool> ValidateProjectSettings(Project project)
        {
            try
            {
                logger.LogInformation("Validating Project Settings for Project {ProjectName} and Query {Query}", project.Name, project.WorkItemQuery);

                var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.WorkItemQuery);

                var workItems = await FetchAdoWorkItemsByQuery(project, query, project.SizeEstimateField ?? string.Empty, project.FeatureOwnerField ?? string.Empty);
                var workItemCount = workItems.Count();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Features with specified Query", workItemCount);

                return workItemCount > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Project Settings for Project {ProjectName}", project.Name);
                return false;
            }
        }

        private async Task<int> GetRelatedWorkItems(Team team, string relatedWorkItemId)
        {
            var witClient = GetClientService(team.WorkTrackingSystemConnection);
            var allItemsQuery = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery)} {PrepareRelatedItemQuery(relatedWorkItemId, team.AdditionalRelatedField)}";

            var totalWorkItems = await GetWorkItemReferencesByQuery(witClient, allItemsQuery);

            return totalWorkItems.Count();
        }

        private async Task<IEnumerable<AdoWorkItem>> FetchAdoWorkItemsByQuery(IWorkItemQueryOwner workItemQueryOwner, string query, params string[] additionalFields)
        {
            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);
            var workItemReferences = await GetWorkItemReferencesByQuery(witClient, query);

            var adoWorkItems = await GetAdoWorkItemsById(workItemReferences.Select(wi => wi.Id), workItemQueryOwner, additionalFields);

            return adoWorkItems;
        }

        private async Task<IEnumerable<WorkItemBase>> ConvertAdoWorkItemToLighthouseWorkItemBase(IEnumerable<AdoWorkItem> adoWorkItems, IWorkItemQueryOwner workItemQueryOwner)
        {
            var tasks = adoWorkItems.Select(async adoWorkItem =>
            {
                var workItemBase = await ConvertAdoWorkItemToLighthouseWorkItem(adoWorkItem, workItemQueryOwner);
                return workItemBase;
            });

            var workItems = await Task.WhenAll(tasks);

            return workItems;
        }

        private async Task<IEnumerable<WorkItemReference>> GetWorkItemReferencesByQuery(WorkItemTrackingHttpClient witClient, string query)
        {
            try
            {
                var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = query });
                return queryResult.WorkItems;
            }
            catch (VssServiceException ex)
            {
                logger.LogError(ex, "Error while querying Work Items with Query '{Query}'", query);
                return [];
            }
        }

        private async Task<IEnumerable<AdoWorkItem>> GetAdoWorkItemsById(IEnumerable<int> workItemIds, IWorkItemQueryOwner workItemQueryOwner, params string[] additionalFields)
        {
            if (!workItemIds.Any())
            {
                return [];
            }

            logger.LogDebug("Getting Work Item with IDs {ItemIds}", string.Join(",", workItemIds));

            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);

            var fields = new List<string>
            {
                AzureDevOpsFieldNames.State,
                AzureDevOpsFieldNames.Title,
                AzureDevOpsFieldNames.WorkItemType,
                AzureDevOpsFieldNames.StackRank,
                AzureDevOpsFieldNames.BacklogPriority,
                AzureDevOpsFieldNames.CreatedDate,
            };

            fields.AddRange(additionalFields.Where(f => !string.IsNullOrEmpty(f)));

            return await GetWorkItemsInChunks(workItemIds, witClient, WorkItemExpand.Links, fields);
        }

        private async Task<IEnumerable<AdoWorkItem>> GetWorkItemsInChunks(IEnumerable<int> workItemIds, WorkItemTrackingHttpClient witClient, WorkItemExpand expand, IEnumerable<string> fields)
        {
            var workItems = new List<AdoWorkItem>();

            foreach (var chunk in workItemIds.Chunk(maxChunkSize))
            {
                logger.LogDebug("Fetching chunk of Work Item IDs: {ChunkIds}", string.Join(",", chunk));
                var chunkWorkItems = await witClient.GetWorkItemsAsync(chunk, fields, expand: expand);
                workItems.AddRange(chunkWorkItems);
            }

            return workItems;
        }

        private async Task<WorkItemBase> ConvertAdoWorkItemToLighthouseWorkItem(AdoWorkItem workItem, IWorkItemQueryOwner workItemQueryOwner)
        {
            var state = workItem.ExtractStateFromWorkItem();

            var (startedDate, closedDate) = await GetStartedAndClosedDateForWorkItem(workItemQueryOwner, workItem.Id);

            return new WorkItemBase
            {
                ReferenceId = $"{workItem.Id}",
                Name = workItem.ExtractTitleFromWorkItem(),
                Type = workItem.ExtractTypeFromWorkItem(),
                State = state,
                StateCategory = workItemQueryOwner.MapStateToStateCategory(state),
                Url = workItem.ExtractUrlFromWorkItem(),
                Order = workItem.ExtractStackRankFromWorkItem(),
                CreatedDate = workItem.ExtractCreatedDateFromWorkItem(),
                StartedDate = startedDate,
                ClosedDate = closedDate,
            };
        }

        private async Task<(DateTime? startedDate, DateTime? closedDate)> GetStartedAndClosedDateForWorkItem(IWorkItemQueryOwner workItemQueryOwner, int? workItemId)
        {
            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);
            var startedDate = await GetStateTransitionDate(witClient, workItemId, workItemQueryOwner.DoingStates);
            var closedDate = await GetStateTransitionDate(witClient, workItemId, workItemQueryOwner.DoneStates);

            // It can happen that no started date is set if an item is created directly in closed state. Assume that the closed date is the started date in this case.
            if (startedDate == null && closedDate != null)
            {
                startedDate = closedDate;
            }

            return (startedDate, closedDate);
        }

        private static async Task<DateTime?> GetStateTransitionDate(WorkItemTrackingHttpClient witClient, int? workItemId, List<string> states)
        {
            DateTime? latestStateChangeDate = null;
            string? previousState = null;

            if (!workItemId.HasValue)
            {
                return latestStateChangeDate;
            }

            var revisions = await witClient.GetRevisionsAsync(workItemId.Value);

            foreach (var revision in revisions)
            {
                if (RevisionWasChangingState(revision, out (string state, DateTime changedDate) result))
                {
                    var isRelevantCategory = states.Contains(result.state) && (previousState == null || !states.Contains(previousState));
                    var isRelevantStateChange = !latestStateChangeDate.HasValue || result.changedDate > latestStateChangeDate.Value;

                    if (isRelevantStateChange && isRelevantCategory)
                    {
                        latestStateChangeDate = result.changedDate;
                    }

                    previousState = result.state;
                }
            }

            return latestStateChangeDate;
        }

        private static bool RevisionWasChangingState(AdoWorkItem revision, out (string state, DateTime changedDate) result)
        {
            result.state = string.Empty;
            result.changedDate = DateTime.MinValue;

            if (revision.Fields.TryGetValue(AzureDevOpsFieldNames.State, out var stateValue) &&
                    revision.Fields.TryGetValue(AzureDevOpsFieldNames.ChangedDate, out var changedDateValue))
            {
                result.state = stateValue.ToString() ?? string.Empty;
                result.changedDate = (DateTime?)changedDateValue ?? DateTime.MinValue;
            }

            return !string.IsNullOrEmpty(result.state) && result.changedDate != DateTime.MinValue;
        }

        private async Task<Dictionary<string, string>> GetParentReferenceForWorkItems(IEnumerable<AdoWorkItem> adoWorkItems, Team team)
        {
            var itemIds = adoWorkItems.Select(wi => wi.Id ?? -1).Where(i => i >= 0).ToList();

            if (itemIds.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            logger.LogDebug("Getting Parent Ids for Work Items with IDs {ItemIds}", string.Join(",", itemIds));

            if (!string.IsNullOrEmpty(team.AdditionalRelatedField))
            {
                logger.LogDebug("Getting Parent Ids for Work Items with Additional Related Field {AdditionalRelatedField}", team.AdditionalRelatedField);
                return ExtractFieldValue(adoWorkItems, team.AdditionalRelatedField);
            }

            return await GetParentReferencesFromRelationFields(team, itemIds);
        }

        private async Task<Dictionary<string, string>> GetParentReferencesFromRelationFields(Team team, List<int> itemIds)
        {
            logger.LogDebug("Getting Parent Ids for Work Items with Parent Field");

            var parentReferences = new Dictionary<string, string>();
            foreach (var id in itemIds)
            {
                parentReferences.Add($"{id}", string.Empty);
            }

            var witClient = GetClientService(team.WorkTrackingSystemConnection);
            var workItemsWithParentRelation = await GetWorkItemsInChunks(itemIds, witClient, WorkItemExpand.Relations, []);

            foreach (var adoWorkItem in workItemsWithParentRelation)
            {
                var parentReference = adoWorkItem.ExtractParentFromWorkItem();
                parentReferences[adoWorkItem.Id.ToString() ?? "-1"] = parentReference;
            }

            return parentReferences;
        }

        private static int GetEstimatedSizeForItem(string estimatedSizeRawValue)
        {
            if (string.IsNullOrEmpty(estimatedSizeRawValue))
            {
                return 0;
            }

            if (int.TryParse(estimatedSizeRawValue, out var estimatedSize))
            {
                return estimatedSize;
            }

            // Try parsing double because for sure someone will have the brilliant idea to make this a decimal
            if (double.TryParse(estimatedSizeRawValue, out var estimateAsDouble))
            {
                return (int)estimateAsDouble;
            }

            return 0;
        }

        private static Dictionary<string, string> ExtractFieldValue(IEnumerable<AdoWorkItem> adoWorkItems, string fieldName)
        {
            var fieldValues = new Dictionary<string, string>();
            foreach (var adoWorkItem in adoWorkItems)
            {
                var key = adoWorkItem.Id?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                fieldValues[key] = string.Empty;

                if (!string.IsNullOrEmpty(fieldName) && adoWorkItem.Fields.TryGetValue(fieldName, out var fieldValue))
                {
                    fieldValues[key] = fieldValue.ToString() ?? string.Empty;
                }
            }

            return fieldValues;
        }

        private static List<int> ConvertToIntegers(IEnumerable<string> orderAsStrings)
        {
            var orderAsInt = new List<int>();

            foreach (var order in orderAsStrings)
            {
                if (int.TryParse(order, out int number))
                {
                    orderAsInt.Add(number);
                }
            }

            return orderAsInt;
        }

        private static string PrepareQuery(
            IEnumerable<string> includedWorkItemTypes,
            IEnumerable<string> includedStates,
            string query)
        {
            return PrepareQuery(includedWorkItemTypes, includedStates, query, string.Empty);
        }

        private static string PrepareQuery(
            IEnumerable<string> includedWorkItemTypes,
            IEnumerable<string> includedStates,
            string query,
            string extraField)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(includedWorkItemTypes);
            var stateQuery = PrepareStateQuery(includedStates);

            var extraFieldsQuery = string.Empty;
            if (!string.IsNullOrEmpty(extraField))
            {
                extraFieldsQuery = $", [{extraField}]";
            }

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}]{extraFieldsQuery} FROM WorkItems WHERE ({query}) " +
                $"{workItemsQuery} " +
                $"{stateQuery}";

            return wiql;
        }

        private static string PrepareWorkItemTypeQuery(IEnumerable<string> workItemTypes)
        {
            return PrepareGenericQuery(workItemTypes, AzureDevOpsFieldNames.WorkItemType, "OR", "=");
        }

        private static string PrepareStateQuery(IEnumerable<string> includedStates)
        {
            return PrepareGenericQuery(includedStates, AzureDevOpsFieldNames.State, "OR", "=");
        }

        private static string PrepareLastUpdatedQuery(DateTime lastUpdated)
        {
            var query = string.Empty;

            var updateHorizon = lastUpdated;
            if (lastUpdated != DateTime.MinValue)
            {
                query = $" AND ([{AzureDevOpsFieldNames.ChangedDate}] >= '{updateHorizon:yyyy-MM-dd}T00:00:00.0000000Z')";
            }

            return query;
        }

        private static string PrepareGenericQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
        {
            var query = string.Join($" {queryOperator} ", options.Select(options => $"[{fieldName}] {queryComparison} '{options}'"));

            if (options.Any())
            {
                query = $"AND ({query}) ";
            }
            else
            {
                query = string.Empty;
            }

            return query;
        }

        private static string PrepareRelatedItemQuery(string relatedItemId, string? additionalRelatedField)
        {
            var additionalRelatedFieldQuery = string.Empty;
            if (!string.IsNullOrEmpty(additionalRelatedField))
            {
                additionalRelatedFieldQuery = $"OR [{additionalRelatedField}] = '{relatedItemId}'";
            }

            return $"AND ([System.Parent] = '{relatedItemId}' {additionalRelatedFieldQuery})";
        }

        private WorkItemTrackingHttpClient GetClientService(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var url = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            var encryptedPersonalAccessToken = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);

            var personalAccessToken = cryptoService.Decrypt(encryptedPersonalAccessToken);

            var connection = CreateConnection(url, personalAccessToken);
            return connection.GetClient<WorkItemTrackingHttpClient>();
        }

        private static VssConnection CreateConnection(string azureDevOpsUrl, string personalAccessToken)
        {
            var azureDevOpsUri = new Uri(azureDevOpsUrl);
            var credentials = new VssBasicCredential(personalAccessToken, string.Empty);

            return new VssConnection(azureDevOpsUri, credentials);
        }
    }
}
