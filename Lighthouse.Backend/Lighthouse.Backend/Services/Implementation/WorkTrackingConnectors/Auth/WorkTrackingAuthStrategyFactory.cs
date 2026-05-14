using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    public class WorkTrackingAuthStrategyFactory(
        PatAuthStrategy patAuthStrategy,
        JiraCloudBasicAuthStrategy jiraCloudBasicAuthStrategy,
        LinearApiKeyAuthStrategy linearApiKeyAuthStrategy,
        NoOpAuthStrategy noOpAuthStrategy,
        OAuthBearerAuthStrategy oauthBearerAuthStrategy)
        : IWorkTrackingAuthStrategyFactory
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
                AuthenticationMethodKeys.None => noOpAuthStrategy,
                string s when s.EndsWith(OAuthAuthenticationMethodKeySuffix, StringComparison.Ordinal) => oauthBearerAuthStrategy,
                _ => throw new WorkTrackingAuthStrategyNotFoundException(
                    $"No IWorkTrackingAuthStrategy is registered for authentication method key '{authenticationMethodKey}'."),
            };
        }
    }
}
