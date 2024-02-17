using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Services.AzureDevOps
{
    public interface IAzureDevOpsWorkItemService
    {
        Task<int[]> GetClosedWorkItemsForTeam(AzureDevOpsTeamConfiguration teamConfiguration, int history);

        Task<int> GetRemainingRelatedWorkItems(AzureDevOpsTeamConfiguration teamConfiguration, int featureId);

        Task<List<int>> GetWorkItemsByAreaPath(string workItemType, string areaPath, AzureDevOpsTeamConfiguration azureDevOpsTeamConfiguration);

        Task<List<int>> GetWorkItemsByTag(string workItemType, string tag, AzureDevOpsTeamConfiguration azureDevOpsTeamConfiguration);
    }
}
