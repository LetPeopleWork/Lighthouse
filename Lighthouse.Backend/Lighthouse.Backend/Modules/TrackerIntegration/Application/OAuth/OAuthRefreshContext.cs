namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    public sealed record OAuthRefreshContext(string RefreshToken, string ClientId, string ClientSecret, string? TenantId = null);
}
