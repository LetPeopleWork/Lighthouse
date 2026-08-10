using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    public interface IWorkTrackingConnector
    {
        bool SupportsTransitionHistory(WorkTrackingSystemConnection connection);

        /// <summary>
        /// Whether this connection can answer a cheap identity sweep (Epic #5687, DDD-1). Per-connection
        /// rather than per-connector, because Jira Cloud and Jira Data Center are one class and do not
        /// answer the same way.
        /// </summary>
        bool SupportsIncrementalSync(WorkTrackingSystemConnection connection);

        IReadOnlyList<AdditionalFieldDefinition> GetPredefinedAdditionalFields(WorkTrackingSystemConnection connection);

        Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team);

        /// <summary>
        /// Phase 2 of the two-phase fetch (DDD-2): full payloads - fields, changelog, transitions - for
        /// the named records only.
        /// </summary>
        Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team, IReadOnlyCollection<string> referenceIds);

        /// <summary>
        /// Phase 1 of the two-phase fetch (D1): the same query the full fetch issues, asking only for
        /// identity plus the remote change timestamp. It enumerates the WHOLE result set - that is what
        /// keeps <c>removed = stored - swept</c> meaning exactly what it means today (D2).
        /// </summary>
        Task<IReadOnlyList<RemoteRecordStamp>> SweepWorkItemsForTeam(Team team);

        Task<List<Feature>> GetFeaturesForProject(Portfolio project);

        Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds);

        Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<ConnectionValidationResult> ValidateTeamSettings(Team team);

        Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio);

        Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates);
    }
}
