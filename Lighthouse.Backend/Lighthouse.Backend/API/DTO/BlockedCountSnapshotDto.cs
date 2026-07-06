namespace Lighthouse.Backend.API.DTO
{
    public class BlockedCountSnapshotDto
    {
        public string RecordedAt { get; set; } = string.Empty;

        public int BlockedCount { get; set; }
    }
}
