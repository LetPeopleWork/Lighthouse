using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.WorkItems
{
    /// <summary>
    /// How much one refresh has to download. There is no partial mode: everything the
    /// refresh cannot resolve with certainty resolves to <see cref="SyncMode.Full"/>, because the
    /// expensive answer is the safe one.
    /// </summary>
    public static class SyncModeResolver
    {
        /// <param name="storedWorkItems">
        /// What the team or the portfolio already has. Typed as the shared base so a portfolio's stored
        /// Features reach the same decision - <see cref="Feature"/> is a sibling of <see cref="WorkItem"/>,
        /// and the stamp this reads lives on the base. There is no per-entity "was this ever swept"
        /// column, so "never swept" is exactly "nothing stored, or something stored without a remote
        /// change stamp" - with nothing to compare against, a delta would silently skip records.
        /// </param>
        /// <param name="fetchShapeChanged">
        /// Whether what the query asks the tracker for changed since the last cycle. A wider
        /// fetch has to re-download records whose timestamps did not move.
        /// </param>
        public static SyncMode Resolve(
            bool trackerCanBeScanned,
            IReadOnlyCollection<WorkItemBase> storedWorkItems,
            bool scanSucceeded,
            bool fetchShapeChanged)
        {
            if (!trackerCanBeScanned)
            {
                return SyncMode.Full;
            }

            if (!scanSucceeded)
            {
                return SyncMode.Full;
            }

            if (fetchShapeChanged)
            {
                return SyncMode.Full;
            }

            if (storedWorkItems.Count == 0)
            {
                return SyncMode.Full;
            }

            if (storedWorkItems.Any(workItem => workItem.LastChangedRemote == null))
            {
                return SyncMode.Full;
            }

            return SyncMode.Delta;
        }
    }
}
