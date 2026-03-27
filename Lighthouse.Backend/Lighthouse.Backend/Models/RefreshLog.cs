using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public enum RefreshType
    {
        Team,
        Portfolio
    }

    public class RefreshLog : IEntity
    {
        public int Id { get; set; }

        public RefreshType Type { get; set; }

        public int EntityId { get; set; }

        public string EntityName { get; set; } = string.Empty;

        public int ItemCount { get; set; }

        public long DurationMs { get; set; }

        public DateTime ExecutedAt { get; set; }

        public bool Success { get; set; }
    }
}
