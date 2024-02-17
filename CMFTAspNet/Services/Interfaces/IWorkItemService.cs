using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Services.Interfaces
{
    public interface IWorkItemService
    {
        Task<int[]> GetClosedWorkItemsForTeam(int history, ITeamConfiguration teamConfiguration);

        Task<int> GetRemainingRelatedWorkItems(int featureId, ITeamConfiguration teamConfiguration);

        Task<List<int>> GetWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, ITeamConfiguration teamConfiguration);

        Task<List<int>> GetWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, ITeamConfiguration teamConfiguration);
    }
}
