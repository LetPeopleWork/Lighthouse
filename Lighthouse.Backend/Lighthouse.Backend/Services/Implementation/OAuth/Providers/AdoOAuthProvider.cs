using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Services.Implementation.OAuth.Providers
{
    public class AdoOAuthProvider : IOAuthProvider
    {
        public const string HttpClientName = "AdoOAuth";

        private const string MicrosoftEndpointTemplate = "https://login.microsoftonline.com/{0}/oauth2/v2.0/{1}";
        private const string CommonTenantSegment = "common";
        private const string ResponseTypeCode = "code";
        private const string ResponseModeQuery = "query";
        private const string PromptConsent = "consent";

        private const string AzureDevOpsResourceId = "499b84ac-1321-427f-aa17-267ca6975798";

        private static readonly IReadOnlyList<string> AzureDevOpsDefaultScopes =
        [
            AzureDevOpsResourceId + "/vso.work_write",
            "offline_access",
        ];

        private static readonly JsonSerializerOptions TokenResponseJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        private readonly HttpClient httpClient;
        private readonly TimeProvider timeProvider;
        private readonly ILogger<AdoOAuthProvider> logger;

        public AdoOAuthProvider(HttpClient httpClient, TimeProvider timeProvider, ILogger<AdoOAuthProvider> logger)
        {
            this.httpClient = httpClient;
            this.timeProvider = timeProvider;
            this.logger = logger;
        }

        public string ProviderKey => AuthenticationMethodKeys.AzureDevOpsOAuth;

        public IReadOnlyList<string> DefaultScopes => AzureDevOpsDefaultScopes;

        public Uri BuildAuthorizationUrl(OAuthFlowContext context)
        {
            var queryParameters = new Dictionary<string, string?>
            {
                ["client_id"] = context.ClientId,
                ["response_type"] = ResponseTypeCode,
                ["redirect_uri"] = context.RedirectUri.ToString(),
                ["scope"] = string.Join(' ', context.Scopes),
                ["state"] = context.State,
                ["response_mode"] = ResponseModeQuery,
                ["prompt"] = PromptConsent,
            };

            var url = QueryHelpers.AddQueryString(BuildAuthorizeEndpoint(context.TenantId), queryParameters);
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
                ["scope"] = string.Join(' ', context.Scopes),
            };

            return PostTokenRequestAsync(BuildTokenEndpoint(context.TenantId), formParameters, cancellationToken);
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

            return PostTokenRequestAsync(BuildTokenEndpoint(context.TenantId), formParameters, cancellationToken);
        }

        private static string BuildAuthorizeEndpoint(string? tenantId)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, MicrosoftEndpointTemplate, ResolveTenantSegment(tenantId), "authorize");
        }

        private static string BuildTokenEndpoint(string? tenantId)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, MicrosoftEndpointTemplate, ResolveTenantSegment(tenantId), "token");
        }

        private static string ResolveTenantSegment(string? tenantId)
        {
            return string.IsNullOrWhiteSpace(tenantId) ? CommonTenantSegment : tenantId;
        }

        private async Task<OAuthTokens> PostTokenRequestAsync(
            string tokenEndpoint,
            IDictionary<string, string> formParameters,
            CancellationToken cancellationToken)
        {
            using var content = new FormUrlEncodedContent(formParameters);
            using var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw BuildErrorException((int)response.StatusCode, responseBody);
            }

            var tokenResponse = JsonSerializer.Deserialize<MicrosoftTokenResponse>(responseBody, TokenResponseJsonOptions)
                ?? throw new OAuthProviderResponseException(
                    ProviderKey,
                    (int)response.StatusCode,
                    "empty_response",
                    "Microsoft identity token endpoint returned an empty body.");

            logger.LogInformation(
                "ado.oauth.token granted scope from Microsoft identity platform: {GrantedScope}",
                tokenResponse.Scope);

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
                    var errorResponse = JsonSerializer.Deserialize<MicrosoftErrorResponse>(responseBody, TokenResponseJsonOptions);
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

        private sealed record MicrosoftTokenResponse(
            [property: JsonPropertyName("access_token")] string AccessToken,
            [property: JsonPropertyName("refresh_token")] string RefreshToken,
            [property: JsonPropertyName("expires_in")] int ExpiresIn,
            [property: JsonPropertyName("token_type")] string TokenType,
            [property: JsonPropertyName("scope")] string Scope);

        private sealed record MicrosoftErrorResponse(
            [property: JsonPropertyName("error")] string? Error,
            [property: JsonPropertyName("error_description")] string? ErrorDescription);
    }
}
