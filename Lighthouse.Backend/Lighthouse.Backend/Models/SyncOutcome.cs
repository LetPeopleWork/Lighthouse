namespace Lighthouse.Backend.Models
{
    /// <summary>What one update did: how it fetched, how many records it saw, how many it downloaded (Epic #5687).</summary>
    public sealed record SyncOutcome(SyncMode Mode, int RecordsScanned, int RecordsFetched)
    {
        /// <summary>Nothing was synced - what an updater falls back on when the fetch never reported.</summary>
        public static SyncOutcome None { get; } = FullSync(0);

        /// <summary>A full sync downloads the payload of every record it scanned; slice 02 is what makes the two counts diverge.</summary>
        public static SyncOutcome FullSync(int recordCount) => new(SyncMode.Full, recordCount, recordCount);

        /// <summary>A delta sync still enumerates the whole query (D2) and downloads only what moved (D12) - the two counts diverge.</summary>
        public static SyncOutcome DeltaSync(int recordsScanned, int recordsFetched) => new(SyncMode.Delta, recordsScanned, recordsFetched);
    }
}
