using CMFTAspNet.Cache;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking.AzureDevOps;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text;

namespace CMFTAspNet.Services.Implementation.AzureDevOps
{
    public class AzureDevOpsWorkItemService : IWorkItemService
    {
        private readonly Cache<int, WorkItem> cache = new Cache<int, WorkItem>();

        public async Task<int[]> GetClosedWorkItemsForTeam(int history, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            return await GetClosedItemsPerDay(witClient, history, team);
        }

        public async Task<List<int>> GetWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            return await GetWorkItemsByTag(witClient, tag, workItemTypes, team, []);
        }

        public async Task<List<int>> GetWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            return await GetWorkItemsByAreaPath(witClient, areaPath, workItemTypes, team, []);
        }

        public async Task<List<int>> GetNotClosedWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            return await GetWorkItemsByTag(witClient, tag, workItemTypes, team, ["Closed", "Removed"]);
        }

        public async Task<List<int>> GetNotClosedWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            return await GetWorkItemsByAreaPath(witClient, areaPath, workItemTypes, team, ["Closed", "Removed"]);
        }

        public async Task<int> GetRemainingRelatedWorkItems(int featureId, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            return await GetRelatedWorkItems(witClient, team, featureId);
        }

        public async Task<bool> IsRelatedToFeature(int itemId, IEnumerable<int> featureIds, Team team)
        {
            var workItem = cache.Get(itemId);

            if (workItem == null)
            {                
                var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

                workItem = await GetWorkItemById(witClient, itemId, team, []);
                cache.Store(itemId, workItem, TimeSpan.FromMinutes(5));
            }

            return featureIds.Any(f => IsWorkItemRelated(workItem, f, team.AdditionalRelatedFields));
        }

        public async Task<(string name, int order)> GetWorkItemDetails(int itemId, Team team)
        {
            var witClient = GetClientService<WorkItemTrackingHttpClient>(team);

            var workItem = await GetWorkItemById(witClient, itemId, team, [AzureDevOpsFieldNames.Title, AzureDevOpsFieldNames.StackRank]);

            var workItemTitle = workItem?.Fields[AzureDevOpsFieldNames.Title].ToString() ?? string.Empty;
            var workItemStackRank = int.Parse(workItem?.Fields[AzureDevOpsFieldNames.StackRank].ToString() ?? "0");

            return (workItemTitle, workItemStackRank);
        }

        private async Task<WorkItem> GetWorkItemById(WorkItemTrackingHttpClient witClient, int workItemId, Team team, List<string> additionalFields)
        {
            var additionalFieldsQuery = PrepareAdditionalFieldsQuery(additionalFields);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}] {additionalFieldsQuery} FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{team.ProjectName}' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'";
            var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql }, team.ProjectName);

            var workItemReference = queryResult.WorkItems.Single();
            return await GetWorkItemFromCache(workItemReference.Id, witClient);
        }

        private async Task<int> GetRelatedWorkItems(WorkItemTrackingHttpClient witClient, Team team, int relatedWorkItemId)
        {
            var remainingItems = 0;

            var areaPathQuery = string.Join(" OR ", team.AreaPaths.Select(path => $"[{AzureDevOpsFieldNames.AreaPath}] UNDER '{path}'"));
            var workItemsQuery = string.Join(" OR ", team.WorkItemTypes.Select(type => $"[{AzureDevOpsFieldNames.WorkItemType}] = '{type}'"));
            var stateQuery = PrepareStateQuery(["Closed", "Removed"]);
            var ignoredTagsQuery = PrepareIgnoredTagsQuery(team.IgnoredTags);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{team.ProjectName}' " +
                $"{stateQuery}" +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) " +
                $"{ignoredTagsQuery}";

            var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql }, team.ProjectName);

            foreach (WorkItemReference workItemRef in queryResult.WorkItems)
            {
                var workItem = await GetWorkItemFromCache(workItemRef.Id, witClient);

                if (IsWorkItemRelated(workItem, relatedWorkItemId, team.AdditionalRelatedFields))
                {
                    remainingItems += 1;
                }
            }

            return remainingItems;
        }

        private async Task<WorkItem> GetWorkItemFromCache(int itemId, WorkItemTrackingHttpClient witClient)
        {
            var workItem = cache.Get(itemId);

            if (workItem == null)
            {
                workItem = await witClient.GetWorkItemAsync(itemId, expand: WorkItemExpand.Relations);
                cache.Store(itemId, workItem, TimeSpan.FromMinutes(5));
            }

            return workItem;
        }

        private bool IsWorkItemRelated(WorkItem workItem, int relatedWorkItemId, List<string> additionalRelatedFields)
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

            foreach (var additionalField in additionalRelatedFields)
            {
                if (workItem.Fields.ContainsKey(additionalField) && workItem.Fields[additionalField].ToString() == $"{relatedWorkItemId}")
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<List<int>> GetWorkItemsByAreaPath(WorkItemTrackingHttpClient witClient, string areaPath, IEnumerable<string> workItemTypes, Team team, string[] excludedStates)
        {
            var foundItemIds = new List<int>();

            var areaPathQuery = $"[{AzureDevOpsFieldNames.AreaPath}] UNDER '{areaPath}'";
            var workItemsQuery = string.Join(" OR ", workItemTypes.Select(itemType => $"[{AzureDevOpsFieldNames.WorkItemType}] = '{itemType}'"));
            var stateQuery = PrepareStateQuery(excludedStates);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{team.ProjectName}' " +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) " +
                $"{stateQuery}";

            try
            {
                var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql });
                foreach (WorkItemReference workItemRef in queryResult.WorkItems)
                {
                    foundItemIds.Add(workItemRef.Id);
                }
            }
            catch (VssServiceException)
            {
                // Issue with query - ignore
            }

            return foundItemIds;
        }

        private async Task<List<int>> GetWorkItemsByTag(WorkItemTrackingHttpClient witClient, string tag, IEnumerable<string> workItemTypes, Team team, string[] excludedStates)
        {
            var foundItemIds = new List<int>();

            var areaPathQuery = string.Join(" OR ", team.AreaPaths.Select(path => $"[{AzureDevOpsFieldNames.AreaPath}] UNDER '{path}'"));
            var workItemsQuery = string.Join(" OR ", workItemTypes.Select(itemType => $"[{AzureDevOpsFieldNames.WorkItemType}] = '{itemType}'"));
            var tagQuery = $"[{AzureDevOpsFieldNames.Tags}] CONTAINS '{tag}'";
            string stateQuery = PrepareStateQuery(excludedStates);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{team.ProjectName}' " +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) " +
                $"{stateQuery}" +
                $"AND ({tagQuery}) ";

            var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql });
            foreach (WorkItemReference workItemRef in queryResult.WorkItems)
            {
                foundItemIds.Add(workItemRef.Id);
            }

            return foundItemIds;
        }

        private async Task<int[]> GetClosedItemsPerDay(WorkItemTrackingHttpClient witClient, int numberOfDays, Team team)
        {
            var closedItemsPerDay = new int[numberOfDays];

            var startDate = DateTime.UtcNow.Date.AddDays(-(numberOfDays - 1));

            var areaPathQuery = string.Join(" OR ", team.AreaPaths.Select(path => $"[{AzureDevOpsFieldNames.AreaPath}] UNDER '{path}'"));
            var workItemsQuery = string.Join(" OR ", team.WorkItemTypes.Select(type => $"[{AzureDevOpsFieldNames.WorkItemType}] = '{type}'"));
            var ignoredTagsQuery = PrepareIgnoredTagsQuery(team.IgnoredTags);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{team.ProjectName}' AND [{AzureDevOpsFieldNames.State}] = 'Closed' " +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) " +
                $"{ignoredTagsQuery}" +
                $"AND [{AzureDevOpsFieldNames.ClosedDate}] >= '{startDate:yyyy-MM-dd}T00:00:00.0000000Z'";

            var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql });

            foreach (WorkItemReference workItemRef in queryResult.WorkItems)
            {
                var workItem = await GetWorkItemFromCache(workItemRef.Id, witClient);
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

        private string PrepareStateQuery(string[] excludedStates)
        {
            var stateQuery = string.Join(" AND ", excludedStates.Select(state => $"[{AzureDevOpsFieldNames.State}] <> '{state}'"));

            if (excludedStates.Length == 0)
            {
                stateQuery = string.Empty;
            }
            else
            {
                stateQuery = $"AND ({stateQuery}) ";
            }

            return stateQuery;
        }

        private string PrepareAdditionalFieldsQuery(List<string> additionalFields)
        {
            var additionalFieldsQuery = new StringBuilder();
            
            foreach (var additionalField in additionalFields)
            {
                additionalFieldsQuery.Append($", [{additionalField}] ");
            }

            return additionalFieldsQuery.ToString();
        }

        private string PrepareIgnoredTagsQuery(List<string> ignoredTags)
        {
            var ignoredTagsQuery = string.Join(" OR ", ignoredTags.Select(tag => $"[{AzureDevOpsFieldNames.Tags}] NOT CONTAINS '{tag}'"));

            if (ignoredTagsQuery.Length != 0)
            {
                ignoredTagsQuery = $"AND ({ignoredTagsQuery}) ";
            }
            else
            {
                ignoredTagsQuery = string.Empty;
            }

            return ignoredTagsQuery;
        }

        private T GetClientService<T>(Team team) where T : VssHttpClientBase
        {
            var (url, personalAccessToken) = GetAzureDevOpsConfiguration(team);
            var connection = CreateConnection(url, personalAccessToken);
            return connection.GetClient<T>();
        }

        private (string azureDevOpsUrl, string personalAccessToken) GetAzureDevOpsConfiguration(Team team)
        {
            var url = team.GetWorkTrackingSystemOptionByKey(AzureDevOpsWorkTrackingOptionNames.AzureDevOpsUrl);
            var personalAccessToken = team.GetWorkTrackingSystemOptionByKey(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);

            return (url, personalAccessToken);
        }

        private VssConnection CreateConnection(string azureDevOpsUrl, string personalAccessToken)
        {
            var azureDevOpsUri = new Uri(azureDevOpsUrl);
            var credentials = new VssBasicCredential(personalAccessToken, string.Empty);

            return new VssConnection(azureDevOpsUri, credentials);
        }
    }
}
