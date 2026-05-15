using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            [FromQuery] string code,
            [FromQuery] string state,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await oauthService.CompleteAsync(code, state, cancellationToken);
                return Redirect($"/settings/connections/{result.ConnectionId}?oauth=success");
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
                return Redirect($"/settings/connections?oauth=error&reason={Uri.EscapeDataString(ex.IdpErrorCode)}");
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

    public record OAuthConnectRequest(int ConnectionId);

    public record OAuthDisconnectRequest(int ConnectionId);
}
