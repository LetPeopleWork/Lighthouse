using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Lighthouse.Backend.API
{
    // Outside /api on purpose: an /api challenge becomes a bare 401, which is the blank rectangle
    // D26 exists to prevent. DisabledAuthenticationHandler is deliberately NOT reused here — it
    // would hand every anonymous caller a session (DESIGN reuse analysis, row 12).
    [Route("embed")]
    [ApiController]
    [AllowAnonymous]
    public class EmbedEntryController(
        IEmbedSessionTokenService embedSessionTokenService,
        IUserProfileLookup userProfileLookup,
        IAuthModeResolver authModeResolver,
        IOptionsMonitor<EmbedConfiguration> embedConfiguration,
        ILogger<EmbedEntryController> logger) : ControllerBase
    {
        public const string EntryPath = "/embed/enter";
        public const string DefaultReturnPath = "/";

        private const string RefusalHtml = """
            <!DOCTYPE html>
            <html lang="en"><head><meta charset="utf-8"><title>Lighthouse embed session</title></head>
            <body><h1>This Lighthouse embed link is no longer valid</h1>
            <p>Embed links are single use and expire within a minute of being issued. Reload the page
            that framed this view to request a fresh one.</p></body></html>
            """;

        [HttpGet("enter")]
        [EnableRateLimiting(RateLimitingConfiguration.EmbedSessionPolicy)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Enter(
            [FromQuery] string? token,
            [FromQuery] string? returnPath,
            CancellationToken cancellationToken = default)
        {
            // The token rides in the query string (D39), so it must not travel onward in a Referer.
            Response.Headers["Referrer-Policy"] = "no-referrer";

            // An instance that has not opted in has no embed surface, so a token minted before the
            // switch was turned off cannot be redeemed either. Epic #5674.
            if (!embedConfiguration.CurrentValue.Enabled)
            {
                return NotFound();
            }

            if (authModeResolver.Resolve().Mode != AuthMode.Enabled)
            {
                return NotFound();
            }

            var redemption = await embedSessionTokenService.RedeemAsync(token, cancellationToken);
            if (!redemption.Succeeded)
            {
                return Refuse();
            }

            var principal = await ResolveViewerPrincipalAsync(redemption.Subject, cancellationToken);
            if (principal is null)
            {
                return Refuse();
            }

            await HttpContext.SignInAsync(
                SmartAuthSchemeSelector.EmbedCookieScheme,
                principal,
                new AuthenticationProperties { IsPersistent = false });

            // The cookie is set, so the token leaves the URL immediately: history and access logs
            // hold it exactly once, already spent (D39).
            return Redirect(ResolveReturnPath(returnPath));
        }

        // ADR-137 D48: a redeemed row names the viewer who signed in, and nobody else.
        private async Task<ClaimsPrincipal?> ResolveViewerPrincipalAsync(
            string? subject,
            CancellationToken cancellationToken)
        {
            // F7, defence in depth, retained deliberately: RedeemAsync already refuses a row that
            // names nobody, and a blank subject here would sign the caller in as no-one.
            if (string.IsNullOrWhiteSpace(subject))
            {
                return null;
            }

            // D57: this lookup cannot create, so a viewer deleted between the handshake and the
            // redemption is refused rather than resurrected.
            var profile = await userProfileLookup.FindBySubjectAsync(subject, cancellationToken);
            if (profile is null)
            {
                logger.LogWarning("Embed session refused: the viewer named by this token no longer has a profile");
                return null;
            }

            return ApiKeyPrincipalFactory.Create(
                profile.Subject, profile.DisplayName, SmartAuthSchemeSelector.EmbedCookieScheme);
        }

        private string ResolveReturnPath(string? returnPath)
        {
            if (string.IsNullOrWhiteSpace(returnPath) || !Url.IsLocalUrl(returnPath))
            {
                return DefaultReturnPath;
            }

            return returnPath;
        }

        private static ContentResult Refuse()
        {
            return new ContentResult
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                ContentType = "text/html; charset=utf-8",
                Content = RefusalHtml,
            };
        }
    }
}
