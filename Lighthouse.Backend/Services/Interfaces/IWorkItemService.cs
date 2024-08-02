using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public enum RelativeOrder
    {
        Above,
        Below,
    }

    public interface IWorkItemService
    {
        Task<int[]> GetClosedWorkItems(int history, Team team);
        
        Task<List<string>> GetOpenWorkItems(IEnumerable<string> workItemTypes, IWorkItemQueryOwner workItemQueryOwner);

        Task<List<string>> GetOpenWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery);

        Task<int> GetRemainingRelatedWorkItems(string featureId, Team team);

        Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team);

        Task<(string name, string order, string url)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner);

        Task<bool> ItemHasChildren(string referenceId, IWorkTrackingSystemOptionsOwner workTrackingSystemOptionsOwner);

        string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder);

        Task<bool> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<int> GetEstimatedSizeForItem(string referenceId, Project project);
    }
}
