using System.Text.Json.Serialization;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// One row of refresh history. Every row of an entity carries the findings of that entity's latest
    /// refresh that recorded any, with that refresh's time, so the screen shows them on whichever row it
    /// draws without searching the history itself. An entity none of whose refreshes recorded findings
    /// sends a row without the two members at all, exactly as before they existed.
    /// </summary>
    public sealed class RefreshLogDto
    {
        public int Id { get; init; }

        public RefreshType Type { get; init; }

        public int EntityId { get; init; }

        public string EntityName { get; init; } = string.Empty;

        public int ItemCount { get; init; }

        public SyncMode Mode { get; init; }

        public int RecordsScanned { get; init; }

        public int RecordsFetched { get; init; }

        public long DurationMs { get; init; }

        public DateTime ExecutedAt { get; init; }

        public bool Success { get; init; }

        public bool Cancelled { get; init; }

        public int RecordsWhoseLinksNamedMoreThanOneParent { get; init; }

        public int RecordsPlacedFromLinksNamingMoreThanOneParent { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<RefreshFinding>? LatestFullRunFindings { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? LatestFullRunAt { get; init; }

        public static List<RefreshLogDto> ListFor(IEnumerable<RefreshLog> rows)
        {
            var history = rows.ToList();

            var latestWithFindings = history
                .Where(row => row.Findings != null)
                .GroupBy(row => (row.Type, row.EntityId))
                .ToDictionary(entity => entity.Key, entity => entity.MaxBy(row => (row.ExecutedAt, row.Id))!);

            return [.. history.Select(row => From(row, latestWithFindings.GetValueOrDefault((row.Type, row.EntityId))))];
        }

        private static RefreshLogDto From(RefreshLog row, RefreshLog? latestWithFindings) => new()
        {
            Id = row.Id,
            Type = row.Type,
            EntityId = row.EntityId,
            EntityName = row.EntityName,
            ItemCount = row.ItemCount,
            Mode = row.Mode,
            RecordsScanned = row.RecordsScanned,
            RecordsFetched = row.RecordsFetched,
            DurationMs = row.DurationMs,
            ExecutedAt = row.ExecutedAt,
            Success = row.Success,
            Cancelled = row.Cancelled,
            RecordsWhoseLinksNamedMoreThanOneParent = row.RecordsWhoseLinksNamedMoreThanOneParent,
            RecordsPlacedFromLinksNamingMoreThanOneParent = row.RecordsPlacedFromLinksNamingMoreThanOneParent,
            LatestFullRunFindings = latestWithFindings?.Findings,
            LatestFullRunAt = latestWithFindings?.ExecutedAt,
        };
    }
}
