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
    // Answering the question and handing in what a browser saw are one job, not two. Everything here
    // is the same anonymous surface for the same browser, identified by the same header, and the only
    // reason the ingest route knows anything at all is that it has to check the answer the other
    // routes recorded. Two controllers would put that one promise behind two exemptions from the
    // authentication policy, with two chances to drift apart.
#pragma warning disable S6960
    public class UsageDataController(
        IUsageDataConsentService consentService, IUsageDataGate gate, IUsageDataEventQueue queue) : ControllerBase
#pragma warning restore S6960
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
        /// A browser reporting that it was shown the dialog without having asked to be.
        ///
        /// It exists so that a browser which closes the dialog is left alone. Its stored answer did
        /// not change, so without this the server keeps saying it is due and the question arrives
        /// once a session for ever - the nag this was built to prevent, delivered by the mechanism
        /// meant to prevent it.
        ///
        /// A browser holding no token gets the same empty answer and nothing is written. There is no
        /// row to write against, and minting one would record a decision nobody made; that browser
        /// remembers being asked in its own storage instead.
        /// </summary>
        [HttpPost("asked")]
        [EnableRateLimiting(RateLimitingConfiguration.UsageDataConsentPolicy)]
        public async Task<IActionResult> RecordAsked(
            [FromHeader(Name = ConsentTokenHeader)] string? token, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                await consentService.RecordAskedAsync(token, cancellationToken);
            }

            // The same answer for a token that resolved, one that did not, and none at all - the
            // reason withdrawal answers the way it does.
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
        public async Task<IActionResult> HandInEvents(
            [FromBody] UsageDataEventBatchDto? batch,
            [FromHeader(Name = ConsentTokenHeader)] string? token,
            CancellationToken cancellationToken)
        {
            if (batch?.Events is not { Length: > 0 } reported)
            {
                return BadRequest();
            }

            var takenIn = new List<UsageDataEventReported>(reported.Length);

            foreach (var one in reported)
            {
                if (AsTakenIn(one) is not { } readable)
                {
                    return BadRequest();
                }

                takenIn.Add(readable);
            }

            // Whether the token resolves decides nothing about the answer below, which is the point.
            // What it decides is whether the batch goes on to wait: it is asked here so that nothing
            // is kept on behalf of somebody who never agreed, and asked again where it is sent so
            // that nothing kept survives somebody changing their mind.
            var permit = await gate.RequestPermitAsync(token, cancellationToken);

            if (permit is not null && token is { } presented)
            {
                queue.HandIn(new AcceptedUsageDataBatch(presented, takenIn));
            }

            return NoContent();
        }

        /// <summary>
        /// Reads one part of the message, or says it cannot be read. Every choice has to be a member
        /// of the list it claims: a whole number left out of a message arrives as zero, and zero
        /// names a real choice in every one of those lists, so reading one straight would invent an
        /// event nobody reported.
        ///
        /// What else a message carries is not the same for every event. Two events say which page
        /// somebody opened and must name one of this product's own; the rest happen on no particular
        /// page and must name none, and the same holds for which kind of system was connected and
        /// which setting was switched. A part on an event that has no business carrying it is
        /// refused rather than ignored -
        /// whoever sent it believed it would be counted, and a message half accepted is the one
        /// nobody notices.
        ///
        /// Reading and checking are one act here rather than two passes, so there is no arrangement
        /// in which something got past the check and was then read as a zero anyway.
        /// </summary>
        private static UsageDataEventReported? AsTakenIn(UsageDataEventDto? reported)
        {
            if (reported is null
                || reported.Name is not { } name || !Enum.IsDefined(name)
                || (reported.Route is { } named && !Enum.IsDefined(named))
                || (reported.WorkTrackingSystem is { } system && !Enum.IsDefined(system))
                || (reported.OptionalFeature is { } setting && !Enum.IsDefined(setting))
                || !UsageDataEventShapes.Fits(
                    name, reported.Route, reported.WorkTrackingSystem, reported.OptionalFeature, reported.Enabled)
                || reported.OffsetMs is not { } offset || offset < 0
                || reported.Sequence is not { } sequence || sequence < 0)
            {
                return null;
            }

            return new UsageDataEventReported(
                name,
                reported.Route,
                reported.WorkTrackingSystem,
                reported.OptionalFeature,
                reported.Enabled,
                offset,
                sequence);
        }
    }
}
