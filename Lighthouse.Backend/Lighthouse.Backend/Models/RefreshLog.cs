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
    }
}
