using CMFTAspNet.Models.Connections;
using CMFTAspNet.Models.Teams;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace CMFTAspNet.Services.AzureDevOps
{
    public class AzureDevOpsWorkItemService : IAzureDevOpsWorkItemService
    {
        public async Task<int[]> GetClosedWorkItemsForTeam(AzureDevOpsTeamConfiguration teamConfiguration, int history)
        {
            var connection = CreateConnection(teamConfiguration.AzureDevOpsConfiguration);
            var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            return await GetClosedItemsPerDay(witClient, history, teamConfiguration);
        }

        public async Task<List<int>> GetWorkItemsByTag(string workItemType, string searchTerm, AzureDevOpsTeamConfiguration teamConfiguration)
        {
            var connection = CreateConnection(teamConfiguration.AzureDevOpsConfiguration);
            var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            return await GetWorkItemsByTag(witClient, searchTerm, workItemType, teamConfiguration);
        }

        public async Task<List<int>> GetWorkItemsByAreaPath(string workItemType, string areaPath, AzureDevOpsTeamConfiguration teamConfiguration)
        {
            var connection = CreateConnection(teamConfiguration.AzureDevOpsConfiguration);
            var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            return await GetWorkItemsByAreaPath(witClient, areaPath, workItemType, teamConfiguration);
        }

        public async Task<int> GetRemainingRelatedWorkItems(AzureDevOpsTeamConfiguration teamConfiguration, int featureId)
        {
            var connection = CreateConnection(teamConfiguration.AzureDevOpsConfiguration);
            var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            return await GetRelatedWorkItems(witClient, teamConfiguration, featureId);
        }

        private async Task<int> GetRelatedWorkItems(WorkItemTrackingHttpClient witClient, AzureDevOpsTeamConfiguration teamConfiguration, int relatedWorkItemId)
        {
            var remainingItems = 0;

            var areaPathQuery = string.Join(" OR ", teamConfiguration.AreaPaths.Select(path => $"[System.AreaPath] UNDER '{path}'"));
            var workItemsQuery = string.Join(" OR ", teamConfiguration.WorkItemType.Select(type => $"[System.WorkItemType] = '{type}'"));
            var ignoredTagsQuery = string.Join(" OR ", teamConfiguration.IgnoredTags.Select(tag => $"[System.Tags] NOT CONTAINS '{tag}'"));

            var wiql = $"SELECT [System.Id], [System.State], [Microsoft.VSTS.Common.ClosedDate] FROM WorkItems WHERE [System.TeamProject] = '{teamConfiguration.TeamProject}' AND ([System.State] <> 'Closed' AND [System.State] <> 'Removed') " +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) " +
                $"AND ({ignoredTagsQuery}) ";

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
                    if (relation.Rel == "System.LinkTypes.Hierarchy-Reverse" && relation.Url.Contains($"/{relatedWorkItemId}"))
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

        private async Task<List<int>> GetWorkItemsByAreaPath(WorkItemTrackingHttpClient witClient, string areaPath, string workItemType, AzureDevOpsTeamConfiguration teamConfiguration)
        {
            var foundItemIds = new List<int>();

            var areaPathQuery = $"[System.AreaPath] UNDER '{areaPath}'";
            var workItemsQuery = $"[System.WorkItemType] = '{workItemType}'";

            var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{teamConfiguration.TeamProject}' " +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) ";

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

        private async Task<List<int>> GetWorkItemsByTag(WorkItemTrackingHttpClient witClient, string tag, string workItemType, AzureDevOpsTeamConfiguration teamConfiguration)
        {
            var foundItemIds = new List<int>();

            var areaPathQuery = string.Join(" OR ", teamConfiguration.AreaPaths.Select(path => $"[System.AreaPath] UNDER '{path}'"));
            var workItemsQuery = $"[System.WorkItemType] = '{workItemType}'";
            var tagQuery = $"[System.Tags] CONTAINS '{tag}'";

            var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{teamConfiguration.TeamProject}' " +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) " +
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

            var areaPathQuery = string.Join(" OR ", teamConfiguration.AreaPaths.Select(path => $"[System.AreaPath] UNDER '{path}'"));
            var workItemsQuery = string.Join(" OR ", teamConfiguration.WorkItemType.Select(type => $"[System.WorkItemType] = '{type}'"));
            var ignoredTagsQuery = string.Join(" OR ", teamConfiguration.IgnoredTags.Select(tag => $"[System.Tags] NOT CONTAINS '{tag}'"));

            var wiql = $"SELECT [System.Id], [System.State], [Microsoft.VSTS.Common.ClosedDate] FROM WorkItems WHERE [System.TeamProject] = '{teamConfiguration.TeamProject}' AND [System.State] = 'Closed' " +
                $"AND ({areaPathQuery}) " +
                $"AND ({workItemsQuery}) " +
                $"AND ({ignoredTagsQuery}) " +
                $"AND [Microsoft.VSTS.Common.ClosedDate] >= '{startDate:yyyy-MM-dd}T00:00:00.0000000Z'";

            var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql });

            foreach (WorkItemReference workItemRef in queryResult.WorkItems)
            {
                var workItem = await witClient.GetWorkItemAsync(workItemRef.Id);
                var closedDate = workItem.Fields["Microsoft.VSTS.Common.ClosedDate"].ToString();

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

        private VssConnection CreateConnection(AzureDevOpsConfiguration azureDevOpsConfig)
        {
            var azureDevOpsUri = new Uri(azureDevOpsConfig.Url);
            var credentials = new VssBasicCredential(azureDevOpsConfig.PersonalAccessToken, string.Empty);

            return new VssConnection(azureDevOpsUri, credentials);
        }
    }
}
