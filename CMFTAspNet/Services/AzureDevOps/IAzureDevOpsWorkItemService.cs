using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Services.AzureDevOps
{
    public interface IAzureDevOpsWorkItemService
    {
        Task<int[]> GetClosedWorkItemsForTeam(AzureDevOpsTeamConfiguration teamConfiguration, int history);
    }
}
