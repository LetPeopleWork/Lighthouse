using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// Every endpoint here is deliberately anonymous. Authentication is optional in Lighthouse and
    /// impossible in the standalone build, and with it off every caller is the same subject - so an
    /// account-scoped consent would collapse to whatever the first person clicked, applied to
    /// everybody. The browser is the only unit of consent that means the same thing in every
    /// deployment shape. The application's fallback policy requires an authenticated user, so the
    /// exemption has to be stated rather than assumed.
    /// </summary>
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class UsageDataController(IUsageDataConsentService consentService) : ControllerBase
    {
        // Public because the rate limiter reads it too: the event endpoint is counted per browser
        // rather than per address, and two spellings of this name would count a whole office as one.
        public const string ConsentTokenHeader = "X-Lighthouse-UsageData-Token";

        private const string Granted = "granted";
        private const string Declined = "declined";

        // Rate-limited like the two writes, for a different reason. This one only reads, but it does
        // measurably more work when it is given a token than when it is not, and an attacker who can
        // ask without limit could average that difference out and learn which tokens this instance
        // has seen - the very thing the identical responses exist to prevent.
        [HttpGet("state")]
        [EnableRateLimiting(RateLimitingConfiguration.UsageDataConsentPolicy)]
        public async Task<ActionResult<UsageDataState>> GetState(
            [FromHeader(Name = ConsentTokenHeader)] string? token, CancellationToken cancellationToken)
        {
            // A cache in front of this would serve one browser's answer to another, and would absorb
            // the request that keeps consent alive - so consent would decay under somebody who is
            // actively using Lighthouse, with nothing on any screen to show it happening.
            Response.Headers.CacheControl = "no-store";

            var state = await consentService.GetStateAsync(token, cancellationToken);

            return Ok(state);
        }

        [HttpPost("consent")]
        [EnableRateLimiting(RateLimitingConfiguration.UsageDataConsentPolicy)]
        public async Task<ActionResult<UsageDataConsentResponseDto>> RecordDecision(
            [FromBody] UsageDataConsentRequestDto request, CancellationToken cancellationToken)
        {
            // Only the two answers a person can actually give. Withdrawal is not one of them: it acts
            // on a consent that already exists and so belongs to the endpoint that carries the token.
            var decision = request.Decision?.Trim().ToLowerInvariant() switch
            {
                Granted => UsageDataDecision.Granted,
                Declined => UsageDataDecision.Declined,
                _ => (UsageDataDecision?)null,
            };

            if (decision is null)
            {
                return BadRequest($"decision must be '{Granted}' or '{Declined}'");
            }

            var token = await consentService.RecordDecisionAsync(decision.Value, cancellationToken);

            return Ok(new UsageDataConsentResponseDto(token));
        }

        [HttpDelete("consent")]
        [EnableRateLimiting(RateLimitingConfiguration.UsageDataConsentPolicy)]
        public async Task<IActionResult> Revoke(
            [FromHeader(Name = ConsentTokenHeader)] string? token, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                await consentService.RevokeAsync(token, cancellationToken);
            }

            // The same answer whether a consent was withdrawn, none was found, or no token was sent
            // at all. Distinguishing them would let anyone hold up a token and be told whether this
            // instance has ever seen it.
            return NoContent();
        }

        /// <summary>
        /// What a browser hands in. The answer is the same whether the batch will be used or thrown
        /// away, so a caller cannot hold up a token and be told whether this instance minted it -
        /// the same reason withdrawal answers the way it does. A body that cannot be read is the one
        /// exception, and it is not a probe: nothing but our own page posts here, so an unreadable
        /// message is our bug and saying so costs nobody anything.
        /// </summary>
        [HttpPost("events")]
        [EnableRateLimiting(RateLimitingConfiguration.UsageDataIngestPolicy)]
        public IActionResult HandInEvents([FromBody] UsageDataEventBatchDto? batch)
        {
            if (!CanBeRead(batch))
            {
                return BadRequest();
            }

            return NoContent();
        }

        private static bool CanBeRead(UsageDataEventBatchDto? batch)
        {
            return batch?.Events is { Length: > 0 } reported && Array.TrueForAll(reported, CanBeRead);
        }

        /// <summary>
        /// Every part has to have actually been sent, and both choices have to be members of the list
        /// they claim. A whole number left out of a message arrives as zero, and zero names a real
        /// choice in both lists - so reading one straight would invent an event nobody reported.
        /// </summary>
        private static bool CanBeRead(UsageDataEventDto reported)
        {
            return reported is not null
                && reported.Name is { } name && Enum.IsDefined(name)
                && reported.Route is { } route && Enum.IsDefined(route)
                && reported.OffsetMs >= 0
                && reported.Sequence >= 0;
        }
    }
}
