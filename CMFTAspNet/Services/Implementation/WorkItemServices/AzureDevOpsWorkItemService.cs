using CMFTAspNet.Cache;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking.AzureDevOps;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace CMFTAspNet.Services.Implementation.WorkItemServices
{
    public class AzureDevOpsWorkItemService : IWorkItemService
    {
        private readonly Cache<string, WorkItem> cache = new Cache<string, WorkItem>();

        private readonly string[] closedStates = ["Done", "Closed", "Removed"];

        public async Task<int[]> GetClosedWorkItems(int history, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            return await GetClosedItemsPerDay(witClient, history, team);
        }

        public async Task<List<string>> GetOpenWorkItems(IEnumerable<string> workItemTypes, IWorkItemQueryOwner workItemQueryOwner)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(workItemQueryOwner);

            var query = PrepareQuery(workItemTypes, closedStates, workItemQueryOwner);
            var workItems = await GetWorkItemsByQuery(witClient, query);

            var workItemReferences = new List<string>();

            foreach (var workItem in workItems)
            {
                workItemReferences.Add(workItem.Id.ToString());
            }

            return workItemReferences;
        }

        public async Task<int> GetRemainingRelatedWorkItems(string featureId, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            return await GetRelatedWorkItems(witClient, team, featureId);
        }

        public async Task<(string name, int order)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(workItemQueryOwner);

            var workItem = await GetWorkItemById(witClient, itemId, workItemQueryOwner);

            var workItemTitle = workItem?.Fields[AzureDevOpsFieldNames.Title].ToString() ?? string.Empty;
            var workItemStackRank = int.Parse(workItem?.Fields[AzureDevOpsFieldNames.StackRank].ToString() ?? "0");

            return (workItemTitle, workItemStackRank);
        }

        public async Task<List<string>> GetOpenWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            var workItemsQuery = PrepareWorkItemTypeQuery(workItemTypes);
            var stateQuery = PrepareStateQuery(closedStates);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}] FROM WorkItems WHERE {unparentedItemsQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery}";

            var workItems = await GetWorkItemsByQuery(witClient, wiql);

            return workItems.Select(x => x.Id.ToString()).ToList();
        }

        public async Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            var workItem = await GetWorkItemById(witClient, itemId, team);

            foreach (var featureId in featureIds)
            {
                if (IsWorkItemRelated(workItem, featureId, team.AdditionalRelatedField))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<WorkItem> GetWorkItemById(WorkItemTrackingHttpClient witClient, string workItemId, IWorkItemQueryOwner workItemQueryOwner)
        {
            var query = PrepareQuery([], [], workItemQueryOwner);
            query += $" AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'";

            var workItems = await GetWorkItemsByQuery(witClient, query);

            var workItemReference = workItems.Single();
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

                if (IsWorkItemRelated(workItem, relatedWorkItemId, team.AdditionalRelatedField))
                {
                    remainingItems += 1;
                }
            }

            return remainingItems;
        }

        private async Task<WorkItem> GetWorkItemFromCache(string itemId, WorkItemTrackingHttpClient witClient)
        {
            var workItem = cache.Get(itemId);

            if (workItem == null)
            {
                workItem = await witClient.GetWorkItemAsync(int.Parse(itemId), expand: WorkItemExpand.Relations);
                cache.Store(itemId, workItem, TimeSpan.FromMinutes(5));
            }

            return workItem;
        }

        private bool IsWorkItemRelated(WorkItem workItem, string relatedWorkItemId, string additionalField)
        {
            // Check if the work item is a child of the specified relatedWorkItemId
            if (workItem.Relations != null)
            {
                foreach (var relation in workItem.Relations)
                {
                    if (relation.Attributes.TryGetValue("name", out var attributeValue) && attributeValue.ToString() == "Parent" && relation.Url.Contains($"/{relatedWorkItemId}"))
                    {
                        return true;
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

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}], [{AzureDevOpsFieldNames.Title}], [{AzureDevOpsFieldNames.StackRank}] FROM WorkItems WHERE {workitemQueryOwner.WorkItemQuery} " +
                $"{workItemsQuery} " +
                $"{stateQuery}";

            return wiql;
        }

        private string PrepareWorkItemTypeQuery(IEnumerable<string> workItemTypes)
        {
            return PrepareQuery(workItemTypes, AzureDevOpsFieldNames.WorkItemType, "OR", "=");
        }

        private string PrepareStateQuery(IEnumerable<string> excludedStates)
        {
            return PrepareQuery(excludedStates, AzureDevOpsFieldNames.State, "AND", "<>");
        }

        private string PrepareQuery(IEnumerable<string> options, string fieldName, string queryOperator, string queryComparison)
        {
            var query = string.Join($" {queryOperator} ", options.Select(options => $"[{fieldName}] {queryComparison} '{options}'"));

            if (options.Count() == 0)
            {
                query = string.Empty;
            }
            else
            {
                query = $"AND ({query}) ";
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
