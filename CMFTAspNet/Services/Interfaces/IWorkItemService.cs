using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Services.Interfaces
{
    public interface IWorkItemService
    {
        Task<int[]> GetClosedWorkItemsForTeam(int history, ITeamConfiguration teamConfiguration);

        Task<int> GetRemainingRelatedWorkItems(int featureId, ITeamConfiguration teamConfiguration);

        Task<List<int>> GetWorkItemsByAreaPath(string workItemType, string areaPath, ITeamConfiguration teamConfiguration);

        Task<List<int>> GetWorkItemsByTag(string workItemType, string tag, ITeamConfiguration teamConfiguration);
    }
}
