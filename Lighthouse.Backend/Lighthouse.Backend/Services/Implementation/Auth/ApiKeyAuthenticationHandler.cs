using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string ApiKeyIdClaimType = "api_key_id";
        private const string ApiKeyHeaderName = "X-Api-Key";
        private readonly IApiKeyService apiKeyService;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IApiKeyService apiKeyService)
            : base(options, logger, encoder)
        {
            this.apiKeyService = apiKeyService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var correlationId = Request.HttpContext.TraceIdentifier;

            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValues))
            {
                return AuthenticateResult.NoResult();
            }

            var apiKey = apiKeyValues.ToString();
            if (string.IsNullOrEmpty(apiKey))
            {
                Logger.LogWarning("API key header present but empty. CorrelationId={CorrelationId}", correlationId);
                return AuthenticateResult.Fail("Empty API key.");
            }

            var validationResult = await apiKeyService.ValidateApiKeyWithOwnerAsync(apiKey);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("API key authentication failed: invalid or unknown key. CorrelationId={CorrelationId}", correlationId);
                return AuthenticateResult.Fail("Invalid or unknown API key.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "api-key-user"),
                new Claim("auth_method", "api-key"),
            };

            if (validationResult.ApiKeyId.HasValue)
            {
                claims.Add(new Claim(
                    ApiKeyIdClaimType,
                    validationResult.ApiKeyId.Value.ToString(CultureInfo.InvariantCulture)));
            }

            if (validationResult.OwnerResolutionState == ApiKeyOwnerResolutionState.Resolved
                && !string.IsNullOrWhiteSpace(validationResult.OwnerSubject))
            {
                claims.Add(new Claim("sub", validationResult.OwnerSubject));

                if (!string.IsNullOrWhiteSpace(validationResult.OwnerDisplayName))
                {
                    claims.Add(new Claim("name", validationResult.OwnerDisplayName));
                }

                Logger.LogDebug(
                    "API key {KeyId} authenticated with resolved owner. CorrelationId={CorrelationId}",
                    validationResult.ApiKeyId,
                    correlationId);
            }
            else
            {
                Logger.LogWarning(
                    "API key {KeyId} authenticated but owner is unlinked. No stable-subject claim will be emitted. CorrelationId={CorrelationId}",
                    validationResult.ApiKeyId,
                    correlationId);
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
    }
}
