using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking.AzureDevOps;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Lighthouse.Backend.Services.Implementation.WorkItemServices
{
    public class AzureDevOpsWorkItemService : IWorkItemService
    {
        private readonly Cache<string, WorkItem> workItemCache = new Cache<string, WorkItem>();

        private readonly ILogger<AzureDevOpsWorkItemService> logger;
        private readonly ICryptoService cryptoService;

        public AzureDevOpsWorkItemService(ILogger<AzureDevOpsWorkItemService> logger, ICryptoService cryptoService)
        {
            this.logger = logger;
            this.cryptoService = cryptoService;
        }

        public async Task<int[]> GetClosedWorkItems(int history, Team team)
        {
            logger.LogInformation("Getting Closed Work Items for Team {TeamName}", team.Name);
            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            return await GetClosedItemsPerDay(witClient, history, team);
        }

        public async Task<List<string>> GetOpenWorkItems(IEnumerable<string> workItemTypes, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Open Work Items for Work Items {WorkItemTypes} and Query '{Query}'", string.Join(", ", workItemTypes), workItemQueryOwner.WorkItemQuery);
            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);

            var query = PrepareQuery(workItemTypes, workItemQueryOwner.OpenStates, workItemQueryOwner.WorkItemQuery);
            var workItems = await GetWorkItemsByQuery(witClient, query);

            var workItemReferences = workItems.Select(wi => wi.Id.ToString()).ToList();
            logger.LogInformation("Found Work Items with IDs {WorkItemIDs}", string.Join(", ", workItemReferences));

            return workItemReferences;
        }

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
                    var childItemForTeam = await GetRelatedWorkItems($"{feature.Id}", team);
                    childItems += childItemForTeam.totalItems;
                }

                childItemList.Add(childItems);
            }

            return childItemList.Where(i => i > 0);
        }

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

        public async Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(string featureId, Team team)
        {
            logger.LogInformation("Getting Related Work Items for Feature {featureId} and Team {TeamName}", featureId, team.Name);
            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var relatedWorkItems = await GetRelatedWorkItems(witClient, team, featureId);

            return relatedWorkItems;
        }

        public async Task<(string name, string order, string url, string state)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Work Item Details for {itemId} and Query {Query}", itemId, workItemQueryOwner.WorkItemQuery);

            var witClient = GetClientService(workItemQueryOwner.WorkTrackingSystemConnection);

            var workItem = await GetWorkItemById(witClient, itemId, workItemQueryOwner);
            var state = workItem?.Fields[AzureDevOpsFieldNames.State].ToString() ?? string.Empty;
            var workItemTitle = workItem?.Fields[AzureDevOpsFieldNames.Title].ToString() ?? string.Empty;

            var url = ((ReferenceLink)workItem?.Links.Links["html"])?.Href ?? string.Empty;

            var workItemOrder = string.Empty;

            if (workItem?.Fields.TryGetValue(AzureDevOpsFieldNames.StackRank, out var stackRank) ?? false)
            {
                workItemOrder = stackRank?.ToString() ?? string.Empty;
            }
            else if (workItem?.Fields.TryGetValue(AzureDevOpsFieldNames.BacklogPriority, out var backlogPriority) ?? false)
            {
                workItemOrder = backlogPriority?.ToString() ?? string.Empty;
            }

            return (workItemTitle, workItemOrder, url, state);
        }

        public async Task<(List<string> remainingWorkItems, List<string> allWorkItems)> GetWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery)
        {
            logger.LogInformation("Getting Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Unaprented Items Query '{Query}'", team.Name, string.Join(", ", workItemTypes), unparentedItemsQuery);

            var witClient = GetClientService(team.WorkTrackingSystemConnection);

            var workItemsQuery = PrepareWorkItemTypeQuery(workItemTypes);
            var doneStateQuery = PrepareStateQuery(team.DoneStates);
            var pendingStateQuery = PrepareStateQuery(team.ToDoStates.Union(team.DoingStates));

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

            logger.LogInformation("Is Item {ItemID} related: {isRelated}", itemId, isRelated);

            return isRelated;
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            logger.LogInformation("Getting Adjacent Order Index for items {ExistingItemsOrder} in order {relativeOrder}", string.Join(", ", existingItemsOrder), relativeOrder);

            var result = string.Empty;

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

            logger.LogInformation("Adjacent Order Index for items {existingItemsOrder} in order {relativeOrder}: {result}", string.Join(", ", existingItemsOrder), relativeOrder, result);

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

                var throughput = await GetClosedItemsPerDay(witClient, team.ThroughputHistory, team);
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
                var witClient = GetClientService(project.WorkTrackingSystemConnection);
                var features = await GetOpenWorkItems(project.WorkItemTypes, project);
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

        public async Task<int> GetEstimatedSizeForItem(string referenceId, Project project)
        {
            if (string.IsNullOrEmpty(project.SizeEstimateField))
            {
                return 0;
            }

            try
            {
                var witClient = GetClientService(project.WorkTrackingSystemConnection);

                var workItem = await GetWorkItemById(witClient, referenceId, project, project.SizeEstimateField);

                if (workItem == null)
                {
                    return 0;
                }

                var estimateRawValue = workItem.Fields[project.SizeEstimateField].ToString() ?? "0";

                // Try parsing double because for sure someone will have the brilliant idea to make this a decimal
                if (double.TryParse(estimateRawValue, out var estimateAsDouble))
                {
                    return (int)estimateAsDouble;
                }

                return 0;
            }
            catch
            {
                return 0;
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

        private async Task<WorkItem?> GetWorkItemById(WorkItemTrackingHttpClient witClient, string workItemId, IWorkItemQueryOwner workItemQueryOwner, params string[] additionalFields)
        {
            var query = PrepareQuery([], workItemQueryOwner.AllStates, workItemQueryOwner.WorkItemQuery, additionalFields);
            query += $" AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'";

            logger.LogDebug("Getting Work Item by Id. ID: {workItemId}. Query: '{query}'", workItemId, query);

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

        private async Task<WorkItem> GetWorkItemFromCache(string itemId, WorkItemTrackingHttpClient witClient)
        {
            logger.LogDebug("Trying to get Work Item {itemId} from cache...", itemId);
            var workItem = workItemCache.Get(itemId);

            if (workItem == null)
            {
                logger.LogDebug("No Item in chace - getting from Azure DevOps...");
                workItem = await witClient.GetWorkItemAsync(int.Parse(itemId), expand: WorkItemExpand.Relations);
                workItemCache.Store(itemId, workItem, TimeSpan.FromMinutes(5));
            }

            return workItem;
        }

        private bool IsWorkItemRelated(WorkItem workItem, string relatedWorkItemId, string additionalField)
        {
            logger.LogDebug("Checking if Work Item: {WorkItemID} is related to {relatedWorkItemId}", workItem.Id, relatedWorkItemId);

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
            catch (VssServiceException)
            {
                return Enumerable.Empty<WorkItemReference>();
            }
        }

        private async Task<int[]> GetClosedItemsPerDay(WorkItemTrackingHttpClient witClient, int numberOfDays, Team team)
        {
            var closedItemsPerDay = new int[numberOfDays];
            var startDate = DateTime.UtcNow.Date.AddDays(-(numberOfDays - 1));

            var query = PrepareQuery(team.WorkItemTypes, team.DoneStates, team.WorkItemQuery);
            query += $" AND ([{AzureDevOpsFieldNames.ClosedDate}] >= '{startDate:yyyy-MM-dd}T00:00:00.0000000Z' OR [{AzureDevOpsFieldNames.ResolvedDate}] >= '{startDate:yyyy-MM-dd}T00:00:00.0000000Z' OR [{AzureDevOpsFieldNames.ActivatedDate}] >= '{startDate:yyyy-MM-dd}T00:00:00.0000000Z')";

            logger.LogDebug("Getting closed items per day for thre last {numberOfDays} for team {TeamName} using query '{query}'", numberOfDays, team.Name, query);

            var workItems = await GetWorkItemsByQuery(witClient, query);

            foreach (WorkItemReference workItemRef in workItems)
            {
                var workItem = await GetWorkItemFromCache(workItemRef.Id.ToString(), witClient);
                string? closedDate = GetClosedDateFromWorkItem(workItem);

                if (!string.IsNullOrEmpty(closedDate))
                {
                    var changedDate = DateTime.Parse(closedDate);

                    int index = (changedDate.Date - startDate).Days;

                    if (index >= 0 && index < numberOfDays)
                    {
                        closedItemsPerDay[index]++;
                    }
                }
            }

            return closedItemsPerDay;
        }

        private static string? GetClosedDateFromWorkItem(WorkItem workItem)
        {
            if (workItem.Fields.TryGetValue(AzureDevOpsFieldNames.ClosedDate, out var closedDate))
            {
                return closedDate.ToString();
            }

            if (workItem.Fields.TryGetValue(AzureDevOpsFieldNames.ResolvedDate, out var resolvedDate))
            {
                return resolvedDate.ToString();
            }

            if (workItem.Fields.TryGetValue(AzureDevOpsFieldNames.ActivatedDate, out var activatedDate))
            {
                return activatedDate.ToString();
            }

            return null;
        }

        private string PrepareQuery(
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

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}]{additionalFieldsQuery} FROM WorkItems WHERE {query} " +
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
