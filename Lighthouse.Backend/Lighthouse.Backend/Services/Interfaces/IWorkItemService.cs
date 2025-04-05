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
        Task<IEnumerable<WorkItem>> GetChangedWorkItemsSinceLastTeamUpdate(Team team);

        Task<List<Feature>> GetFeaturesForProject(Project project);

        Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery);

        string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder);

        Task<Dictionary<string, int>> GetHistoricalFeatureSize(Project project);

        Task<bool> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<bool> ValidateTeamSettings(Team team);

        Task<bool> ValidateProjectSettings(Project project);
    }
}
