using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class CliTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string BearerPrefix = "Bearer ";
        private readonly ICliAuthSessionService cliAuthSessionService;

        public CliTokenAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ICliAuthSessionService cliAuthSessionService)
            : base(options, logger, encoder)
        {
            this.cliAuthSessionService = cliAuthSessionService;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var token = authHeader[BearerPrefix.Length..].Trim();
            if (string.IsNullOrEmpty(token))
            {
                return Task.FromResult(AuthenticateResult.Fail("Empty bearer token."));
            }

            if (!cliAuthSessionService.ValidateToken(token, out var userName))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid or expired CLI token."));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userName ?? "cli-user"),
                new Claim("auth_method", "cli-token"),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
    }
}
