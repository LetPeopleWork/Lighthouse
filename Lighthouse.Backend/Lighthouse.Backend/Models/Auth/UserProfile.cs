namespace Lighthouse.Backend.Models.Auth
{
    public class UserProfile : IConcurrencyTokenEntity
    {
        public int Id { get; set; }

        public Guid ConcurrencyToken { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string SubjectClaimType { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastSeenAt { get; set; }

        public string? LastKnownGroupClaimValues { get; set; }
    }
}