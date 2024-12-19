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

        Task<(List<string> remainingWorkItems, List<string> allWorkItems)> GetWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery);

        Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(string featureId, Team team);

        Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team);

        Task<(string name, string order, string url, string state)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner);

        string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder);

        Task<bool> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<int> GetEstimatedSizeForItem(string referenceId, Project project);

        Task<IEnumerable<int>> GetChildItemsForFeaturesInProject(Project project);

        Task<IEnumerable<string>> GetFeaturesInProgressForTeam(Team team);

        Task<bool> ValidateTeamSettings(Team team);

        Task<bool> ValidateProjectSettings(Project project);
    }
}
