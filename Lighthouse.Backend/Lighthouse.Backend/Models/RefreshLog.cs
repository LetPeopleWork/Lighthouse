using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    // Append new members only. A member inserted above an existing one shifts the numbers already
    // written to the Type column of the refresh log, which would relabel every refresh recorded so far.
    public enum RefreshType
    {
        Team,
        Portfolio,
        Forecast
    }

    public class RefreshLog : IEntity
    {
        public int Id { get; set; }

        public RefreshType Type { get; set; }

        public int EntityId { get; set; }

        public string EntityName { get; set; } = string.Empty;

        public int ItemCount { get; set; }

        public SyncMode Mode { get; set; }

        public int RecordsScanned { get; set; }

        public int RecordsFetched { get; set; }

        public long DurationMs { get; set; }

        public DateTime ExecutedAt { get; set; }

        public bool Success { get; set; }

        /// <summary>
        /// Whether an operator stopped this refresh. A cancel is not a failure - somebody chose it - and
        /// reporting the two the same way puts a red row in refresh history for something that went exactly
        /// as asked. Kept beside <see cref="Success"/> rather than replacing it: migrations here are
        /// expand-only, so a column cannot be renamed or dropped.
        /// </summary>
        public bool Cancelled { get; set; }

        /// <summary>
        /// Whether this forecast was confirmed rather than computed, because nothing it is computed from had
        /// changed since the last one. A row written before skipping existed reads false, which is what it was.
        /// </summary>
        public bool Skipped { get; set; }

        /// <summary>
        /// How many records this refresh read whose links named more than one issue to hang them under, and
        /// that it could not place under any of them. Each of those took no parent from its links, which on
        /// screen is indistinguishable from a record nobody ever linked - so without a number here, refresh
        /// history shows a tracker that needs tidying and one that does not as exactly the same thing.
        /// </summary>
        public int RecordsWhoseLinksNamedMoreThanOneParent { get; set; }

        /// <summary>
        /// How many records this refresh read whose links named more than one issue, and that it placed under
        /// one of them all the same. Kept apart from the ones it could not place, so an administrator can see
        /// how often a parent was decided on their behalf.
        /// </summary>
        public int RecordsPlacedFromLinksNamingMoreThanOneParent { get; set; }

        /// <summary>
        /// What this refresh found wrong with how a setting is configured. Only a whole download that
        /// finished can judge that, so only such a refresh writes a list here, and an empty list means it
        /// found nothing wrong. A cheap refresh reads too little to judge and a failed one did not finish,
        /// so both leave this null, as does a row written before findings were recorded; Refresh History
        /// then keeps showing what the last refresh that did judge found.
        /// </summary>
        public List<RefreshFinding>? Findings { get; set; }
    }
}
