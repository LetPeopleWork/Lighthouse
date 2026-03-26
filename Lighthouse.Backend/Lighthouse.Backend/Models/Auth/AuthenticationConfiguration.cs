namespace Lighthouse.Backend.Models.Auth
{
    public record AuthenticationConfiguration
    {
        public bool Enabled { get; init; }

        public string Authority { get; init; } = string.Empty;

        public string ClientId { get; init; } = string.Empty;

        public string ClientSecret { get; init; } = string.Empty;

        public string CallbackPath { get; init; } = "/api/auth/callback";

        public string SignedOutCallbackPath { get; init; } = "/api/auth/signout-callback";

        public bool RequireHttpsMetadata { get; init; } = true;

        public string MetadataAddress { get; init; } = string.Empty;

        public IReadOnlyList<string> Scopes { get; init; } = ["openid", "profile", "email"];

        public IReadOnlyList<string> AllowedOrigins { get; init; } = [];

        public int SessionLifetimeMinutes { get; init; } = 480;

        public IReadOnlyList<string> TrustedProxies { get; init; } = [];

        public IReadOnlyList<string> TrustedNetworks { get; init; } = [];
    }
}
