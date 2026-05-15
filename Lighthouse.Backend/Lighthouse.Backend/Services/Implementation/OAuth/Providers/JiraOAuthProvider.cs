using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Services.Implementation.OAuth.Providers
{
    public class JiraOAuthProvider : IOAuthProvider
    {
        public const string HttpClientName = "JiraOAuth";

        private const string AtlassianAuthorizeEndpoint = "https://auth.atlassian.com/authorize";
        private const string AtlassianTokenEndpoint = "https://auth.atlassian.com/oauth/token";
        private const string AtlassianAudience = "api.atlassian.com";
        private const string ResponseTypeCode = "code";
        private const string PromptConsent = "consent";

        private static readonly IReadOnlyList<string> AtlassianDefaultScopes =
        [
            "read:jira-work",
            "read:jira-user",
            "read:board-scope:jira-software",
            "read:sprint:jira-software",
            "read:issue:jira-software",
            "offline_access",
        ];

        private static readonly JsonSerializerOptions TokenResponseJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        private readonly HttpClient httpClient;
        private readonly TimeProvider timeProvider;

        public JiraOAuthProvider(HttpClient httpClient, TimeProvider timeProvider)
        {
            this.httpClient = httpClient;
            this.timeProvider = timeProvider;
        }

        public string ProviderKey => AuthenticationMethodKeys.JiraOAuth;

        public IReadOnlyList<string> DefaultScopes => AtlassianDefaultScopes;

        public Uri BuildAuthorizationUrl(OAuthFlowContext context)
        {
            var queryParameters = new Dictionary<string, string?>
            {
                ["audience"] = AtlassianAudience,
                ["client_id"] = context.ClientId,
                ["scope"] = string.Join(' ', context.Scopes),
                ["redirect_uri"] = context.RedirectUri.ToString(),
                ["state"] = context.State,
                ["response_type"] = ResponseTypeCode,
                ["prompt"] = PromptConsent,
            };

            var url = QueryHelpers.AddQueryString(AtlassianAuthorizeEndpoint, queryParameters);
            return new Uri(url);
        }

        public Task<OAuthTokens> ExchangeCodeAsync(string code, OAuthFlowContext context, CancellationToken cancellationToken)
        {
            var formParameters = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = context.ClientId,
                ["client_secret"] = context.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = context.RedirectUri.ToString(),
            };

            return PostTokenRequestAsync(formParameters, cancellationToken);
        }

        public Task<OAuthTokens> RefreshTokenAsync(OAuthRefreshContext context, CancellationToken cancellationToken)
        {
            var formParameters = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = context.RefreshToken,
                ["client_id"] = context.ClientId,
                ["client_secret"] = context.ClientSecret,
            };

            return PostTokenRequestAsync(formParameters, cancellationToken);
        }

        private async Task<OAuthTokens> PostTokenRequestAsync(
            IDictionary<string, string> formParameters,
            CancellationToken cancellationToken)
        {
            using var content = new FormUrlEncodedContent(formParameters);
            using var response = await httpClient.PostAsync(AtlassianTokenEndpoint, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw BuildErrorException((int)response.StatusCode, responseBody);
            }

            var tokenResponse = JsonSerializer.Deserialize<AtlassianTokenResponse>(responseBody, TokenResponseJsonOptions)
                ?? throw new OAuthProviderResponseException(
                    ProviderKey,
                    (int)response.StatusCode,
                    "empty_response",
                    "Atlassian token endpoint returned an empty body.");

            var expiresAt = timeProvider.GetUtcNow().AddSeconds(tokenResponse.ExpiresIn);
            return new OAuthTokens(tokenResponse.AccessToken, tokenResponse.RefreshToken, expiresAt);
        }

        private OAuthProviderResponseException BuildErrorException(int httpStatus, string responseBody)
        {
            var errorCode = "unknown_error";
            var errorDescription = responseBody;

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<AtlassianErrorResponse>(responseBody, TokenResponseJsonOptions);
                    if (errorResponse is not null)
                    {
                        errorCode = errorResponse.Error ?? errorCode;
                        errorDescription = errorResponse.ErrorDescription ?? errorDescription;
                    }
                }
                catch (JsonException)
                {
                    // Non-JSON error body — keep raw body as the description.
                }
            }

            return new OAuthProviderResponseException(ProviderKey, httpStatus, errorCode, errorDescription);
        }

        private sealed record AtlassianTokenResponse(
            [property: JsonPropertyName("access_token")] string AccessToken,
            [property: JsonPropertyName("refresh_token")] string RefreshToken,
            [property: JsonPropertyName("expires_in")] int ExpiresIn,
            [property: JsonPropertyName("token_type")] string TokenType,
            [property: JsonPropertyName("scope")] string Scope);

        private sealed record AtlassianErrorResponse(
            [property: JsonPropertyName("error")] string? Error,
            [property: JsonPropertyName("error_description")] string? ErrorDescription);
    }
}
