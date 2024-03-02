using CMFTAspNet.Models;

namespace CMFTAspNet.Services.Interfaces
{
    public interface IWorkItemService
    {
        Task<int[]> GetClosedWorkItemsForTeam(int history, Team team);
        
        Task<List<int>> GetNotClosedWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);
        
        Task<List<int>> GetNotClosedWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);
        
        Task<int> GetRemainingRelatedWorkItems(int featureId, Team team);
        
        Task<(string name, int order)> GetWorkItemDetails(int itemId, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);

        Task<List<int>> GetWorkItemsByArea(IEnumerable<string> workItemTypes, string area, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);

        Task<List<int>> GetWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);

        Task<bool> IsRelatedToFeature(int itemId, IEnumerable<int> featureIds, Team team);
    }
}
