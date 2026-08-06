using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.API
{
    // ADR-137 hop 2. Its own controller on the embed prefix so [AllowAnonymous] covers nothing but
    // this one read verb: the poller holds no credential.
    [Route("api/v1/embed")]
    [Route("api/latest/embed")]
    [ApiController]
    [AllowAnonymous]
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

                // Only on a grant: D45 keeps unresolved, unknown, consumed and refused identical.
                SessionLifetimeSeconds = outcome.Token is null ? null : ResolveSessionLifetimeSeconds(),
            });
        }

        private int ResolveSessionLifetimeSeconds()
        {
            var configured = embedConfiguration.CurrentValue.SessionLifetimeMinutes;
            var minutes = configured > 0 ? configured : EmbedConfiguration.DefaultSessionLifetimeMinutes;

            return minutes * 60;
        }
    }
}
