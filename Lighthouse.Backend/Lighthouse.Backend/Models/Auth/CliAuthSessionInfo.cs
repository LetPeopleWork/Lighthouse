namespace Lighthouse.Backend.Models.Auth
{
    public record CliAuthSessionInfo
    {
        public required string SessionId { get; init; }

        public required string VerificationUrl { get; init; }

        public required DateTime ExpiresAt { get; init; }
    }
}
