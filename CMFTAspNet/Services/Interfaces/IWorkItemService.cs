using CMFTAspNet.Models;

namespace CMFTAspNet.Services.Interfaces
{
    public interface IWorkItemService
    {
        Task<int[]> GetClosedWorkItemsForTeam(int history, Team team);
        
        Task<List<string>> GetNotClosedWorkItemsByAreaPath(IEnumerable<string> workItemTypes, string areaPath, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);
        
        Task<List<string>> GetNotClosedWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);
        
        Task<int> GetRemainingRelatedWorkItems(string featureId, Team team);
        
        Task<(string name, int order)> GetWorkItemDetails(string itemId, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);

        Task<List<string>> GetWorkItemsByArea(IEnumerable<string> workItemTypes, string area, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);

        Task<List<string>> GetWorkItemsByTag(IEnumerable<string> workItemTypes, string tag, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);

        Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team);
    }
}
