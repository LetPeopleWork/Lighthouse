using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Concurrent;
using System.Net;
using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using LighthouseWorkItem = Lighthouse.Backend.Models.WorkItem;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    public class AzureDevOpsWorkTrackingConnector : IWorkTrackingConnector
    {
        private const int maxChunkSize = 200;
        private readonly int requestTimeoutInSeconds = 100;

        private readonly ILogger<AzureDevOpsWorkTrackingConnector> logger;
        private readonly ICryptoService cryptoService;

        private static readonly ConcurrentDictionary<string, VssConnection> ConnectionCache = new();
        private static readonly ConcurrentDictionary<string, WorkItemTrackingHttpClient> ClientCache = new();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> OrgLimiters = new();

        private SemaphoreSlim GetLimiter(string url) => OrgLimiters.GetOrAdd(new Uri(url).Host, _ => new SemaphoreSlim(6));

        public AzureDevOpsWorkTrackingConnector(ILogger<AzureDevOpsWorkTrackingConnector> logger, ICryptoService cryptoService, IAppSettingService appSettingService)
        {
            this.logger = logger;
            this.cryptoService = cryptoService;

            var workTrackingSystemSettings = appSettingService.GetWorkTrackingSystemSettings();
            if (workTrackingSystemSettings.OverrideRequestTimeout)
            {
                requestTimeoutInSeconds = workTrackingSystemSettings.RequestTimeoutInSeconds;
            }
        }

        public async Task<IEnumerable<LighthouseWorkItem>> GetWorkItemsForTeam(Team team)
        {
            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);

            var workItemQuery = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery, team.ParentOverrideField ?? string.Empty)}";

            var adoWorkItems = await FetchAdoWorkItemsByQuery(team, workItemQuery, team.ParentOverrideField ?? string.Empty);
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

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.WorkItemQuery, project.ParentOverrideField ?? string.Empty);
            var features = await GetFeaturesForProjectByQuery(project, query);

            logger.LogInformation("Found Features with IDs {FeatureIds}", string.Join(", ", features.Select(f => f.ReferenceId)));

            return features;
        }

        public async Task<List<Feature>> GetParentFeaturesDetails(Project project, IEnumerable<string> parentFeatureIds)
        {
            logger.LogInformation("Getting Parent Features with IDs {ParentFeatureIds} for Project {ProjectName}", string.Join(", ", parentFeatureIds), project.Name);

            var extraFieldsQuery = string.Empty;
            if (!string.IsNullOrEmpty(project.ParentOverrideField))
            {
                extraFieldsQuery = $", [{project.ParentOverrideField}]";
            }
            var whereClause = string.Join(" OR ", parentFeatureIds.Select(id => $"[{AzureDevOpsFieldNames.Id}] = {id}"));

            var query = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}]{extraFieldsQuery} FROM WorkItems WHERE {whereClause}";

            var features = await GetFeaturesForProjectByQuery(project, query);

            logger.LogInformation("Found Parent Features with IDs {ParentFeatureIds}", string.Join(", ", features.Select(f => f.ReferenceId)));
            return features;
        }

        public async Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Additional Items Query '{Query}'", team.Name, string.Join(", ", team.WorkItemTypes), additionalQuery);

            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var workItemsQuery = $"{PrepareQuery(team.WorkItemTypes, team.AllStates, additionalQuery, team.ParentOverrideField ?? string.Empty)} AND {team.WorkItemQuery}";

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
                var witClient = GetClientService(team.WorkTrackingSystemConnection);
                var workItems = await GetWorkItemReferencesByQuery(witClient, query);

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

        private async Task<List<Feature>> GetFeaturesForProjectByQuery(Project project, string query)
        {
            var adoWorkItems = await FetchAdoWorkItemsByQuery(project, query, project.SizeEstimateField ?? string.Empty, project.FeatureOwnerField ?? string.Empty, project.ParentOverrideField ?? string.Empty);

            var parentReferencesTask = GetParentReferenceForWorkItems(adoWorkItems, project);

            var workItemBase = await ConvertAdoWorkItemToLighthouseWorkItemBase(adoWorkItems, project);

            var estimatedSizes = ExtractFieldValue(adoWorkItems, project.SizeEstimateField ?? string.Empty);
            var featureOwners = ExtractFieldValue(adoWorkItems, project.FeatureOwnerField ?? string.Empty);

            var parentReferences = await parentReferencesTask;
            var features = new List<Feature>();

            foreach (var workItem in workItemBase)
            {
                workItem.ParentReferenceId = parentReferences[workItem.ReferenceId];
                var estimatedSize = GetEstimatedSizeForItem(estimatedSizes[workItem.ReferenceId]);

                var feature = new Feature(workItem)
                {
                    EstimatedSize = estimatedSize,
                    OwningTeam = featureOwners[workItem.ReferenceId]
                };

                features.Add(feature);
            }

            return features;
        }

        private async Task<IEnumerable<AdoWorkItem>> FetchAdoWorkItemsByQuery(IWorkItemQueryOwner workItemQueryOwner, string query, params string[] additionalFields)
        {
            try
            {
                var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);
                var workItemReferences = await GetWorkItemReferencesByQuery(witClient, query);

                if (!workItemReferences.Any())
                    return Array.Empty<AdoWorkItem>();

                return await GetAdoWorkItemsById(workItemReferences.Select(wi => wi.Id), workItemQueryOwner, additionalFields);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch ADO work items for query '{Query}'", query);
                return Array.Empty<AdoWorkItem>();
            }
        }

        private async Task<T> ExecuteWithThrottle<T>(WorkItemTrackingHttpClient witClient, string url, Func<Task<T>> action)
        {
            var limiter = GetLimiter(url);
            await limiter.WaitAsync();
            try
            {
                return await ExecuteWithRetry(action);
            }
            finally
            {
                limiter.Release();
            }
        }

        private static bool IsRateLimited(VssServiceException ex)
        {
            var msg = ex.Message ?? string.Empty;
            return msg.Contains("Rate limits", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("exceeding usage of resource 'Concurrency'", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action)
        {
            var delay = TimeSpan.FromSeconds(1);
            for (var attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (VssServiceException ex) when (IsRateLimited(ex))
                {
                    await Task.Delay(delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)));
                    delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
                }
                catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(delay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)));
                    delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
                }
            }
            return await action();
        }

        private async Task<IEnumerable<WorkItemBase>> ConvertAdoWorkItemToLighthouseWorkItemBase(IEnumerable<AdoWorkItem> adoWorkItems, IWorkItemQueryOwner workItemQueryOwner)
        {
            var throttler = new SemaphoreSlim(8);
            var tasks = adoWorkItems.Select(async wi =>
            {
                await throttler.WaitAsync();
                try { return await ConvertAdoWorkItemToLighthouseWorkItem(wi, workItemQueryOwner); }
                finally { throttler.Release(); }
            });
            return await Task.WhenAll(tasks);
        }

        private async Task<IEnumerable<WorkItemReference>> GetWorkItemReferencesByQuery(WorkItemTrackingHttpClient witClient, string query)
        {
            try
            {
                var result = await ExecuteWithThrottle(witClient, witClient.BaseAddress!.ToString(),
                    () => witClient.QueryByWiqlAsync(new Wiql { Query = query }));

                return result.WorkItems ?? Array.Empty<WorkItemReference>();
            }
            catch (VssServiceException ex)
            {
                logger.LogError(ex, "Error while querying Work Items with Query '{Query}'", query);
                return Array.Empty<WorkItemReference>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while querying Work Items with Query '{Query}'", query);
                return Array.Empty<WorkItemReference>();
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
                AzureDevOpsFieldNames.Tags,
            };

            fields.AddRange(additionalFields.Where(f => !string.IsNullOrEmpty(f)));

            return await GetWorkItemsInChunks(workItemIds, witClient, WorkItemExpand.Links, fields);
        }

        private async Task<IEnumerable<AdoWorkItem>> GetWorkItemsInChunks(IEnumerable<int> workItemIds, WorkItemTrackingHttpClient witClient, WorkItemExpand expand, IEnumerable<string> fields)
        {
            var url = witClient.BaseAddress!.ToString();
            var workItems = new List<AdoWorkItem>();

            foreach (var chunk in workItemIds.Chunk(maxChunkSize))
            {
                var result = await ExecuteWithThrottle(witClient, url, () => witClient.GetWorkItemsAsync(chunk, fields, expand: expand));
                workItems.AddRange(result);
            }

            return workItems;
        }

        private async Task<WorkItemBase> ConvertAdoWorkItemToLighthouseWorkItem(AdoWorkItem workItem, IWorkItemQueryOwner workItemQueryOwner)
        {
            var state = workItem.ExtractStateFromWorkItem();
            var stateCategory = workItemQueryOwner.MapStateToStateCategory(state);

            var (startedDate, closedDate) = await GetStartedAndClosedDateForWorkItem(workItemQueryOwner, stateCategory, workItem.Id);

            return new WorkItemBase
            {
                ReferenceId = $"{workItem.Id}",
                Name = workItem.ExtractTitleFromWorkItem(),
                Type = workItem.ExtractTypeFromWorkItem(),
                State = state,
                StateCategory = stateCategory,
                Url = workItem.ExtractUrlFromWorkItem(),
                Order = workItem.ExtractStackRankFromWorkItem(),
                CreatedDate = workItem.ExtractCreatedDateFromWorkItem(),
                StartedDate = startedDate,
                ClosedDate = closedDate,
                Tags = workItem.ExtractTagsFromWorkItem(),
            };
        }

        private async Task<(DateTime? startedDate, DateTime? closedDate)> GetStartedAndClosedDateForWorkItem(IWorkItemQueryOwner workItemQueryOwner, StateCategories stateCategory, int? workItemId)
        {
            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);
            DateTime? startedDate = null;
            DateTime? closedDate = null;

            if (stateCategory == StateCategories.Done)
            {
                startedDate = await GetStateTransitionDateThrottled(witClient, workItemId, workItemQueryOwner.DoingStates, workItemQueryOwner.DoneStates);
                closedDate = await GetStateTransitionDateThrottled(witClient, workItemId, workItemQueryOwner.DoneStates, []);
            }
            else if (stateCategory == StateCategories.Doing)
            {
                startedDate = await GetStateTransitionDateThrottled(witClient, workItemId, workItemQueryOwner.DoingStates, workItemQueryOwner.DoneStates);
            }

            if (startedDate == null && closedDate != null)
            {
                startedDate = closedDate;
            }

            return (startedDate, closedDate);
        }

        private async Task<DateTime?> GetStateTransitionDateThrottled(WorkItemTrackingHttpClient witClient, int? workItemId, List<string> targetStates, List<string> statesToIgnore)
        {
            if (!workItemId.HasValue) return null;

            var revisions = await ExecuteWithThrottle(witClient, witClient.BaseAddress!.ToString(), () => witClient.GetRevisionsAsync(workItemId.Value));
            var movedToStateCategory = new List<DateTime>();
            var previousState = string.Empty;

            foreach (var revision in revisions)
            {
                if (RevisionWasChangingState(revision, out var result))
                {
                    var isRelevantCategory = targetStates.IsItemInList(result.state) && !targetStates.IsItemInList(previousState) && !statesToIgnore.IsItemInList(previousState);
                    if (isRelevantCategory) movedToStateCategory.Add(result.changedDate);
                    previousState = result.state;
                }
            }

            var last = movedToStateCategory.OrderByDescending(d => d).FirstOrDefault();
            return last == default ? null : DateTime.SpecifyKind(last, DateTimeKind.Utc);
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

        private async Task<Dictionary<string, string>> GetParentReferenceForWorkItems(IEnumerable<AdoWorkItem> adoWorkItems, WorkTrackingSystemOptionsOwner workTrackingSystemOptionOwner)
        {
            var itemIds = adoWorkItems.Select(wi => wi.Id ?? -1).Where(i => i >= 0).ToList();

            if (itemIds.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            logger.LogDebug("Getting Parent Ids for Work Items with IDs {ItemIds}", string.Join(",", itemIds));

            if (!string.IsNullOrEmpty(workTrackingSystemOptionOwner.ParentOverrideField))
            {
                logger.LogDebug("Getting Parent Ids for Work Items with Additional Related Field {AdditionalRelatedField}", workTrackingSystemOptionOwner.ParentOverrideField);
                return ExtractFieldValue(adoWorkItems, workTrackingSystemOptionOwner.ParentOverrideField);
            }

            return await GetParentReferencesFromRelationFields(workTrackingSystemOptionOwner, itemIds);
        }

        private async Task<Dictionary<string, string>> GetParentReferencesFromRelationFields(WorkTrackingSystemOptionsOwner workTrackingSystemOptionOwner, List<int> itemIds)
        {
            logger.LogDebug("Getting Parent Ids for Work Items with Parent Field");

            var parentReferences = new Dictionary<string, string>();
            foreach (var id in itemIds)
            {
                parentReferences.Add($"{id}", string.Empty);
            }

            var witClient = GetClientService(workTrackingSystemOptionOwner.WorkTrackingSystemConnection);
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

        private WorkItemTrackingHttpClient GetClientService(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var url = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            var encryptedPersonalAccessToken = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);
            var personalAccessToken = cryptoService.Decrypt(encryptedPersonalAccessToken);
            var key = $"{url}|{personalAccessToken}";

            var connection = ConnectionCache.GetOrAdd(key, _ =>
            {
                var c = CreateConnection(url, personalAccessToken);
                c.Settings.SendTimeout = TimeSpan.FromSeconds(requestTimeoutInSeconds);
                return c;
            });

            return ClientCache.GetOrAdd(key, _ => connection.GetClient<WorkItemTrackingHttpClient>());
        }

        private static VssConnection CreateConnection(string azureDevOpsUrl, string personalAccessToken)
        {
            var azureDevOpsUri = new Uri(azureDevOpsUrl);
            var credentials = new VssBasicCredential(personalAccessToken, string.Empty);
            return new VssConnection(azureDevOpsUri, credentials);
        }
    }
}
