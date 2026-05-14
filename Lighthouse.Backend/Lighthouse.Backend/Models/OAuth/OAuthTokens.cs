namespace Lighthouse.Backend.Models.OAuth
{
    public record OAuthTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
}
