using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Modules.Access.Application;
using Lighthouse.Backend.Modules.Access.Contracts;
using Lighthouse.Backend.Platform.Host;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.API
{
    // Second hop of the embed handshake. Its own controller on the embed prefix so [AllowAnonymous] covers nothing but
    // this one read verb: the poller holds no credential.
    [Route("api/v1/embed")]
    [Route("api/latest/embed")]
    [ApiController]
    [AllowAnonymous]
    // Its answer carries the embed session token, and no compressed answer may carry a secret.
    [NeverCompressResponse]
    public class EmbedHandshakeController(
        IAuthModeResolver authModeResolver,
        IEmbedSessionTokenService embedSessionTokenService,
        IOptionsMonitor<EmbedConfiguration> embedConfiguration) : ControllerBase
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
            // An instance that has not opted in has no embed surface at all. Epic #5674.
            if (!embedConfiguration.CurrentValue.Enabled)
            {
                return NotFound();
            }

            // The same two answers the first hop gives: the embed surface does not exist on an instance
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

                // Only on a grant, so the unresolved, unknown, consumed and refused answers stay identical.
                SessionLifetimeSeconds = outcome.Token is null ? null : ResolveSessionLifetimeSeconds(),
            });
        }

        private int ResolveSessionLifetimeSeconds()
        {
            return embedConfiguration.CurrentValue.ResolveSessionLifetimeMinutes() * 60;
        }
    }
}
