namespace Lighthouse.Backend.Models.OAuth
{
    public sealed record OAuthRefreshContext(string RefreshToken, string ClientId, string ClientSecret);
}
