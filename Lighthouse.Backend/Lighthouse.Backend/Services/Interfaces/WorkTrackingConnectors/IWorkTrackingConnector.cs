using Lighthouse.Backend.Models;

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
    }
}
