using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.Jira;
using System.Net.Http.Headers;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira
{
    public class JiraOAuthWorkTrackingConnector : IWorkTrackingConnector
    {
        private readonly JiraWorkTrackingConnector baseConnector;
        private readonly IOAuthService oauthService;
        private readonly ICryptoService cryptoService;
        private readonly ILogger<JiraOAuthWorkTrackingConnector> logger;

        public JiraOAuthWorkTrackingConnector(
            JiraWorkTrackingConnector baseConnector,
            IOAuthService oauthService,
            ICryptoService cryptoService,
            ILogger<JiraOAuthWorkTrackingConnector> logger)
        {
            this.baseConnector = baseConnector;
            this.oauthService = oauthService;
            this.cryptoService = cryptoService;
            this.logger = logger;
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItemsForTeam(Team team)
        {
            // Use reflection to call the private method in baseConnector or create a shared base class
            // For now, delegate to the base connector but this would need the private methods to be protected
            return await baseConnector.GetWorkItemsForTeam(team);
        }

        public async Task<List<Feature>> GetFeaturesForProject(Project project)
        {
            return await baseConnector.GetFeaturesForProject(project);
        }

        public async Task<Dictionary<string, int>> GetHistoricalFeatureSize(Project project)
        {
            return await baseConnector.GetHistoricalFeatureSize(project);
        }

        public async Task<List<string>> GetWorkItemsIdsForTeamWithAdditionalQuery(Team team, string additionalQuery)
        {
            return await baseConnector.GetWorkItemsIdsForTeamWithAdditionalQuery(team, additionalQuery);
        }

        public string GetAdjacentOrderIndex(IEnumerable<string> existingItemsOrder, RelativeOrder relativeOrder)
        {
            return baseConnector.GetAdjacentOrderIndex(existingItemsOrder, relativeOrder);
        }

        public async Task<bool> ValidateConnection(WorkTrackingSystemConnection connection)
        {
            try
            {
                logger.LogInformation("Validating Jira OAuth connection");
                
                var accessToken = await GetValidOAuthAccessToken(connection);
                if (string.IsNullOrEmpty(accessToken))
                {
                    return false;
                }

                var jiraUrl = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraOAuthWorkTrackingOptionNames.Url);
                return await oauthService.ValidateAccessToken(accessToken, jiraUrl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to validate Jira OAuth connection");
                return false;
            }
        }

        public async Task<bool> ValidateTeamSettings(Team team)
        {
            return await baseConnector.ValidateTeamSettings(team);
        }

        public async Task<bool> ValidateProjectSettings(Project project)
        {
            return await baseConnector.ValidateProjectSettings(project);
        }

        private async Task<string> GetValidOAuthAccessToken(WorkTrackingSystemConnection connection)
        {
            try
            {
                var encryptedAccessToken = GetConnectionOption(connection, JiraOAuthWorkTrackingOptionNames.AccessToken);
                if (string.IsNullOrEmpty(encryptedAccessToken))
                {
                    logger.LogDebug("No access token found in connection");
                    return string.Empty;
                }

                var accessToken = cryptoService.Decrypt(encryptedAccessToken);
                var jiraUrl = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraOAuthWorkTrackingOptionNames.Url);

                // Check if token is still valid
                if (await oauthService.ValidateAccessToken(accessToken, jiraUrl))
                {
                    return accessToken;
                }

                // Try to refresh the token
                logger.LogInformation("Access token expired, attempting to refresh");
                return await RefreshOAuthToken(connection);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get valid OAuth access token");
                return string.Empty;
            }
        }

        private async Task<string> RefreshOAuthToken(WorkTrackingSystemConnection connection)
        {
            try
            {
                var clientId = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraOAuthWorkTrackingOptionNames.ClientId);
                var encryptedClientSecret = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraOAuthWorkTrackingOptionNames.ClientSecret);
                var encryptedRefreshToken = GetConnectionOption(connection, JiraOAuthWorkTrackingOptionNames.RefreshToken);

                if (string.IsNullOrEmpty(encryptedRefreshToken))
                {
                    logger.LogWarning("No refresh token available");
                    return string.Empty;
                }

                var clientSecret = cryptoService.Decrypt(encryptedClientSecret);
                var refreshToken = cryptoService.Decrypt(encryptedRefreshToken);

                var tokenResponse = await oauthService.RefreshAccessToken(clientId, clientSecret, refreshToken);

                // Update the connection with new tokens
                await UpdateConnectionTokens(connection, tokenResponse.AccessToken, tokenResponse.RefreshToken);

                logger.LogInformation("Successfully refreshed OAuth tokens");
                return tokenResponse.AccessToken;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh OAuth token");
                return string.Empty;
            }
        }

        private Task UpdateConnectionTokens(WorkTrackingSystemConnection connection, string newAccessToken, string newRefreshToken)
        {
            // Update access token
            var accessTokenOption = connection.Options.FirstOrDefault(o => o.Key == JiraOAuthWorkTrackingOptionNames.AccessToken);
            if (accessTokenOption != null)
            {
                accessTokenOption.Value = cryptoService.Encrypt(newAccessToken);
            }

            // Update refresh token if provided
            if (!string.IsNullOrEmpty(newRefreshToken))
            {
                var refreshTokenOption = connection.Options.FirstOrDefault(o => o.Key == JiraOAuthWorkTrackingOptionNames.RefreshToken);
                if (refreshTokenOption != null)
                {
                    refreshTokenOption.Value = cryptoService.Encrypt(newRefreshToken);
                }
            }            // Note: In a real implementation, you'd want to save these changes to the database
            // This would require access to the repository or a service that can persist the changes
            return Task.CompletedTask;
        }

        private string GetConnectionOption(WorkTrackingSystemConnection connection, string key)
        {
            return connection.Options.FirstOrDefault(x => x.Key == key)?.Value ?? string.Empty;
        }
    }
}
