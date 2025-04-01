using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
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
        private readonly Cache<string, AdoWorkItem> workItemCache = new Cache<string, AdoWorkItem>();

        private readonly ILogger<AzureDevOpsWorkItemService> logger;
        private readonly ICryptoService cryptoService;

        public AzureDevOpsWorkItemService(ILogger<AzureDevOpsWorkItemService> logger, ICryptoService cryptoService)
        {
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        // TODO: Refactor this horrible code
        public async Task<IEnumerable<LighthouseWorkItem>> UpdateWorkItemsForTeam(Team team)
        {
            var workItems = new List<LighthouseWorkItem>();

            logger.LogInformation("Updating Work Items for Team {TeamName}", team.Name);
            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var workItemQuery = PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery, team.AdditionalRelatedField ?? string.Empty);

            var updateHorizon = team.TeamUpdateTime;
            if (team.TeamUpdateTime != DateTime.MinValue)
            {
                logger.LogInformation("Team was last updated on {LastUpdateTime} - Getting all items that were changed since then", team.TeamUpdateTime);
                workItemQuery += $" AND ([{AzureDevOpsFieldNames.ChangedDate}] >= '{updateHorizon:yyyy-MM-dd}T00:00:00.0000000Z')";
            }
            else
            {
                logger.LogInformation("No Update Time found - Getting all Work Items that macht the query (this might take a while...)");
            }

            var workItemReferences = await GetWorkItemsByQuery(witClient, workItemQuery);
            foreach (var workItemReference in workItemReferences)
            {
                var workItemBase = await GetWorkItemById(workItemReference.Id.ToString(), team, team.AdditionalRelatedField);

                var adoWorkItem = await witClient.GetWorkItemAsync(workItemReference.Id, expand: WorkItemExpand.Relations);
                var parentId = GetParentIdForWorkItem(adoWorkItem, team.AdditionalRelatedField);

                var workItem = new LighthouseWorkItem(workItemBase, team, parentId);
                workItems.Add(workItem);
            }

            return workItems;
        }

        private string GetParentIdForWorkItem(AdoWorkItem adoWorkItem, string? parentOverrideFieldName)
        {
            if (!string.IsNullOrEmpty(parentOverrideFieldName))
            {
                return adoWorkItem.Fields[parentOverrideFieldName].ToString() ?? string.Empty;
            }

            if (adoWorkItem.Relations != null)
            {
                foreach (var relation in adoWorkItem.Relations)
                {
                    if (relation.Attributes.TryGetValue("name", out var attributeValue) && attributeValue.ToString() == "Parent")
                    {
                        var splittedUrl = relation.Url.Split("/");
                        var parentId = splittedUrl[splittedUrl.Length - 1];

                        return parentId ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        [Obsolete]
        public async Task<int[]> GetThroughputForTeam(Team team)
        {
            logger.LogInformation("Getting Closed Work Items for Team {TeamName}", team.Name);
            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            return await GetClosedItemsPerDay(witClient, team);
        }

        [Obsolete]
        public async Task<string[]> GetClosedWorkItemsForTeam(Team team)
        {
            var witClient = GetClientService(team.WorkTrackingSystemConnection);
            return await GetClosedWorkItems(witClient, team);
        }

        // TODO: Return Feature instead of string
        public async Task<List<string>> GetFeaturesForProject(Project project)
        {
            logger.LogInformation("Getting Open Work Items for Work Items {WorkItemTypes} and Query '{Query}'", string.Join(", ", project.WorkItemTypes), project.WorkItemQuery);
            var witClient = GetClientService(project.WorkTrackingSystemConnection);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.WorkItemQuery);
            var workItems = await GetWorkItemsByQuery(witClient, query);

            var workItemReferences = workItems.Select(wi => wi.Id.ToString()).ToList();
            logger.LogInformation("Found Work Items with IDs {WorkItemIDs}", string.Join(", ", workItemReferences));

            return workItemReferences;
        }

        [Obsolete]
        public async Task<IEnumerable<int>> GetChildItemsForFeaturesInProject(Project project)
        {
            var childItemList = new List<int>();

            logger.LogInformation("Getting Child Items for Features in Project {Project} for Work Item Types {WorkItemTypes} and Query '{Query}'", project.Name, string.Join(", ", project.WorkItemTypes), project.HistoricalFeaturesWorkItemQuery);

            var witClient = GetClientService(project.WorkTrackingSystemConnection);

            var query = PrepareQuery(project.WorkItemTypes, project.AllStates, project.HistoricalFeaturesWorkItemQuery);
            var features = await GetWorkItemsByQuery(witClient, query);

            foreach (var feature in features)
            {
                var childItems = 0;
                foreach (var team in project.Teams)
                {
                    var (_, totalItems) = await GetRelatedWorkItems($"{feature.Id}", team);
                    childItems += totalItems;
                }

                childItemList.Add(childItems);
            }

            return childItemList.Where(i => i > 0);
        }

        [Obsolete]
        public async Task<IEnumerable<string>> GetFeaturesInProgressForTeam(Team team)
        {
            logger.LogInformation("Getting Features in Progress for Team {TeamName}", team.Name);

            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var parentField = AzureDevOpsFieldNames.Parent;
            if (!string.IsNullOrEmpty(team.AdditionalRelatedField))
            {
                parentField = team.AdditionalRelatedField;
            }

            var query = PrepareQuery(team.WorkItemTypes, team.DoingStates, team.WorkItemQuery, parentField);
            var workItems = await GetWorkItemsByQuery(witClient, query);

            var featuresInProgress = new List<string>();

            foreach (var workItem in workItems)
            {
                var item = await GetWorkItemById(witClient, workItem.Id.ToString(), team, parentField);

                var parentId = string.Empty;

                if (item?.Fields.ContainsKey(parentField) ?? false)
                {
                    parentId = item.Fields[parentField].ToString() ?? string.Empty;
                }

                featuresInProgress.Add(parentId);
            }

            return featuresInProgress.Distinct();
        }

        [Obsolete]
        public async Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(string featureId, Team team)
        {
            logger.LogInformation("Getting Related Work Items for Feature {FeatureId} and Team {TeamName}", featureId, team.Name);
            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var relatedWorkItems = await GetRelatedWorkItems(witClient, team, featureId);

            return relatedWorkItems;
        }

        // TODO: Make private?
        // TODO: Refactor this horrible code
        public async Task<WorkItemBase> GetWorkItemById(string itemId, IWorkItemQueryOwner workItemQueryOwner, string? parentOverrideField)
        {
            logger.LogInformation("Getting Work Item with ID {ItemId}", itemId);
            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.Id}] = '{itemId}'";

            var workItems = await GetWorkItemsByQuery(witClient, wiql);
            var workItemReference = workItems.Single();

            var fields = new List<string>
            {
                AzureDevOpsFieldNames.State,
                AzureDevOpsFieldNames.Title,
                AzureDevOpsFieldNames.WorkItemType,
                AzureDevOpsFieldNames.StackRank,
                AzureDevOpsFieldNames.BacklogPriority,
            };

            var workItem = await witClient.GetWorkItemAsync(int.Parse(itemId), fields);

            var state = workItem.Fields[AzureDevOpsFieldNames.State].ToString() ?? string.Empty;
            var workItemTitle = workItem.Fields[AzureDevOpsFieldNames.Title].ToString() ?? string.Empty;

            var url = ((ReferenceLink)workItem.Links.Links["html"])?.Href ?? string.Empty;

            var workItemOrder = string.Empty;
            if (workItem.Fields.TryGetValue(AzureDevOpsFieldNames.StackRank, out var stackRank))
            {
                workItemOrder = stackRank?.ToString() ?? string.Empty;
            }
            else if (workItem.Fields.TryGetValue(AzureDevOpsFieldNames.BacklogPriority, out var backlogPriority))
            {
                workItemOrder = backlogPriority?.ToString() ?? string.Empty;
            }

            var startedDate = await GetStateTransitionDate(witClient, !string.IsNullOrEmpty(itemId) ? int.Parse(itemId) : null, workItemQueryOwner.DoingStates);
            var closedDate = await GetStateTransitionDate(witClient, !string.IsNullOrEmpty(itemId) ? int.Parse(itemId) : null, workItemQueryOwner.DoneStates);
            if (startedDate == null && closedDate != null)
            {
                startedDate = closedDate;
            }

            return new WorkItemBase
            {
                ReferenceId = itemId,
                Name = workItem.Fields[AzureDevOpsFieldNames.Title].ToString() ?? string.Empty,
                Type = workItem.Fields[AzureDevOpsFieldNames.WorkItemType].ToString() ?? string.Empty,
                State = state,
                StateCategory = workItemQueryOwner.MapStateToStateCategory(state),
                Url = url,
                Order = workItemOrder,
                StartedDate = startedDate,
                ClosedDate = closedDate,
            };
        }

        // TODO: Return Feature instead?
        // Merge with GetFeaturesForProject
        public async Task<(string name, string order, string url, string state, DateTime? startedDate, DateTime? closedDate)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            var workItem = await GetWorkItemById(itemId, workItemQueryOwner, null);

            return (workItem.Name, workItem.Order, workItem.Url, workItem.State, workItem.StartedDate, workItem.ClosedDate);
        }

        [Obsolete]
        // TODO: Check how to handle unparented items
        public async Task<(List<string> remainingWorkItems, List<string> allWorkItems)> GetWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Unaprented Items Query '{Query}'", team.Name, string.Join(", ", workItemTypes), unparentedItemsQuery);

            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var workItemsQuery = PrepareWorkItemTypeQuery(workItemTypes);
            var doneStateQuery = PrepareStateQuery(team.DoneStates);
            var pendingStateQuery = PrepareStateQuery(team.OpenStates);

            var queryBase = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}] FROM WorkItems WHERE {unparentedItemsQuery} " +
                $"{workItemsQuery} " +
                $" AND {team.WorkItemQuery}";

            var doneWorkItemsQuery = queryBase + doneStateQuery;
            var remainingWorkItemsQuery = queryBase + pendingStateQuery;

            var doneWorkItems = await GetWorkItemsByQuery(witClient, doneWorkItemsQuery);
            var remainingWorkItems = await GetWorkItemsByQuery(witClient, remainingWorkItemsQuery);

            var doneWorkItemIds = doneWorkItems.Select(x => x.Id.ToString()).ToList();
            var remainingWorkItemsIds = remainingWorkItems.Select(x => x.Id.ToString()).ToList();

            logger.LogDebug("Found following Done Work Items {DoneWorkItems}", string.Join(", ", doneWorkItems));
            logger.LogDebug("Found following Undone Work Items {RemainingWorkItems}", string.Join(", ", remainingWorkItemsIds));

            return (remainingWorkItemsIds, remainingWorkItemsIds.Union(doneWorkItemIds).ToList());
        }

        [Obsolete]
        public async Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team)
        {
            logger.LogInformation("Checking if Item {ItemID} of Team {TeamName} is related to {FeatureIDs}", itemId, team.Name, string.Join(", ", featureIds));

            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var workItem = await GetWorkItemById(witClient, itemId, team);

            if (workItem == null)
            {
                return false;
            }

            var isRelated = featureIds.Any(f => IsWorkItemRelated(workItem, f, team.AdditionalRelatedField ?? string.Empty));

            logger.LogInformation("Is Item {ItemID} related: {IsRelated}", itemId, isRelated);

            return isRelated;
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
                var witClient = GetClientService(team.WorkTrackingSystemConnection);

                var throughput = await GetClosedItemsPerDay(witClient, team);
                var totalThroughput = throughput.Sum();

                logger.LogInformation("Found a total of {NumberOfWorkItems} Closed Work Items with specified Query in the last {Days} days", totalThroughput, team.ThroughputHistory);

                return totalThroughput > 0;
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
                var features = await GetFeaturesForProject(project);
                var totalFeatures = features.Count;

                logger.LogInformation("Found a total of {NumberOfFeature} Features with the specified Query", totalFeatures);

                return totalFeatures > 0;
            }
            catch (Exception exception)
            {
                logger.LogInformation(exception, "Error during Validation of Project Settings for Project {ProjectName}", project.Name);
                return false;
            }
        }

        // TODO: Move this to Feature Class?
        public async Task<int> GetEstimatedSizeForItem(string referenceId, Project project)
        {
            if (string.IsNullOrEmpty(project.SizeEstimateField))
            {
                return 0;
            }

            var estimationFieldValue = await GetFieldValue(referenceId, project, project.SizeEstimateField);

            // Try parsing double because for sure someone will have the brilliant idea to make this a decimal
            if (double.TryParse(estimationFieldValue, out var estimateAsDouble))
            {
                return (int)estimateAsDouble;
            }

            return 0;
        }

        // TODO: Move this to Feature Class?
        public async Task<string> GetFeatureOwnerByField(string referenceId, Project project)
        {
            if (string.IsNullOrEmpty(project.FeatureOwnerField))
            {
                return string.Empty;
            }

            var featureOwnerFieldValue = await GetFieldValue(referenceId, project, project.FeatureOwnerField);

            return featureOwnerFieldValue;
        }

        private async Task<string> GetFieldValue(string referenceId, Project project, string fieldName)
        {
            try
            {
                var witClient = GetClientService(project.WorkTrackingSystemConnection);

                var workItem = await GetWorkItemById(witClient, referenceId, project, fieldName);

                if (workItem == null)
                {
                    return string.Empty;
                }

                return workItem.Fields[fieldName].ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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

        private async Task<AdoWorkItem?> GetWorkItemById(WorkItemTrackingHttpClient witClient, string workItemId, IWorkItemQueryOwner workItemQueryOwner, params string[] additionalFields)
        {
            var query = PrepareQuery([], workItemQueryOwner.AllStates, workItemQueryOwner.WorkItemQuery, additionalFields);
            query += $" AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'";

            logger.LogDebug("Getting Work Item by Id. ID: {WorkItemId}. Query: '{Query}'", workItemId, query);

            var workItems = await GetWorkItemsByQuery(witClient, query);

            var workItemReference = workItems.SingleOrDefault();

            if (workItemReference == null)
            {
                logger.LogDebug("Found No Item");
                return null;
            }

            logger.LogDebug("Found Item {WorkItemReferenceID}", workItemReference.Id);

            return await GetWorkItemFromCache(workItemReference.Id.ToString(), witClient);
        }

        private async Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(WorkItemTrackingHttpClient witClient, Team team, string relatedWorkItemId)
        {
            var relatedItemQuery = PrepareRelatedItemQuery(relatedWorkItemId, team.AdditionalRelatedField);

            var remainingItemsQuery = PrepareQuery(team.WorkItemTypes, team.OpenStates, team.WorkItemQuery);
            remainingItemsQuery += relatedItemQuery;

            var allItemsQuery = PrepareQuery(team.WorkItemTypes, team.AllStates, team.WorkItemQuery);
            allItemsQuery += relatedItemQuery;

            var remainingWorkItems = await GetWorkItemsByQuery(witClient, remainingItemsQuery);
            var totalWorkItems = await GetWorkItemsByQuery(witClient, allItemsQuery);

            return (remainingWorkItems.Count(), totalWorkItems.Count());
        }

        private async Task<AdoWorkItem> GetWorkItemFromCache(string itemId, WorkItemTrackingHttpClient witClient)
        {
            logger.LogDebug("Trying to get Work Item {ItemId} from cache...", itemId);
            var workItem = workItemCache.Get(itemId);

            if (workItem == null)
            {
                logger.LogDebug("No Item in chace - getting from Azure DevOps...");
                workItem = await witClient.GetWorkItemAsync(int.Parse(itemId), expand: WorkItemExpand.Relations);
                workItemCache.Store(itemId, workItem, TimeSpan.FromMinutes(5));
            }

            return workItem;
        }

        private bool IsWorkItemRelated(AdoWorkItem workItem, string relatedWorkItemId, string additionalField)
        {
            logger.LogDebug("Checking if Work Item: {WorkItemID} is related to {RelatedWorkItemId}", workItem.Id, relatedWorkItemId);

            // Check if the work item is a child of the specified relatedWorkItemId
            if (workItem.Relations != null)
            {
                foreach (var relation in workItem.Relations)
                {
                    if (relation.Attributes.TryGetValue("name", out var attributeValue) && attributeValue.ToString() == "Parent")
                    {
                        var splittedUrl = relation.Url.Split("/");
                        var parentId = splittedUrl[splittedUrl.Length - 1];

                        return parentId == relatedWorkItemId;
                    }
                }
            }

            if (!string.IsNullOrEmpty(additionalField) && workItem.Fields.ContainsKey(additionalField) && workItem.Fields[additionalField].ToString() == $"{relatedWorkItemId}")
            {
                return true;
            }

            return false;
        }


        private async Task<IEnumerable<WorkItemReference>> GetWorkItemsByQuery(WorkItemTrackingHttpClient witClient, string query)
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

        private async Task<string[]> GetClosedWorkItems(WorkItemTrackingHttpClient witClient, Team team)
        {
            var throughputSettings = team.GetThroughputSettings();

            var query = PrepareQuery(team.WorkItemTypes, team.DoneStates, team.WorkItemQuery);
            query += $" AND ([{AzureDevOpsFieldNames.ClosedDate}] <= '{throughputSettings.EndDate:yyyy-MM-dd}T00:00:00.0000000Z' OR [{AzureDevOpsFieldNames.ResolvedDate}] <= '{throughputSettings.EndDate:yyyy-MM-dd}T00:00:00.0000000Z' OR [{AzureDevOpsFieldNames.ActivatedDate}] <= '{throughputSettings.EndDate:yyyy-MM-dd}T00:00:00.0000000Z')";
            query += $" AND ([{AzureDevOpsFieldNames.ClosedDate}] >= '{throughputSettings.StartDate:yyyy-MM-dd}T00:00:00.0000000Z' OR [{AzureDevOpsFieldNames.ResolvedDate}] >= '{throughputSettings.StartDate:yyyy-MM-dd}T00:00:00.0000000Z' OR [{AzureDevOpsFieldNames.ActivatedDate}] >= '{throughputSettings.StartDate:yyyy-MM-dd}T00:00:00.0000000Z')";

            logger.LogDebug("Getting closed items per day for for team {TeamName} using query '{Query}'", team.Name, query);

            var workItems = await GetWorkItemsByQuery(witClient, query);

            return workItems.Select(wi => wi.Id.ToString()).ToArray();
        }

        private async Task<int[]> GetClosedItemsPerDay(WorkItemTrackingHttpClient witClient, Team team)
        {
            var throughputSettings = team.GetThroughputSettings();
            var numberOfDays = throughputSettings.NumberOfDays;
            var closedItemsPerDay = new int[numberOfDays];

            var workItems = await GetClosedWorkItems(witClient, team);

            foreach (var workItemId in workItems)
            {
                var workItem = await GetWorkItemFromCache(workItemId, witClient);
                var closedDate = await GetStateTransitionDate(witClient, workItem.Id, team.DoneStates);

                if (closedDate.HasValue)
                {
                    int index = (closedDate.Value.Date - throughputSettings.StartDate).Days;

                    if (index >= 0 && index < numberOfDays)
                    {
                        closedItemsPerDay[index]++;
                    }
                }
            }

            return closedItemsPerDay;
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

            foreach (var revisionFields in revisions.Select(revision => revision.Fields))
            {
                if (revisionFields.TryGetValue(AzureDevOpsFieldNames.State, out var stateValue) &&
                    revisionFields.TryGetValue(AzureDevOpsFieldNames.ChangedDate, out var changedDateValue))
                {
                    var state = stateValue.ToString() ?? string.Empty;
                    var changedDate = (DateTime?)changedDateValue;

                    var isRelevantCategory = states.Contains(state) && (previousState == null || !states.Contains(previousState));
                    var isRelevantStateChange = changedDate.HasValue && (!latestStateChangeDate.HasValue || changedDate > latestStateChangeDate.Value);

                    if (isRelevantStateChange && isRelevantCategory)
                    {
                        latestStateChangeDate = changedDate;
                    }

                    previousState = state;
                }
            }

            return latestStateChangeDate;
        }

        private static string PrepareQuery(
            IEnumerable<string> includedWorkItemTypes,
            IEnumerable<string> includedStates,
            string query,
            params string[] additionalFields)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(includedWorkItemTypes);
            var stateQuery = PrepareStateQuery(includedStates);

            var additionalFieldsQuery = string.Empty;
            if (!string.IsNullOrEmpty(additionalFieldsQuery))
            {
                additionalFieldsQuery = ", " + string.Join(", ", additionalFields.Where(f => !string.IsNullOrEmpty(f)).Select(field => $"[{field}]"));
            }

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}]{additionalFieldsQuery} FROM WorkItems WHERE ({query}) " +
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
