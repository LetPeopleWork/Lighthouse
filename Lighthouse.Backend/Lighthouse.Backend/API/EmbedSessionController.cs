using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;

namespace Lighthouse.Backend.API
{
    // ADR-129: no new authentication scheme — X-Api-Key already routes here through
    // SmartAuthSchemeSelector, and minting a session for your own identity needs no extra privilege.
    [Route("api/v1/embed")]
    [Route("api/latest/embed")]
    [ApiController]
    [Authorize]
    public class EmbedSessionController(
        IEmbedSessionTokenService embedSessionTokenService,
        IAuthModeResolver authModeResolver) : ControllerBase
    {
        [HttpPost("session-token")]
        [EnableRateLimiting(RateLimitingConfiguration.EmbedSessionPolicy)]
        [ProducesResponseType<EmbedSessionTokenResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> MintSessionToken(CancellationToken cancellationToken = default)
        {
            if (!IsEmbedSurfaceAvailable())
            {
                return NotFound();
            }

            if (!TryGetApiKeyId(out var apiKeyId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(User.FindFirst(ApiKeyPrincipalFactory.SubjectClaimType)?.Value))
            {
                return Conflict(new EmbedSessionRefusal
                {
                    Reason = "api_key_owner_unlinked",
                    Message = "The API key has no linked owner, so an embed session established from it "
                        + "would resolve no permissions. Reassign the key to a user profile and retry.",
                });
            }

            var mint = await embedSessionTokenService.MintAsync(apiKeyId, cancellationToken);

            return Ok(new EmbedSessionTokenResponse
            {
                Token = mint.Token,
                ExpiresAt = mint.ExpiresAt,
                EmbedUrl = BuildEmbedUrl(mint.Token),
            });
        }

        [HttpPost("session-token/revoke-all")]
        [EnableRateLimiting(RateLimitingConfiguration.EmbedSessionPolicy)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeAllSessionTokens(CancellationToken cancellationToken = default)
        {
            if (!IsEmbedSurfaceAvailable())
            {
                return NotFound();
            }

            if (!TryGetApiKeyId(out var apiKeyId))
            {
                return Unauthorized();
            }

            await embedSessionTokenService.RevokeAllAsync(apiKeyId, cancellationToken);
            return NoContent();
        }

        private bool IsEmbedSurfaceAvailable()
        {
            return authModeResolver.Resolve().Mode == AuthMode.Enabled;
        }

        private bool TryGetApiKeyId(out int apiKeyId)
        {
            apiKeyId = 0;
            var claimValue = User.FindFirst(ApiKeyPrincipalFactory.ApiKeyIdClaimType)?.Value;

            return !string.IsNullOrWhiteSpace(claimValue)
                && int.TryParse(claimValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out apiKeyId);
        }

        private string BuildEmbedUrl(string token)
        {
            return $"{Request.Scheme}://{Request.Host}{EmbedEntryController.EntryPath}?token={Uri.EscapeDataString(token)}";
        }
    }
}
