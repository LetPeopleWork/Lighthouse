using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    /// <summary>
    /// Sweeping a query for identities and remote change stamps, then fetching in full only the records that moved.
    /// Declared apart from the connector, because only some trackers can answer a cheap sweep; a connector that cannot
    /// does not take this on, and its Teams and Portfolios are downloaded in full on every refresh.
    /// </summary>
    public interface IIncrementalSource
    {
        /// <summary>
        /// Whether this connection can answer a sweep now. Asked per connection rather than per connector, because
        /// Jira Cloud and Jira Data Center are one connector and do not answer the same way.
        /// </summary>
        bool SupportsIncrementalSync(WorkTrackingSystemConnection connection);

        /// <summary>
        /// The same query the full download issues, asking only for identity plus the remote change stamp. It
        /// enumerates the WHOLE result set: the records stored but not swept are the ones that left the query.
        /// </summary>
        Task<IReadOnlyList<RemoteRecordStamp>> SweepWorkItemsForTeam(Team team, CancellationToken cancellationToken);

        /// <summary>Full payloads (fields, changelog, transitions) for the named work items only.</summary>
        Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team, IReadOnlyCollection<string> referenceIds, CancellationToken cancellationToken);

        /// <summary>
        /// The same query <see cref="IFeatureSource.GetFeaturesForPortfolio(Portfolio, CancellationToken)"/> issues,
        /// asking only for identity plus the remote change stamp. It enumerates the WHOLE result set, for the same
        /// reason as the Team sweep.
        /// </summary>
        Task<IReadOnlyList<RemoteRecordStamp>> SweepFeaturesForPortfolio(Portfolio portfolio, CancellationToken cancellationToken);

        /// <summary>Full payloads (fields, changelog, transitions) for the named Features only.</summary>
        Task<List<Feature>> GetFeaturesForPortfolio(Portfolio portfolio, IReadOnlyCollection<string> referenceIds, CancellationToken cancellationToken);

        /// <summary>
        /// The parent read is already a keyed query, so its sweep is that same keyed query asking only for identity
        /// plus the remote change stamp; the records that moved are then read with
        /// <see cref="IFeatureSource.GetParentFeaturesDetails"/> over a shorter key list.
        ///
        /// <paramref name="parentFeatureIds"/> is derived from what is STORED, never from what this cycle fetched:
        /// deriving it from the fetched set shrinks it with every partial download, and parents drop out silently.
        /// </summary>
        Task<IReadOnlyList<RemoteRecordStamp>> SweepParentFeatures(Portfolio portfolio, IEnumerable<string> parentFeatureIds, CancellationToken cancellationToken);
    }
}
