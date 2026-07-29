using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    // S107 fires at the sixth constructor parameter. The pragma must wrap the declaration that
    // triggers it — anywhere else it silently does nothing (docs/ci-learnings.md, 2026-05-16).
#pragma warning disable S107
    public class WorkTrackingAuthStrategyFactory(
        PatAuthStrategy patAuthStrategy,
        JiraCloudBasicAuthStrategy jiraCloudBasicAuthStrategy,
        LinearApiKeyAuthStrategy linearApiKeyAuthStrategy,
        ServiceNowBasicAuthStrategy serviceNowBasicAuthStrategy,
        NoOpAuthStrategy noOpAuthStrategy,
        OAuthBearerAuthStrategy oauthBearerAuthStrategy)
        : IWorkTrackingAuthStrategyFactory
#pragma warning restore S107
    {
        private const string OAuthAuthenticationMethodKeySuffix = ".oauth";

        public IWorkTrackingAuthStrategy Resolve(string authenticationMethodKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationMethodKey);

            return authenticationMethodKey switch
            {
                AuthenticationMethodKeys.AzureDevOpsPat => patAuthStrategy,
                AuthenticationMethodKeys.JiraCloud => jiraCloudBasicAuthStrategy,
                AuthenticationMethodKeys.JiraDataCenter => jiraCloudBasicAuthStrategy,
                AuthenticationMethodKeys.JiraScopedToken => jiraCloudBasicAuthStrategy,
                AuthenticationMethodKeys.LinearApiKey => linearApiKeyAuthStrategy,
                AuthenticationMethodKeys.ServiceNowBasic => serviceNowBasicAuthStrategy,
                AuthenticationMethodKeys.None => noOpAuthStrategy,
                string s when s.EndsWith(OAuthAuthenticationMethodKeySuffix, StringComparison.Ordinal) => oauthBearerAuthStrategy,
                _ => throw new WorkTrackingAuthStrategyNotFoundException(
                    $"No IWorkTrackingAuthStrategy is registered for authentication method key '{authenticationMethodKey}'."),
            };
        }
    }
}
