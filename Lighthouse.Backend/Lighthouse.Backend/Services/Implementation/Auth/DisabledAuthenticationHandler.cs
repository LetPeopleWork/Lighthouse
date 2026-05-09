using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public sealed class DisabledAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "LighthouseDisabledAuth";
        private const string DisabledSubject = "lighthouse|auth-disabled";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
            [
                new Claim("sub", DisabledSubject),
                new Claim(ClaimTypes.NameIdentifier, DisabledSubject),
                new Claim("name", "Authentication Disabled")
            ], SchemeName);

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
