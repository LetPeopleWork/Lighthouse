namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public record OAuthStateClaims
    {
        public int ConnectionId { get; init; }

        public string ProviderKey { get; init; } = string.Empty;

        public string Nonce { get; init; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; init; }
    }
}
