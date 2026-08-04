using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string ApiKeyIdClaimType = ApiKeyPrincipalFactory.ApiKeyIdClaimType;
        public const string ApiKeyHeaderName = "X-Api-Key";
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

            if (validationResult.OwnerResolutionState == ApiKeyOwnerResolutionState.Resolved
                && !string.IsNullOrWhiteSpace(validationResult.OwnerSubject))
            {
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

            var principal = ApiKeyPrincipalFactory.Create(validationResult, Scheme.Name);
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
