using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/auth/cli")]
    [Route("api/latest/auth/cli")]
    [ApiController]
    [AllowAnonymous]
    public class CliAuthController : ControllerBase
    {
        private const string ContentType = "text/html";
        private readonly ICliAuthSessionService cliAuthSessionService;
        private readonly IAuthModeResolver authModeResolver;

        public CliAuthController(
            ICliAuthSessionService cliAuthSessionService,
            IAuthModeResolver authModeResolver)
        {
            this.cliAuthSessionService = cliAuthSessionService;
            this.authModeResolver = authModeResolver;
        }

        /// <summary>
        /// Start a CLI device-authorization session.
        /// Returns a sessionId and verification URL the CLI should open in the browser.
        /// </summary>
        [HttpPost("session")]
        [ProducesResponseType<CliAuthSessionInfo>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult StartSession()
        {
            var authStatus = authModeResolver.Resolve();
            if (authStatus.Mode is not AuthMode.Enabled)
            {
                return NotFound();
            }

            var (sessionId, expiresAt) = cliAuthSessionService.StartSession();

            var scheme = Request.Scheme;
            var host = Request.Host.ToUriComponent();
            var verificationUrl = $"{scheme}://{host}/api/v1/auth/cli/verify/{sessionId}";

            var info = new CliAuthSessionInfo
            {
                SessionId = sessionId,
                VerificationUrl = verificationUrl,
                ExpiresAt = expiresAt,
            };

            return Ok(info);
        }

        /// <summary>
        /// Browser-facing verification page. The user authenticates via OIDC (if not already
        /// authenticated) and then approves or denies CLI access.
        /// This endpoint handles authentication manually because [AllowAnonymous] is set at
        /// the class level — OIDC challenge is issued explicitly when the user is not logged in.
        /// </summary>
        [HttpGet("verify/{sessionId}")]
        public async Task<IActionResult> VerifyCliSession(string sessionId)
        {
            var authStatus = authModeResolver.Resolve();
            if (authStatus.Mode is not AuthMode.Enabled)
            {
                return NotFound();
            }

            var pollResult = cliAuthSessionService.PollSession(sessionId);
            if (pollResult.Status == "expired")
            {
                return Content(ExpiredPageHtml, ContentType);
            }

            // Check if the user is currently authenticated via cookie.
            var authResult = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authResult.Succeeded)
            {
                // Redirect to OIDC login. After login, the OIDC callback redirects back here.
                var returnUrl = $"/api/v1/auth/cli/verify/{HtmlEncoder.Default.Encode(sessionId)}";
                return Challenge(
                    new AuthenticationProperties { RedirectUri = returnUrl },
                    OpenIdConnectDefaults.AuthenticationScheme);
            }

            var userName = authResult.Principal?.FindFirst("name")?.Value
                ?? authResult.Principal?.Identity?.Name
                ?? "Unknown";

            return Content(GetVerifyPageHtml(sessionId, userName), ContentType);
        }

        /// <summary>
        /// Approves an open CLI session for the currently authenticated user.
        /// Issues a bearer token that the CLI can poll for.
        /// </summary>
        [HttpPost("approve/{sessionId}")]
        public async Task<IActionResult> ApproveCliSession(string sessionId)
        {
            var authStatus = authModeResolver.Resolve();
            if (authStatus.Mode is not AuthMode.Enabled)
            {
                return NotFound();
            }

            var authResult = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authResult.Succeeded)
            {
                return Unauthorized();
            }

            var userName = authResult.Principal?.FindFirst("name")?.Value
                ?? authResult.Principal?.FindFirst(ClaimTypes.Email)?.Value
                ?? authResult.Principal?.Identity?.Name
                ?? "Unknown";

            var approved = cliAuthSessionService.TryApproveSession(sessionId, userName);
            if (!approved)
            {
                return Content(ExpiredPageHtml, ContentType);
            }

            return Content(GetApprovedPageHtml(userName), ContentType);
        }

        /// <summary>
        /// Polls the status of a CLI auth session.
        /// Returns "pending", "approved" (with token), or "expired".
        /// </summary>
        [HttpGet("poll/{sessionId}")]
        [ProducesResponseType<CliAuthSessionPollResponse>(StatusCodes.Status200OK)]
        public IActionResult PollSession(string sessionId)
        {
            var result = cliAuthSessionService.PollSession(sessionId);
            return Ok(result);
        }

        /// <summary>
        /// Revokes a previously issued CLI bearer token.
        /// </summary>
        [HttpPost("revoke")]
        public IActionResult RevokeToken([FromBody] CliRevokeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest("Token is required.");
            }

            cliAuthSessionService.RevokeToken(request.Token);
            return Ok();
        }

        private static string GetVerifyPageHtml(string sessionId, string userName)
        {
            var encodedSessionId = HtmlEncoder.Default.Encode(sessionId);
            var encodedUserName = HtmlEncoder.Default.Encode(userName);

            return $$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="UTF-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1.0">
                  <title>Lighthouse CLI Authorization</title>
                  <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                           max-width: 480px; margin: 80px auto; padding: 24px;
                           color: #1a1a1a; background: #f5f5f5; }
                    .card { background: white; border-radius: 8px; padding: 32px;
                             box-shadow: 0 2px 8px rgba(0,0,0,.1); }
                    h1 { font-size: 1.4rem; margin: 0 0 8px; }
                    .user { color: #555; margin-bottom: 24px; font-size: 0.9rem; }
                    .actions { display: flex; gap: 12px; margin-top: 24px; }
                    .btn { flex: 1; padding: 12px; border: none; border-radius: 6px;
                            font-size: 1rem; cursor: pointer; font-weight: 500; }
                    .btn-approve { background: #2563eb; color: white; }
                    .btn-approve:hover { background: #1d4ed8; }
                    .btn-deny { background: #e5e7eb; color: #374151; }
                    .btn-deny:hover { background: #d1d5db; }
                  </style>
                </head>
                <body>
                  <div class="card">
                    <h1>Lighthouse CLI Authorization</h1>
                    <p class="user">Signed in as <strong>{{encodedUserName}}</strong></p>
                    <p>The Lighthouse CLI is requesting access to this Lighthouse instance.</p>
                    <div class="actions">
                      <form method="post" action="/api/v1/auth/cli/approve/{{encodedSessionId}}" style="flex:1">
                        <button type="submit" class="btn btn-approve">Authorize CLI Access</button>
                      </form>
                    </div>
                  </div>
                </body>
                </html>
                """;
        }

        private static string GetApprovedPageHtml(string userName)
        {
            var encodedUserName = HtmlEncoder.Default.Encode(userName);
            return $$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="UTF-8">
                  <title>CLI Authorized</title>
                  <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                           max-width: 480px; margin: 80px auto; padding: 24px; background: #f5f5f5; }
                    .card { background: white; border-radius: 8px; padding: 32px;
                             box-shadow: 0 2px 8px rgba(0,0,0,.1); }
                    .check { font-size: 2rem; margin-bottom: 16px; }
                  </style>
                </head>
                <body>
                  <div class="card">
                    <div class="check">✅</div>
                    <h1>Authorization successful</h1>
                    <p>The CLI has been granted access as <strong>{{encodedUserName}}</strong>.</p>
                    <p>You can close this window and return to your terminal.</p>
                  </div>
                </body>
                </html>
                """;
        }

        private static string ExpiredPageHtml => """
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="UTF-8">
                  <title>Session Expired</title>
                  <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                           max-width: 480px; margin: 80px auto; padding: 24px; background: #f5f5f5; }
                    .card { background: white; border-radius: 8px; padding: 32px;
                            box-shadow: 0 2px 8px rgba(0,0,0,.1); }
                  </style>
                </head>
                <body>
                  <div class="card">
                    <h1>Session expired</h1>
                    <p>The CLI authorization session has expired or was already used.</p>
                    <p>Run <code>lh connect</code> in your terminal to start a new session.</p>
                  </div>
                </body>
                </html>
                """;
    }

    public record CliRevokeRequest
    {
        public string Token { get; init; } = string.Empty;
    }
}