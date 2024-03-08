using CMFTAspNet.Models;

namespace CMFTAspNet.Services.Interfaces
{
    public interface IWorkItemService
    {
        Task<int[]> GetClosedWorkItems(int history, Team team);
        
        Task<List<string>> GetOpenWorkItems(IEnumerable<string> workItemTypes, IWorkItemQueryOwner workItemQueryOwner);
        
        Task<int> GetRemainingRelatedWorkItems(string featureId, Team team);
        
        Task<(string name, int order)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner);
    }
}
