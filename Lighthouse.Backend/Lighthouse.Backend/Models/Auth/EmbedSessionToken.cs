using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.Auth
{
    // ADR-131: single use is a conditional update over these columns, not a read-then-write.
    public class EmbedSessionToken : IEntity
    {
        public int Id { get; set; }

        public string TokenId { get; set; } = string.Empty;

        public string SecretHash { get; set; } = string.Empty;

        public int ApiKeyId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? RedeemedAt { get; set; }

        public DateTime? RevokedAt { get; set; }
    }
}
