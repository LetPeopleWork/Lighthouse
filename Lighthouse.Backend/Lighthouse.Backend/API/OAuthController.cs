using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API
{
    [Route("api/oauth")]
    [ApiController]
    public class OAuthController(IOAuthService oauthService, ILogger<OAuthController> logger) : ControllerBase
    {
        private readonly IOAuthService oauthService = oauthService ?? throw new ArgumentNullException(nameof(oauthService));
        private readonly ILogger<OAuthController> logger = logger ?? throw new ArgumentNullException(nameof(logger));

        [HttpPost("{providerKey}/connect")]
        [Authorize]
        [LicenseGuard(RequirePremium = true)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<IActionResult> Connect(string providerKey, [FromBody] OAuthConnectRequest request, CancellationToken cancellationToken)
        {
            var authorizationUrl = await oauthService.InitiateAsync(request.ConnectionId, cancellationToken);
            return Ok(new { authorizationUrl = authorizationUrl.ToString() });
        }

        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback(
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(code))
            {
                if (!string.IsNullOrEmpty(error))
                {
                    return Redirect($"/oauth/popup-complete?status=error&reason={Uri.EscapeDataString(error)}");
                }

                return BadRequest(new { error = "missing_code", message = "OAuth callback received without a code or error parameter." });
            }

            try
            {
                var result = await oauthService.CompleteAsync(code, state ?? string.Empty, cancellationToken);
                return Redirect($"/oauth/popup-complete?status=success&connectionId={result.ConnectionId}");
            }
            catch (OAuthStateTokenInvalidException ex)
            {
                logger.LogWarning(ex, "oauth.callback.invalid_state");
                return BadRequest(new { error = "invalid state", message = "The OAuth state token is invalid or has expired." });
            }
            catch (OAuthStateTokenExpiredException ex)
            {
                logger.LogWarning(ex, "oauth.callback.invalid_state");
                return BadRequest(new { error = "invalid state", message = "The OAuth state token is invalid or has expired." });
            }
            catch (OAuthProviderResponseException ex)
            {
                logger.LogWarning(ex, "oauth.flow.failed {ProviderKey} {IdpErrorCode}", ex.ProviderKey, ex.IdpErrorCode);
                return Redirect($"/oauth/popup-complete?status=error&reason={Uri.EscapeDataString(ex.IdpErrorCode)}");
            }
        }

        [HttpPost("{providerKey}/disconnect")]
        [Authorize]
        [LicenseGuard(RequirePremium = true)]
        [RbacGuard(RbacGuardRequirement.SystemAdmin)]
        public async Task<IActionResult> Disconnect(string providerKey, [FromBody] OAuthDisconnectRequest request, CancellationToken cancellationToken)
        {
            await oauthService.DisconnectAsync(request.ConnectionId, cancellationToken);
            return NoContent();
        }
    }

    public record OAuthConnectRequest([property: JsonRequired] int ConnectionId);

    public record OAuthDisconnectRequest([property: JsonRequired] int ConnectionId);
}
