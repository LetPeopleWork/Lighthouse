using Lighthouse.Cache;
using Lighthouse.Models;
using Lighthouse.Services.Interfaces;
using Lighthouse.WorkTracking.AzureDevOps;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Lighthouse.Services.Implementation.WorkItemServices
{
    public class AzureDevOpsWorkItemService : IWorkItemService
    {
        private readonly Cache<string, WorkItem> cache = new Cache<string, WorkItem>();

        private readonly string[] closedStates = ["Done", "Closed", "Removed"];
        private readonly ILogger<AzureDevOpsWorkItemService> logger;

        public AzureDevOpsWorkItemService(ILogger<AzureDevOpsWorkItemService> logger)
        {
            this.logger = logger;
        }

        public async Task<int[]> GetClosedWorkItems(int history, Team team)
        {
            logger.LogInformation($"Getting Closed Work Items for Team {team.Name}");
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            return await GetClosedItemsPerDay(witClient, history, team);
        }

        public async Task<List<string>> GetOpenWorkItems(IEnumerable<string> workItemTypes, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation($"Getting Open Work Items for Work Items {string.Join(", ", workItemTypes)} and Query '{workItemQueryOwner.WorkItemQuery}'");
            var witClient = GetClientService<WorkItemTrackingHttpClient>(workItemQueryOwner);

            var query = PrepareQuery(workItemTypes, closedStates, workItemQueryOwner);
            var workItems = await GetWorkItemsByQuery(witClient, query);

            var workItemReferences = new List<string>();

            foreach (var workItem in workItems)
            {
                logger.LogInformation($"Found Work Item with ID {workItem.Id}");
                workItemReferences.Add(workItem.Id.ToString());
            }

            return workItemReferences;
        }

        public async Task<int> GetRemainingRelatedWorkItems(string featureId, Team team)
        {
            logger.LogInformation($"Getting Related Work Items for Feature {featureId} and Team {team.Name}");
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            var relatedWorkItems = await GetRelatedWorkItems(witClient, team, featureId);

            return relatedWorkItems;
        }

        public async Task<(string name, string order)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            logger.LogInformation($"Getting Work Item Details for {itemId} and Query {workItemQueryOwner.WorkItemQuery}");

            var witClient = GetClientService<WorkItemTrackingHttpClient>(workItemQueryOwner);

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
            logger.LogInformation($"Getting Open Work Items for Team {team.Name}, Item Types {string.Join(", ", workItemTypes)} and Unaprented Items Query '{unparentedItemsQuery}'");

            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            var workItemsQuery = PrepareWorkItemTypeQuery(workItemTypes);
            var stateQuery = PrepareStateQuery(closedStates);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}], [{AzureDevOpsFieldNames.BacklogPriority}] FROM WorkItems WHERE {unparentedItemsQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery}" +
                $" AND {team.WorkItemQuery}";

            var workItems = await GetWorkItemsByQuery(witClient, wiql);

            var openWorkItems = workItems.Select(x => x.Id.ToString()).ToList();

            logger.LogInformation($"Found following Open Work Items {string.Join(", ", openWorkItems)}");

            return openWorkItems;
        }

        public async Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team)
        {
            logger.LogInformation($"Checking if Item {itemId} of Team {team.Name} is related to {string.Join(", ", featureIds)}");

            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            var workItem = await GetWorkItemById(witClient, itemId, team);

            if (workItem == null)
            {
                return false;
            }

            var isRelated = featureIds.Any(f => IsWorkItemRelated(workItem, f, team.AdditionalRelatedField ?? string.Empty));

            logger.LogInformation($"Is Item {itemId} related: {isRelated}");

            return isRelated;
        }

        public async Task<bool> ItemHasChildren(string referenceId, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner)
        {
            logger.LogInformation($"Checking if Item {referenceId} has Children");

            var witClient = GetClientService<WorkItemTrackingHttpClient>(workTrackingSystemOptionsOwner);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItemLinks WHERE [Source].[{AzureDevOpsFieldNames.Id}] = '{referenceId}' AND [System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward' AND Target.[System.WorkItemType] <> '' MODE (Recursive)";
            var workItems = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql });

            var hasChildren = workItems.WorkItemRelations.Count() > 1;

            logger.LogInformation($"Item {referenceId} has Children: {hasChildren}");

            return hasChildren;
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            logger.LogInformation($"Getting Adjacent Order Index for items {string.Join(", ", existingItemsOrder)} in order {relativeOrder}");

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

            logger.LogInformation($"Adjacent Order Index for items {string.Join(", ", existingItemsOrder)} in order {relativeOrder}: {result}");

            return result;
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

            logger.LogDebug($"Getting Work Item by Id. ID: {workItemId}. Query: '{query}'");

            var workItems = await GetWorkItemsByQuery(witClient, query);

            var workItemReference = workItems.SingleOrDefault();

            if (workItemReference == null)
            {
                logger.LogDebug($"Found No Item");
                return null;
            }

            logger.LogDebug($"Found Item {workItemReference.Id}");

            return await GetWorkItemFromCache(workItemReference.Id.ToString(), witClient);
        }

        private async Task<int> GetRelatedWorkItems(WorkItemTrackingHttpClient witClient, Team team, string relatedWorkItemId)
        {
            var remainingItems = 0;

            var query = PrepareQuery(team.WorkItemTypes, closedStates, team);
            var workItems = await GetWorkItemsByQuery(witClient, query);

            foreach (WorkItemReference workItemRef in workItems)
            {
                var workItem = await GetWorkItemFromCache(workItemRef.Id.ToString(), witClient);

                if (IsWorkItemRelated(workItem, relatedWorkItemId, team.AdditionalRelatedField ?? string.Empty))
                {
                    logger.LogInformation($"Found Related Work Item: {workItem.Id}");
                    remainingItems += 1;
                }
            }

            return remainingItems;
        }

        private async Task<WorkItem> GetWorkItemFromCache(string itemId, WorkItemTrackingHttpClient witClient)
        {
            logger.LogDebug($"Trying to get Work Item {itemId} from cache...");
            var workItem = cache.Get(itemId);

            if (workItem == null)
            {
                logger.LogDebug($"No Item in chace - getting from Azure DevOps...");
                workItem = await witClient.GetWorkItemAsync(int.Parse(itemId), expand: WorkItemExpand.Relations);
                cache.Store(itemId, workItem, TimeSpan.FromMinutes(5));
            }

            return workItem;
        }

        private bool IsWorkItemRelated(WorkItem workItem, string relatedWorkItemId, string additionalField)
        {
            logger.LogDebug($"Checking if Work Item: {workItem.Id} is related to {relatedWorkItemId}");

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

            logger.LogDebug($"Getting closed items per day for thre last {numberOfDays} for team {team.Name} using query '{query}'");

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

        private T GetClientService<T>(IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner) where T : VssHttpClientBase
        {
            var url = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(AzureDevOpsWorkTrackingOptionNames.Url);
            var personalAccessToken = workTrackingSystemOptionsOwner.GetWorkTrackingSystemOptionByKey(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);
            var connection = CreateConnection(url, personalAccessToken);
            return connection.GetClient<T>();
        }

        private VssConnection CreateConnection(string azureDevOpsUrl, string personalAccessToken)
        {
            var azureDevOpsUri = new Uri(azureDevOpsUrl);
            var credentials = new VssBasicCredential(personalAccessToken, string.Empty);

            return new VssConnection(azureDevOpsUri, credentials);
        }
    }
}
