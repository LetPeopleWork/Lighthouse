using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
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
            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValues))
            {
                return AuthenticateResult.NoResult();
            }

            var apiKey = apiKeyValues.ToString();
            if (string.IsNullOrEmpty(apiKey))
            {
                return AuthenticateResult.Fail("Empty API key.");
            }

            var isValid = await apiKeyService.ValidateApiKeyAsync(apiKey);
            if (!isValid)
            {
                return AuthenticateResult.Fail("Invalid or unknown API key.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "api-key-user"),
                new Claim("auth_method", "api-key"),
            };
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
