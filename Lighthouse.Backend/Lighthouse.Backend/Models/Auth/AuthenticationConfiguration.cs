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

        public IReadOnlyList<string> Scopes { get; init; } = ["openid", "profile", "email"];
    }
}
