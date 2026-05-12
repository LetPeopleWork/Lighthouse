using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "TestScheme";
        public const string SubjectHeader = "X-Test-User-Sub";
        public const string RolesHeader = "X-Test-User-Roles";
        public const string DisplayNameHeader = "X-Test-User-DisplayName";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.Request.Headers.TryGetValue(SubjectHeader, out var subjectHeader))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var subject = subjectHeader.ToString();
            if (string.IsNullOrWhiteSpace(subject))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var displayName = Context.Request.Headers.TryGetValue(DisplayNameHeader, out var nameHeader)
                ? nameHeader.ToString()
                : subject;

            var claims = new List<Claim>
            {
                new("sub", subject),
                new("name", displayName),
            };

            if (Context.Request.Headers.TryGetValue(RolesHeader, out var rolesHeader))
            {
                foreach (var roleGrant in rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    claims.Add(new Claim("rbac_grant", roleGrant));
                }
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    }
}
