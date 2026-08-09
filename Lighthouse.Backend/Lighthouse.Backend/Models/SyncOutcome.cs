namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What one update actually did: how it fetched, how many records it saw, how many it downloaded.
    /// Epic #5687: reported by the sync path so the updater does not have to count after the fact.
    /// </summary>
    public sealed record SyncOutcome(SyncMode Mode, int RecordsScanned, int RecordsFetched);
}
