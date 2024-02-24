using CMFTAspNet.Models;

namespace CMFTAspNet.Services.Interfaces
{
    public interface IWorkItemService
    {
        Task<int[]> GetClosedWorkItemsForTeam(int history, Team team);
        
        Task<List<int>> GetNotClosedWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, Team team);
        
        Task<List<int>> GetNotClosedWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, Team team);
        
        Task<int> GetRemainingRelatedWorkItems(int featureId, Team team);
        
        Task<(string name, int order)> GetWorkItemDetails(int itemId, Team team);

        Task<List<int>> GetWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, Team team);

        Task<List<int>> GetWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, Team team);

        Task<bool> IsRelatedToFeature(int itemId, IEnumerable<int> featureIds, Team team);
    }
}
