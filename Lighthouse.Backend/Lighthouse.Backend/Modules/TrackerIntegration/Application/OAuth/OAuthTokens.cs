namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public record OAuthTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
}
