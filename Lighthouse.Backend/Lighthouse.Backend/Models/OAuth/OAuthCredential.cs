using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.OAuth
{
    public class OAuthCredential : IEntity
    {
        public int Id { get; set; }

        public int WorkTrackingSystemConnectionId { get; set; }

        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public OAuthCredentialStatus Status { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
