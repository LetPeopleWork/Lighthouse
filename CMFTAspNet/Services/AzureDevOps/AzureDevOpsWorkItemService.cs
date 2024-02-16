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

            return await GetClosedItemsPerDay(witClient, history, teamConfiguration.TeamProject, teamConfiguration.AreaPaths);
        }

        private async Task<int[]> GetClosedItemsPerDay(WorkItemTrackingHttpClient witClient, int numberOfDays, string teamProject, string[] areaPaths)
        {
            var closedItemsPerDay = new int[numberOfDays];

            var startDate = DateTime.UtcNow.Date.AddDays(- (numberOfDays - 1));

            var areaPathQuery = string.Join(" OR ", areaPaths.Select(path => $"[System.AreaPath] UNDER '{path}'"));
            var wiql = $"SELECT [System.Id], [System.State], [Microsoft.VSTS.Common.ClosedDate] FROM WorkItems WHERE [System.TeamProject] = '{teamProject}' AND [System.State] = 'Closed' AND ({areaPathQuery}) AND [Microsoft.VSTS.Common.ClosedDate] >= '{startDate:yyyy-MM-dd}T00:00:00.0000000Z'";

            var queryResult = await witClient.QueryByWiqlAsync(new Wiql() { Query = wiql });

            foreach (WorkItemReference workItemRef in queryResult.WorkItems)
            {
                var workItem = await witClient.GetWorkItemAsync(workItemRef.Id);
                var changedDate = DateTime.Parse(workItem.Fields["Microsoft.VSTS.Common.ClosedDate"].ToString());

                int index = (changedDate.Date - startDate).Days;

                if (index >= 0 && index < numberOfDays)
                {
                    closedItemsPerDay[index]++;
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
