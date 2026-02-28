using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    public interface IWorkTrackingConnector
    {
        Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team);

        Task<List<Feature>> GetFeaturesForProject(Portfolio project);

        Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds);

        Task<bool> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<bool> ValidateTeamSettings(Team team);

        Task<bool> ValidatePortfolioSettings(Portfolio portfolio);

        Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates);
    }
}
