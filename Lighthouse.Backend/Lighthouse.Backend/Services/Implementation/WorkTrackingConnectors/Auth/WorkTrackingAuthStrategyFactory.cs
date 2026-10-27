using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    // S107 (too many constructor parameters) is reported on the declaration itself, so the pragma
    // has to wrap that declaration; placed anywhere else it silently does nothing.
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
