using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    // Every method here that loops over pages takes a CancellationToken; the ones that do not - validating a
    // connection, reading predefined fields - do not, because a token they could not honour
    // would teach the next reader that the parameter is decorative. A driven adapter should not have to
    // reach into the update pipeline's ambient state to learn it has been told to stop, so the ask arrives
    // where a reader meets it.
    public interface IWorkTrackingConnector
    {
        bool SupportsTransitionHistory(WorkTrackingSystemConnection connection);

        IReadOnlyList<AdditionalFieldDefinition> GetPredefinedAdditionalFields(WorkTrackingSystemConnection connection);

        Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team, CancellationToken cancellationToken);

        Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<ConnectionValidationResult> ValidateTeamSettings(Team team);

        Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio);
    }
}
