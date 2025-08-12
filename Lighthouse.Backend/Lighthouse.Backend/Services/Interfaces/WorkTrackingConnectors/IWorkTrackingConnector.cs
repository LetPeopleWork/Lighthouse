using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    public enum RelativeOrder
    {
        Above,
        Below,
    }

    public interface IWorkTrackingConnector
    {
        Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team);

        Task<List<Feature>> GetFeaturesForProject(Project project);

        Task<List<Feature>> GetParentFeaturesDetails(Project project, IEnumerable<string> parentFeatureIds);

        Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery);

        string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder);

        Task<bool> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<bool> ValidateTeamSettings(Team team);

        Task<bool> ValidateProjectSettings(Project project);
    }
}
