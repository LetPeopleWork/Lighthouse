using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Services.Interfaces
{
    public interface IWorkItemService
    {
        Task<int[]> GetClosedWorkItemsForTeam(int history, ITeamConfiguration teamConfiguration);
        
        Task<List<int>> GetNotClosedWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, ITeamConfiguration teamConfiguration);
        
        Task<List<int>> GetNotClosedWorkItemsByTag(IEnumerable<string> workItemTypes, string searchTerm, ITeamConfiguration teamConfiguration);
        
        Task<int> GetRemainingRelatedWorkItems(int featureId, ITeamConfiguration teamConfiguration);
        
        Task<(string name, int order)> GetWorkItemDetails(int itemId, ITeamConfiguration teamConfiguration);

        Task<List<int>> GetWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, ITeamConfiguration teamConfiguration);

        Task<List<int>> GetWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, ITeamConfiguration teamConfiguration);

        Task<bool> IsRelatedToFeature(int itemId, IEnumerable<int> featureIds, ITeamConfiguration teamConfiguration);
    }
}
