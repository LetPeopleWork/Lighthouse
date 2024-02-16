using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Services.AzureDevOps
{
    public interface IAzureDevOpsWorkItemService
    {
        int[] GetClosedWorkItemsForTeam(AzureDevOpsTeamConfiguration teamConfiguration, int history);
    }
}
