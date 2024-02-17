using CMFTAspNet.Models.Connections;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Interfaces;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace CMFTAspNet.Services.Implementation.AzureDevOps
{
    public class AzureDevOpsWorkItemService : IWorkItemService
    {

        public async Task<int[]> GetClosedWorkItemsForTeam(int history, ITeamConfiguration teamConfiguration)
        {
            var azureDevOpsTeamConfiguration = GetAzureDevOpsTeamConfiguration(teamConfiguration);
            var witClient = GetClientService<WorkItemTrackingHttpClient>(azureDevOpsTeamConfiguration);

            return await GetClosedItemsPerDay(witClient, history, azureDevOpsTeamConfiguration);
        }

        public async Task<List<int>> GetWorkItemsByTag(IEnumerable<string> workItemTypes, string searchTerm, ITeamConfiguration teamConfiguration)
        {
            var azureDevOpsTeamConfiguration = GetAzureDevOpsTeamConfiguration(teamConfiguration);
            var witClient = GetClientService<WorkItemTrackingHttpClient>(azureDevOpsTeamConfiguration);

            return await GetWorkItemsByTag(witClient, searchTerm, workItemTypes, azureDevOpsTeamConfiguration, []);
        }

        public async Task<List<int>> GetWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, ITeamConfiguration teamConfiguration)
        {
            var azureDevOpsTeamConfiguration = GetAzureDevOpsTeamConfiguration(teamConfiguration);
            var witClient = GetClientService<WorkItemTrackingHttpClient>(azureDevOpsTeamConfiguration);

            return await GetWorkItemsByAreaPath(witClient, areaPath, workItemTypes, azureDevOpsTeamConfiguration, []);
        }

        public async Task<List<int>> GetNotClosedWorkItemsByTag(IEnumerable<string> workItemTypes, string searchTerm, ITeamConfiguration teamConfiguration)
        {
            var azureDevOpsTeamConfiguration = GetAzureDevOpsTeamConfiguration(teamConfiguration);
            var witClient = GetClientService<WorkItemTrackingHttpClient>(azureDevOpsTeamConfiguration);

            return await GetWorkItemsByTag(witClient, searchTerm, workItemTypes, azureDevOpsTeamConfiguration, ["Closed", "Removed"]);
        }

        public async Task<List<int>> GetNotClosedWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, ITeamConfiguration teamConfiguration)
        {
            var azureDevOpsTeamConfiguration = GetAzureDevOpsTeamConfiguration(teamConfiguration);
            var witClient = GetClientService<WorkItemTrackingHttpClient>(azureDevOpsTeamConfiguration);

            return await GetWorkItemsByAreaPath(witClient, areaPath, workItemTypes, azureDevOpsTeamConfiguration, ["Closed", "Removed"]);
        }

        public async Task<int> GetRemainingRelatedWorkItems(int featureId, ITeamConfiguration teamConfiguration)
        {
            var azureDevOpsTeamConfiguration = GetAzureDevOpsTeamConfiguration(teamConfiguration);
            var witClient = GetClientService<WorkItemTrackingHttpClient>(azureDevOpsTeamConfiguration);

            return await GetRelatedWorkItems(witClient, azureDevOpsTeamConfiguration, featureId);
        }

        public async Task<bool> IsRelatedToFeature(int itemId, IEnumerable<int> featureIds, ITeamConfiguration teamConfiguration)
        {
            var azureDevOpsTeamConfiguration = GetAzureDevOpsTeamConfiguration(teamConfiguration);
            var witClient = GetClientService<WorkItemTrackingHttpClient>(azureDevOpsTeamConfiguration);

            var workItem = await GetWorkItemById(witClient, itemId, azureDevOpsTeamConfiguration, []);

            foreach (var featureId in featureIds)
            {
                if (IsWorkItemRelated(workItem, featureId, azureDevOpsTeamConfiguration.AdditionalRelatedFields))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<(string name, int order)> GetWorkItemDetails(int itemId, ITeamConfiguration teamConfiguration)
        {
            var azureDevOpsTeamConfiguration = GetAzureDevOpsTeamConfiguration(teamConfiguration);
            var witClient = GetClientService<WorkItemTrackingHttpClient>(azureDevOpsTeamConfiguration);

            var workItem = await GetWorkItemById(witClient, itemId, azureDevOpsTeamConfiguration, [AzureDevOpsFieldNames.Title, AzureDevOpsFieldNames.StackRank]);

            var workItemTitle = workItem?.Fields[AzureDevOpsFieldNames.Title].ToString() ?? string.Empty;
            var workItemStackRank = int.Parse(workItem?.Fields[AzureDevOpsFieldNames.StackRank].ToString() ?? "0");

            return (workItemTitle, workItemStackRank);
        }

        private async Task<WorkItem?> GetWorkItemById(WorkItemTrackingHttpClient witClient, int workItemId, AzureDevOpsTeamConfiguration teamConfiguration, List<string> additionalFields)
        {
            var additionalFieldsQuery = PrepareAdditionalFieldsQuery(additionalFields);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}] {additionalFieldsQuery} FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{teamConfiguration.TeamProject}' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'";
            var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql }, teamConfiguration.TeamProject);

            var workItemReference = queryResult.WorkItems.Single();
            return await witClient.GetWorkItemAsync(workItemReference.Id, expand: WorkItemExpand.Relations);
        }

        private async Task<int> GetRelatedWorkItems(WorkItemTrackingHttpClient witClient, AzureDevOpsTeamConfiguration teamConfiguration, int relatedWorkItemId)
        {
            var remainingItems = 0;

            var areaPathQuery = string.Join(" OR ", teamConfiguration.AreaPaths.Select(path => $"[{AzureDevOpsFieldNames.AreaPath}] UNDER '{path}'"));
            var workItemsQuery = string.Join(" OR ", teamConfiguration.WorkItemTypes.Select(type => $"[{AzureDevOpsFieldNames.WorkItemType}] = '{type}'"));
            var stateQuery = PrepareStateQuery(["Closed", "Removed"]);
            var ignoredTagsQuery = PrepareIgnoredTagsQuery(teamConfiguration.IgnoredTags);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{teamConfiguration.TeamProject}' " +
                $"{stateQuery}" +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) " +
                $"{ignoredTagsQuery}";

            var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql }, teamConfiguration.TeamProject);

            foreach (WorkItemReference workItemRef in queryResult.WorkItems)
            {
                var workItem = await witClient.GetWorkItemAsync(workItemRef.Id, expand: WorkItemExpand.Relations);

                if (IsWorkItemRelated(workItem, relatedWorkItemId, teamConfiguration.AdditionalRelatedFields))
                {
                    remainingItems += 1;
                }
            }

            return remainingItems;
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

        private async Task<List<int>> GetWorkItemsByAreaPath(WorkItemTrackingHttpClient witClient, string areaPath, IEnumerable<string> workItemTypes, AzureDevOpsTeamConfiguration teamConfiguration, string[] excludedStates)
        {
            var foundItemIds = new List<int>();

            var areaPathQuery = $"[{AzureDevOpsFieldNames.AreaPath}] UNDER '{areaPath}'";
            var workItemsQuery = string.Join(" OR ", workItemTypes.Select(itemType => $"[{AzureDevOpsFieldNames.WorkItemType}] = '{itemType}'"));
            var stateQuery = PrepareStateQuery(excludedStates);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{teamConfiguration.TeamProject}' " +
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

        private async Task<List<int>> GetWorkItemsByTag(WorkItemTrackingHttpClient witClient, string tag, IEnumerable<string> workItemTypes, AzureDevOpsTeamConfiguration teamConfiguration, string[] excludedStates)
        {
            var foundItemIds = new List<int>();

            var areaPathQuery = string.Join(" OR ", teamConfiguration.AreaPaths.Select(path => $"[{AzureDevOpsFieldNames.AreaPath}] UNDER '{path}'"));
            var workItemsQuery = string.Join(" OR ", workItemTypes.Select(itemType => $"[{AzureDevOpsFieldNames.WorkItemType}] = '{itemType}'"));
            var tagQuery = $"[{AzureDevOpsFieldNames.Tags}] CONTAINS '{tag}'";
            string stateQuery = PrepareStateQuery(excludedStates);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{teamConfiguration.TeamProject}' " +
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

        private async Task<int[]> GetClosedItemsPerDay(WorkItemTrackingHttpClient witClient, int numberOfDays, AzureDevOpsTeamConfiguration teamConfiguration)
        {
            var closedItemsPerDay = new int[numberOfDays];

            var startDate = DateTime.UtcNow.Date.AddDays(-(numberOfDays - 1));

            var areaPathQuery = string.Join(" OR ", teamConfiguration.AreaPaths.Select(path => $"[{AzureDevOpsFieldNames.AreaPath}] UNDER '{path}'"));
            var workItemsQuery = string.Join(" OR ", teamConfiguration.WorkItemTypes.Select(type => $"[{AzureDevOpsFieldNames.WorkItemType}] = '{type}'"));
            var ignoredTagsQuery = PrepareIgnoredTagsQuery(teamConfiguration.IgnoredTags);

            var wiql = $"SELECT [{AzureDevOpsFieldNames.Id}], [{AzureDevOpsFieldNames.State}], [{AzureDevOpsFieldNames.ClosedDate}] FROM WorkItems WHERE [{AzureDevOpsFieldNames.TeamProject}] = '{teamConfiguration.TeamProject}' AND [{AzureDevOpsFieldNames.State}] = 'Closed' " +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) " +
                $"{ignoredTagsQuery}" +
                $"AND [{AzureDevOpsFieldNames.ClosedDate}] >= '{startDate:yyyy-MM-dd}T00:00:00.0000000Z'";

            var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql });

            foreach (WorkItemReference workItemRef in queryResult.WorkItems)
            {
                var workItem = await witClient.GetWorkItemAsync(workItemRef.Id);
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
            var additionalFieldsQuery = string.Empty;
            
            foreach (var additionalField in additionalFields)
            {
                additionalFieldsQuery += $", [{additionalField}] ";
            }

            return additionalFieldsQuery;
        }

        private string PrepareIgnoredTagsQuery(List<string> ignoredTags)
        {
            var ignoredTagsQuery = string.Join(" OR ", ignoredTags.Select(tag => $"[{AzureDevOpsFieldNames.Tags}] NOT CONTAINS '{tag}'"));

            if (ignoredTagsQuery.Any())
            {
                ignoredTagsQuery = $"AND ({ignoredTagsQuery}) ";
            }
            else
            {
                ignoredTagsQuery = string.Empty;
            }

            return ignoredTagsQuery;
        }

        private T GetClientService<T>(AzureDevOpsTeamConfiguration teamConfiguration) where T : VssHttpClientBase
        {
            var connection = CreateConnection(teamConfiguration.AzureDevOpsConfiguration);
            return connection.GetClient<T>();
        }

        private AzureDevOpsTeamConfiguration GetAzureDevOpsTeamConfiguration(ITeamConfiguration teamConfiguration)
        {
            if (teamConfiguration is AzureDevOpsTeamConfiguration azureDevOpsTeamConfiguration)
            {
                return azureDevOpsTeamConfiguration;
            }

            throw new NotSupportedException("Only Azure DevOps Team Configuration supported");
        }

        private VssConnection CreateConnection(AzureDevOpsConfiguration azureDevOpsConfig)
        {
            var azureDevOpsUri = new Uri(azureDevOpsConfig.Url);
            var credentials = new VssBasicCredential(azureDevOpsConfig.PersonalAccessToken, string.Empty);

            return new VssConnection(azureDevOpsUri, credentials);
        }
    }
}
