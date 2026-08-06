using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lighthouse.Backend.API
{
    // ADR-137 hop 2. A second controller on the embed prefix so [AllowAnonymous] never lands on the
    // minting one: the resolver holds no credential and is given exactly one read verb.
    [Route("api/v1/embed")]
    [Route("api/latest/embed")]
    [ApiController]
    [AllowAnonymous]
    public class EmbedHandshakeController(
        IAuthModeResolver authModeResolver,
        IEmbedSessionTokenService embedSessionTokenService) : ControllerBase
    {
        [HttpGet("handshake/{nonce}")]
        [EnableRateLimiting(RateLimitingConfiguration.EmbedSessionPolicy)]
        [ProducesResponseType<EmbedHandshakeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Poll(
            [FromRoute] string? nonce,
            CancellationToken cancellationToken = default)
        {
            // D31, the same two answers hop 1 gives: the embed surface does not exist on an instance
            // whose authentication is off or cannot work, and Blocked withholds a surface it admits to.
            var mode = authModeResolver.Resolve().Mode;
            if (mode != AuthMode.Enabled)
            {
                return mode == AuthMode.Blocked ? StatusCode(StatusCodes.Status403Forbidden) : NotFound();
            }

            var outcome = await embedSessionTokenService.ConsumeHandshakeAsync(nonce, cancellationToken);

            return Ok(new EmbedHandshakeResponse
            {
                Token = outcome.Token,
                ExpiresAt = outcome.ExpiresAt,
                RefusalCode = outcome.RefusalCode,
            });
        }
    }
}
