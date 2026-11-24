using Lighthouse.Backend.Models.OAuth;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth
{
    /// <param name="GrantedRights">What the identity platform granted on work items; null for a provider that does not report it.</param>
    public record OAuthTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, WorkItemRights? GrantedRights = null);
}
