namespace Lighthouse.Backend.Models
{
    /// <summary>What one update did: how it fetched, how many records it saw, how many it downloaded (Epic #5687).</summary>
    public sealed record SyncOutcome(SyncMode Mode, int RecordsScanned, int RecordsFetched)
    {
        /// <summary>Why this update fetched the way it did, or null when there is nothing to name (AC-5.2). An <c>init</c> member rather than a fourth positional parameter so every construction site keeps reading as what it always was.</summary>
        public string? Reason { get; init; }

        /// <summary>AC-5.2's only reason: the operator changed something the query asks the tracker for.</summary>
        public const string ConfigurationChanged = "configuration-changed";

        /// <summary>Nothing was synced - what an updater falls back on when the fetch never reported.</summary>
        public static SyncOutcome None { get; } = FullSync(0);

        /// <summary>A full sync downloads the payload of every record it scanned; slice 02 is what makes the two counts diverge.</summary>
        public static SyncOutcome FullSync(int recordCount) => new(SyncMode.Full, recordCount, recordCount);

        /// <summary>A delta sync still enumerates the whole query (D2) and downloads only what moved (D12) - the two counts diverge.</summary>
        public static SyncOutcome DeltaSync(int recordsScanned, int recordsFetched) => new(SyncMode.Delta, recordsScanned, recordsFetched);
    }
}
