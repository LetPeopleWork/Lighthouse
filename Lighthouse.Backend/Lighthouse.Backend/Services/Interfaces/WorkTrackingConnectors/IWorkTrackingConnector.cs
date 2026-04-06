using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    public interface IWorkTrackingConnector
    {
        Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team);

        Task<List<Feature>> GetFeaturesForProject(Portfolio project);

        Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds);

        Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<ConnectionValidationResult> ValidateTeamSettings(Team team);

        Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio);

        Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates);
    }
}
