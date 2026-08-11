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

        /// <summary>
        /// Phase 2 of the two-phase portfolio fetch (Epic #5687 slice 03): full payloads - fields,
        /// changelog, transitions - for the named Features only. Adjacent to its sibling on purpose:
        /// S4136 is error-severity here and fires in every implementing file at once.
        /// </summary>
        Task<List<Feature>> GetFeaturesForProject(Portfolio project, IReadOnlyCollection<string> referenceIds);

        /// <summary>
        /// Phase 1 of the two-phase portfolio fetch (Epic #5687 slice 03): the same query
        /// <see cref="GetFeaturesForProject(Portfolio)"/> issues, asking only for identity plus the remote
        /// change timestamp. It enumerates the WHOLE result set, which is what keeps
        /// <c>removed = stored - swept</c> meaning exactly what it means today (D2).
        /// </summary>
        Task<IReadOnlyList<RemoteRecordStamp>> SweepFeaturesForPortfolio(Portfolio project);

        Task<List<Feature>> GetParentFeaturesDetails(Portfolio project, IEnumerable<string> parentFeatureIds);

        /// <summary>
        /// Phase 1 of the parent-Feature fetch (Epic #5687 slice 03). The parent path is already a keyed
        /// query, so its sweep is that same keyed query asking only for identity plus the remote change
        /// timestamp - and phase 2 is <see cref="GetParentFeaturesDetails"/> called with a shorter key
        /// list, which is why the parent path needs no second overload.
        ///
        /// <paramref name="parentFeatureIds"/> is derived from what is STORED, never from what this cycle
        /// fetched: deriving it from the fetched set shrinks it under delta and parents drop out silently.
        /// </summary>
        Task<IReadOnlyList<RemoteRecordStamp>> SweepParentFeatures(Portfolio project, IEnumerable<string> parentFeatureIds);

        Task<ConnectionValidationResult> ValidateConnection(WorkTrackingSystemConnection connection);

        Task<ConnectionValidationResult> ValidateTeamSettings(Team team);

        Task<ConnectionValidationResult> ValidatePortfolioSettings(Portfolio portfolio);

        Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates);
    }
}
