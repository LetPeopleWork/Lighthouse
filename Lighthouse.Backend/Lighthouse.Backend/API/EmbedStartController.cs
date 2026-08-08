using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Lighthouse.Backend.API
{
    // ADR-137 hop 1. Outside /api on purpose: an /api challenge becomes a bare 401, which is the
    // blank rectangle D26 exists to prevent.
    [Route("embed")]
    [ApiController]
    [AllowAnonymous]
    public class EmbedStartController(
        IAuthModeResolver authModeResolver,
        ICurrentUserProfileService currentUserProfileService,
        IEmbedSessionTokenService embedSessionTokenService,
        IOptionsMonitor<EmbedConfiguration> embedConfiguration) : ControllerBase
    {
        public const string StartPath = "/embed/start";

        // ADR-137 DQ-1: one class-level code, never prose and never anything about who the viewer is
        // or what the instance holds.
        public const string NoProfileRefusalCode = "no_profile";

        private const int MinimumNonceLength = 22;
        private const int MaximumNonceLength = 128;

        private const string GrantHtml = """
            <!DOCTYPE html>
            <html lang="en"><head><meta charset="utf-8"><title>Lighthouse embed session</title></head>
            <body><h1>You are signed in to Lighthouse</h1>
            <p>You can close this tab and return to the page that opened it.</p></body></html>
            """;

        private const string RefusalHtml = """
            <!DOCTYPE html>
            <html lang="en"><head><meta charset="utf-8"><title>Lighthouse embed session</title></head>
            <body><h1>Lighthouse could not tell who you are</h1>
            <p>The sign-in worked, but this Lighthouse did not recognise it as an account. That is a
            fault on the instance, not something you can fix — report it to whoever runs it, with
            the time you tried. You can close this tab.</p></body></html>
            """;

        [HttpGet("start")]
        [EnableRateLimiting(RateLimitingConfiguration.EmbedSessionPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Start(
            [FromQuery] string? nonce,
            CancellationToken cancellationToken = default)
        {
            // The nonce rides in the query string, so it must not travel onward in a Referer.
            Response.Headers["Referrer-Policy"] = "no-referrer";

            // Before any scheme is looked up: the embed cookie scheme is not registered when
            // authentication is off, and a misconfigured instance has no challenge scheme at all.
            var unavailable = ResolveSurfaceUnavailability();
            if (unavailable is not null)
            {
                return unavailable;
            }

            if (!IsWellFormedNonce(nonce))
            {
                return BadRequest();
            }

            // D64: the ticket names the cookie scheme, the identity names OpenIdConnect. Reading the
            // identity's authentication type instead refuses every genuine session, silently.
            var authentication = await HttpContext.AuthenticateAsync(SmartAuthSchemeSelector.CookieScheme);
            if (!authentication.Succeeded || authentication.Principal is null)
            {
                return ChallengeIdentityProvider(nonce);
            }

            return await ResolveOutcomeAsync(authentication.Principal, nonce, cancellationToken);
        }

        private static bool IsWellFormedNonce([NotNullWhen(true)] string? nonce)
        {
            if (nonce is null || nonce.Length < MinimumNonceLength || nonce.Length > MaximumNonceLength)
            {
                return false;
            }

            return nonce.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
        }

        private IActionResult? ResolveSurfaceUnavailability()
        {
            // An instance that has not opted in has no embed surface, ahead of anything the auth
            // ladder would say about it. Epic #5674.
            if (!embedConfiguration.CurrentValue.Enabled)
            {
                return NotFound();
            }

            var mode = authModeResolver.Resolve().Mode;
            if (mode == AuthMode.Enabled)
            {
                return null;
            }

            // D31: an instance whose authentication is off, or cannot work, has no embed surface at
            // all. Blocked is the one state that admits the surface exists and withholds it.
            return mode == AuthMode.Blocked
                ? StatusCode(StatusCodes.Status403Forbidden)
                : NotFound();
        }

        private ChallengeResult ChallengeIdentityProvider(string nonce)
        {
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = $"{StartPath}?nonce={Uri.EscapeDataString(nonce)}",
                },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        private async Task<IActionResult> ResolveOutcomeAsync(
            ClaimsPrincipal principal,
            string nonce,
            CancellationToken cancellationToken)
        {
            // The opposite answer to D57's: this seam creates the profile, so a first-time viewer
            // appears in the administrator's user list.
            var profile = await currentUserProfileService.GetOrCreateFromPrincipalAsync(principal, cancellationToken);

            // A sign-in that worked and still names nobody is the only refusal left here: what a
            // viewer may read is RBAC's answer on every request, not this hop's (D49/D60, amended
            // 2026-08-06). D31's ladder above is a different sentence and is untouched.
            if (profile is null)
            {
                await embedSessionTokenService.RecordHandshakeRefusalAsync(
                    null, nonce, NoProfileRefusalCode, cancellationToken);

                return TerminalPage(RefusalHtml);
            }

            await embedSessionTokenService.RecordHandshakeGrantAsync(profile.Subject, nonce, cancellationToken);
            return TerminalPage(GrantHtml);
        }

        // D61: the orphaned tab ends on a page a person can read, not on a redirect or an error.
        private static ContentResult TerminalPage(string document)
        {
            return new ContentResult
            {
                StatusCode = StatusCodes.Status200OK,
                ContentType = "text/html; charset=utf-8",
                Content = document,
            };
        }
    }
}
