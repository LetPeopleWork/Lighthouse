using Lighthouse.Backend.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Linq;

namespace Lighthouse.Backend.Services.Implementation
{
    public class OAuthService : IOAuthService
    {
        private const string JIRA_AUTH_URL = "https://auth.atlassian.com/authorize";
        private const string JIRA_TOKEN_URL = "https://auth.atlassian.com/oauth/token";
        private const string DEFAULT_SCOPE = "read:jira-work read:jira-user offline_access";
        
        private readonly HttpClient httpClient;
        private readonly ILogger<OAuthService> logger;

        public OAuthService(HttpClient httpClient, ILogger<OAuthService> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public string GetJiraAuthorizationUrl(string clientId, string redirectUri, string state)
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["audience"] = "api.atlassian.com";
            queryParams["client_id"] = clientId;
            queryParams["scope"] = DEFAULT_SCOPE;
            queryParams["redirect_uri"] = redirectUri;
            queryParams["state"] = state;
            queryParams["response_type"] = "code";
            queryParams["prompt"] = "consent";

            return $"{JIRA_AUTH_URL}?{queryParams}";
        }

        public async Task<OAuthTokenResponse> ExchangeCodeForTokens(string clientId, string clientSecret, string code, string redirectUri)
        {
            logger.LogInformation("Exchanging authorization code for access token");

            var requestBody = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            };

            return await MakeTokenRequest(requestBody);
        }

        public async Task<OAuthTokenResponse> RefreshAccessToken(string clientId, string clientSecret, string refreshToken)
        {
            logger.LogInformation("Refreshing access token");

            var requestBody = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken
            };

            return await MakeTokenRequest(requestBody);
        }        public async Task<bool> ValidateAccessToken(string accessToken, string jiraUrl)
        {
            try
            {
                logger.LogInformation("Validating OAuth access token for Jira URL: {JiraUrl}", jiraUrl);
                
                // First, let's get the accessible resources to see what Jira instances are available
                var accessibleResources = await GetAccessibleResources(accessToken);
                if (accessibleResources != null)
                {
                    logger.LogInformation("Accessible Jira resources: {Resources}", 
                        string.Join(", ", accessibleResources.Select(r => $"{r.Name} ({r.Url})")));
                    
                    // Try to find a matching resource
                    var matchingResource = accessibleResources.FirstOrDefault(r => 
                        r.Url.Equals(jiraUrl, StringComparison.OrdinalIgnoreCase) ||
                        jiraUrl.Contains(r.Id) ||
                        r.Url.Contains(new Uri(jiraUrl).Host));
                    
                    if (matchingResource != null)
                    {
                        logger.LogInformation("Found matching resource: {ResourceName} at {ResourceUrl}", 
                            matchingResource.Name, matchingResource.Url);
                        jiraUrl = matchingResource.Url; // Use the canonical URL
                    }
                    else
                    {
                        logger.LogWarning("No matching resource found for URL: {JiraUrl}. Available resources: {Resources}", 
                            jiraUrl, string.Join(", ", accessibleResources.Select(r => r.Url)));
                    }
                }
                
                var apiUrl = $"{jiraUrl.TrimEnd('/')}/rest/api/3/myself";
                logger.LogDebug("Making validation request to: {ApiUrl}", apiUrl);
                
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("Accept", "application/json");

                var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                logger.LogInformation("Validation response: Status={StatusCode}, Body={ResponseBody}", 
                    response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("OAuth token validation successful");
                    return true;
                }
                else
                {
                    logger.LogWarning("OAuth token validation failed: {StatusCode} - {ResponseBody}", 
                        response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate OAuth access token");
                return false;
            }
        }

        private async Task<OAuthTokenResponse> MakeTokenRequest(Dictionary<string, string> requestBody)
        {
            try
            {
                var content = new FormUrlEncodedContent(requestBody);
                var response = await httpClient.PostAsync(JIRA_TOKEN_URL, content);

                var responseBody = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                var tokenData = JsonSerializer.Deserialize<JsonElement>(responseBody);

                return new OAuthTokenResponse
                {
                    AccessToken = tokenData.GetProperty("access_token").GetString() ?? string.Empty,
                    RefreshToken = tokenData.TryGetProperty("refresh_token", out var refreshProp) ? refreshProp.GetString() ?? string.Empty : string.Empty,
                    ExpiresIn = tokenData.TryGetProperty("expires_in", out var expiresProp) ? expiresProp.GetInt32() : 3600,
                    TokenType = tokenData.TryGetProperty("token_type", out var typeProp) ? typeProp.GetString() ?? "Bearer" : "Bearer"
                };            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to exchange OAuth tokens");
                throw new InvalidOperationException("OAuth token exchange failed", ex);
            }        }

        public async Task<List<AccessibleResource>?> GetAccessibleResources(string accessToken)
        {
            try
            {
                logger.LogDebug("Getting accessible resources with access token");
                
                const string resourcesUrl = "https://api.atlassian.com/oauth/token/accessible-resources";
                
                using var request = new HttpRequestMessage(HttpMethod.Get, resourcesUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("Accept", "application/json");

                var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                logger.LogDebug("Accessible resources response: Status={StatusCode}, Body={ResponseBody}", 
                    response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    var resources = JsonSerializer.Deserialize<List<AccessibleResource>>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    return resources;
                }
                else
                {
                    logger.LogWarning("Failed to get accessible resources: {StatusCode} - {ResponseBody}", 
                        response.StatusCode, responseBody);
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get accessible resources");
                return null;
            }
        }
    }
}
