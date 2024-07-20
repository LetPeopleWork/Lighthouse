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

        private readonly string[] closedStates = ["Done", "Closed", "Removed"];
        private readonly ILogger<AzureDevOpsWorkItemService> logger;

        public AzureDevOpsWorkItemService(ILogger<AzureDevOpsWorkItemService> logger)
        {
            this.logger = logger;
        }

        public async Task<int[]> GetClosedWorkItems(int history, Team team)
        {
            logger.LogInformation("Getting Closed Work Items for Team {TeamName}", team.Name);
            var witClient = GetClientService(team);

            return await GetClosedItemsPerDay(witClient, history, team);
        }

        public async Task<List<string>> GetOpenWorkItems(IEnumerable<string> workItemTypes, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Open Work Items for Work Items {WorkItemTypes} and Query '{Query}'", string.Join(", ", workItemTypes), workItemQueryOwner.WorkItemQuery);
            var witClient = GetClientService(workItemQueryOwner);

            var query = PrepareQuery(workItemTypes, closedStates, workItemQueryOwner);
            var workItems = await GetWorkItemsByQuery(witClient, query);

            var workItemReferences = workItems.Select(wi => wi.Id.ToString()).ToList();
            logger.LogInformation("Found Work Items with IDs {WorkItemIDs}", string.Join(", ", workItemReferences));

            return workItemReferences;
        }

        public async Task<int> GetRemainingRelatedWorkItems(string featureId, Team team)
        {
            logger.LogInformation("Getting Related Work Items for Feature {featureId} and Team {TeamName}", featureId, team.Name);
            var witClient = GetClientService(team);

            var relatedWorkItems = await GetRelatedWorkItems(witClient, team, featureId);

            return relatedWorkItems;
        }

        public async Task<(string name, string order)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation("Getting Work Item Details for {itemId} and Query {Query}", itemId, workItemQueryOwner.WorkItemQuery);

            var witClient = GetClientService(workItemQueryOwner);

            var workItem = await GetWorkItemById(witClient, itemId, workItemQueryOwner);

            var workItemTitle = workItem?.Fields[AzureDevOpsFieldNames.Title].ToString() ?? string.Empty;
            var workItemOrder = string.Empty;

            if (workItem?.Fields.TryGetValue(AzureDevOpsFieldNames.StackRank, out var stackRank) ?? false)
            {
                workItemOrder = stackRank?.ToString() ?? string.Empty;
            }
            else if (workItem?.Fields.TryGetValue(AzureDevOpsFieldNames.BacklogPriority, out var backlogPriority) ?? false)
            {
                workItemOrder = backlogPriority?.ToString() ?? string.Empty;
            }

            return (workItemTitle, workItemOrder);
        }

        public async Task<List<string>> GetOpenWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery)
        {
            logger.LogInformation("Getting Open Work Items for Team {TeamName}, Item Types {WorkItemTypes} and Unaprented Items Query '{Query}'", team.Name, string.Join(", ", workItemTypes), unparentedItemsQuery);

            var witClient = GetClientService(team);

            var workItemsQuery = PrepareWorkItemTypeQuery(workItemTypes);
            var stateQuery = PrepareStateQuery(closedStates);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}] FROM WorkItems WHERE {unparentedItemsQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery}" +
                $" AND {team.WorkItemQuery}";

            var workItems = await GetWorkItemsByQuery(witClient, wiql);

            var openWorkItems = workItems.Select(x => x.Id.ToString()).ToList();

            logger.LogInformation("Found following Open Work Items {OpenItems}", string.Join(", ", openWorkItems));

            return openWorkItems;
        }

        public async Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team)
        {
            logger.LogInformation("Checking if Item {ItemID} of Team {TeamName} is related to {FeatureIDs}", itemId, team.Name, string.Join(", ", featureIds));

            var witClient = GetClientService(team);

            var workItem = await GetWorkItemById(witClient, itemId, team);

            if (workItem == null)
            {
                return false;
            }

            var isRelated = featureIds.Any(f => IsWorkItemRelated(workItem, f, team.AdditionalRelatedField ?? string.Empty));

            logger.LogInformation("Is Item {ItemID} related: {isRelated}", itemId, isRelated);

            return isRelated;
        }

        public async Task<bool> ItemHasChildren(string referenceId, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            logger.LogInformation("Checking if Item {referenceId} has Children", referenceId);

            var witClient = GetClientService(workTrackingSystemOptionsOwner);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItemLinks WHERE [Source].[{AzureDevOpsFieldNames.Id}] = '{referenceId}' AND [System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward' AND Target.[System.WorkItemType] <> '' MODE (Recursive)";
            var workItems = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql });

            var hasChildren = workItems.WorkItemRelations.Count() > 1;

            logger.LogInformation("Item {referenceId} has Children: {hasChildren}", referenceId, hasChildren);

            return hasChildren;
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
            var witClient = GetClientService(connection);
            var query = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.Id}] = 12";

            try
            {
                await witClient.QueryByWiqlAsync(new Wiql() { Query = query });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<int> ConvertToIntegers(IEnumerable<string> orderAsStrings)
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

        private async Task<WorkItem?> GetWorkItemById(WorkItemTrackingHttpClient witClient, string workItemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            var query = PrepareQuery([], [], workItemQueryOwner);
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

        private async Task<int> GetRelatedWorkItems(WorkItemTrackingHttpClient witClient, Team team, string relatedWorkItemId)
        {
            var query = PrepareQuery(team.WorkItemTypes, closedStates, team);
            query += PrepareRelatedItemQuery(relatedWorkItemId, team.AdditionalRelatedField);

            var workItems = await GetWorkItemsByQuery(witClient, query);

            return workItems.Count();
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

            var query = PrepareQuery(team.WorkItemTypes, [], team);
            query += $" AND ([{AzureDevOpsFieldNames.State}] = 'Closed' OR [{AzureDevOpsFieldNames.State}] = 'Done') AND [{AzureDevOpsFieldNames.ClosedDate}] >= '{startDate:yyyy-MM-dd}T00:00:00.0000000Z'";

            logger.LogDebug("Getting closed items per day for thre last {numberOfDays} for team {TeamName} using query '{query}'", numberOfDays, team.Name, query);

            var workItems = await GetWorkItemsByQuery(witClient, query);

            foreach (WorkItemReference workItemRef in workItems)
            {
                var workItem = await GetWorkItemFromCache(workItemRef.Id.ToString(), witClient);
                var closedDate = workItem.Fields[AzureDevOpsFieldNames.ClosedDate].ToString();

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

        private string PrepareQuery(
            IEnumerable<string> workItemTypes,
            IEnumerable<string> excludedStates,
            IWorkItemQueryOwner workitemQueryOwner)
        {
            var workItemsQuery = PrepareWorkItemTypeQuery(workItemTypes);
            var stateQuery = PrepareStateQuery(excludedStates);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}] FROM WorkItems WHERE {workitemQueryOwner.WorkItemQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery}";

            return wiql;
        }

        private string PrepareWorkItemTypeQuery(IEnumerable<string> workItemTypes)
        {
            return PrepareGenericQuery(workItemTypes, AzureDevOpsFieldNames.WorkItemType, "OR", "=");
        }

        private string PrepareStateQuery(IEnumerable<string> excludedStates)
        {
            return PrepareGenericQuery(excludedStates, AzureDevOpsFieldNames.State, "AND", "<>");
        }

        private string PrepareGenericQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
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

        private string PrepareRelatedItemQuery(string relatedItemId, string? additionalRelatedField)
        {
            var additionalRelatedFieldQuery = string.Empty;
            if (!string.IsNullOrEmpty(additionalRelatedField))
            {
                additionalRelatedFieldQuery = $"OR [{additionalRelatedField}] = '{relatedItemId}'";
            }

            return $"AND ([System.Parent] = '{relatedItemId}' {additionalRelatedFieldQuery})";
        }

        private WorkItemTrackingHttpClient GetClientService(IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            var url = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            var personalAccessToken = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);

            var connection = CreateConnection(url, personalAccessToken);
            return connection.GetClient<WorkItemTrackingHttpClient>();
        }

        private WorkItemTrackingHttpClient GetClientService(WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            var url = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            var personalAccessToken = workTrackingSystemConnection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);

            var connection = CreateConnection(url, personalAccessToken);
            return connection.GetClient<WorkItemTrackingHttpClient>();
        }

        private VssConnection CreateConnection(string azureDevOpsUrl, string personalAccessToken)
        {
            var azureDevOpsUri = new Uri(azureDevOpsUrl);
            var credentials = new VssBasicCredential(personalAccessToken, string.Empty);

            return new VssConnection(azureDevOpsUri, credentials);
        }
    }
}
