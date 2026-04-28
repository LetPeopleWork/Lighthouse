using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.Auth
{
    public class ApiKey : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string KeyHash { get; set; } = string.Empty;

        public string Salt { get; set; } = string.Empty;

        public string CreatedByUser { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }
    }
}
