using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    public class WorkTrackingAuthStrategyFactory(
        PatAuthStrategy patAuthStrategy,
        JiraCloudBasicAuthStrategy jiraCloudBasicAuthStrategy,
        LinearApiKeyAuthStrategy linearApiKeyAuthStrategy,
        NoOpAuthStrategy noOpAuthStrategy)
        : IWorkTrackingAuthStrategyFactory
    {
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
                _ => throw new WorkTrackingAuthStrategyNotFoundException(
                    $"No IWorkTrackingAuthStrategy is registered for authentication method key '{authenticationMethodKey}'."),
            };
        }
    }
}
