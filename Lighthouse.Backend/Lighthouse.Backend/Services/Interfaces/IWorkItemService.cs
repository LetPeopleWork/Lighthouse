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
        Task<IEnumerable<WorkItem>> UpdateWorkItemsForTeam(Team team);

        Task<int[]> GetThroughputForTeam(Team team);

        Task<string[]> GetClosedWorkItemsForTeam(Team team);

        Task<List<string>> GetFeaturesForProject(Project project);

        Task<(List<string> remainingWorkItems, List<string> allWorkItems)> GetWorkItemsByQuery(List<string> workItemTypes, Team team, string unparentedItemsQuery);

        Task<(int remainingItems, int totalItems)> GetRelatedWorkItems(string featureId, Team team);

        Task<bool> IsRelatedToFeature(string itemId, IEnumerable<string> featureIds, Team team);

        Task<(string name, string order, string url, string state, DateTime? startedDate, DateTime? closedDate)> GetWorkItemDetails(string itemId, IWorkItemQueryOwner workItemQueryOwner);

        string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder);

        Task<bool> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<int> GetEstimatedSizeForItem(string referenceId, Project project);

        Task<string> GetFeatureOwnerByField(string referenceId, Project project);

        Task<IEnumerable<int>> GetChildItemsForFeaturesInProject(Project project);

        Task<IEnumerable<string>> GetFeaturesInProgressForTeam(Team team);

        Task<bool> ValidateTeamSettings(Team team);

        Task<bool> ValidateProjectSettings(Project project);
    }
}
