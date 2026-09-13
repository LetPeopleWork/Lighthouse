using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Services.Implementation.UsageData
{
    public class UsageDataConsentService(
        IUsageDataConsentRepository repository,
        ILicenseService licenseService,
        IAppSettingService appSettings,
        IUsageDataMasterSwitch masterSwitch,
        IOptionsMonitor<UsageDataConfiguration> configuration,
        TimeProvider timeProvider) : IUsageDataConsentService
    {
        // The liveness stamp is refreshed at most this often per browser. Without a throttle a
        // read-shaped request becomes a write on every page load, and SQLite serialises writers across
        // the whole process - a few open tabs would then contend with the background refreshes.
        private const int TouchesPerWindow = 100;

        // 256 bits. The token is the only handle on a consent record and is never re-issued, so it
        // has to be unguessable rather than merely unique.
        private const int TokenByteLength = 32;

        // 128 bits, which is plenty for a value whose only job is to be distinct: unlike the token it
        // grants nothing, so guessing one buys an attacker no capability.
        private const int AnalyticsIdByteLength = 16;

        public async Task<UsageDataState> GetStateAsync(string? token, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var window = LivenessWindow;

            var consent = string.IsNullOrWhiteSpace(token)
                ? null
                : await repository.FindByTokenHashAsync(UsageDataConsentToken.HashOf(token), cancellationToken);

            if (consent != null)
            {
                await repository.TouchAsync(
                    consent.TokenHash, now, now - (window / TouchesPerWindow), cancellationToken);
            }

            // This browser's own answer, not the instance's. The indicator beside it says "being sent
            // from this browser", and a colleague's grant must not make that sentence appear over
            // somebody who declined - which is what asking whether anyone at all still consents did.
            // No liveness check here on purpose: liveness is how a browser that went away stops
            // counting, and this browser is asking right now. Its stamp was just refreshed above, and
            // the refresh does not write back to the entity read before it - so testing that stamp
            // would report "not sending" on the very request that revived it.
            var sending = consent?.Decision == UsageDataDecision.Granted;

            // A token this instance never minted lands here with consent still null, which is exactly
            // where a browser holding no token lands. The two answers are identical by construction
            // rather than by a rule somebody has to remember.
            return new UsageDataState(
                Sending: sending,
                Decision: consent?.Decision.ToString(),
                MayAsk: MayAsk(consent, now),
                ReAskAfterDays: configuration.CurrentValue.ReAskAfterDays);
        }

        public async Task<string> RecordDecisionAsync(UsageDataDecision decision, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            var token = UrlSafeValue.Generate(TokenByteLength);

            await repository.AddAsync(
                new UsageDataConsent
                {
                    TokenHash = UsageDataConsentToken.HashOf(token),
                    Decision = decision,
                    DecidedAt = now,
                    LastSeenAt = now,

                    // Written in the same save as the answer itself, so there is no moment where a
                    // browser has consented but has nothing to be counted under. Only for a yes: a
                    // browser that refused must have no pseudonym anywhere.
                    AnalyticsId = decision == UsageDataDecision.Granted
                        ? UrlSafeValue.Generate(AnalyticsIdByteLength)
                        : null,
                },
                cancellationToken);

            return token;
        }

        public async Task RevokeAsync(string token, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            // The affected-row count is deliberately dropped. It is the honest answer to "did anything
            // change", and telling the browser would make this endpoint a way to find out whether a
            // given token is real.
            await repository.TryRevokeAsync(UsageDataConsentToken.HashOf(token), now, cancellationToken);
        }

        public async Task RecordAskedAsync(string token, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            // The affected-row count is dropped here for the reason it is dropped in the withdrawal
            // above: reporting whether anything was written would tell a caller whether the token it
            // held is one this instance ever minted.
            await repository.TryMarkAskedAsync(UsageDataConsentToken.HashOf(token), now, cancellationToken);
        }

        private TimeSpan LivenessWindow => TimeSpan.FromDays(configuration.CurrentValue.ConsentLivenessWindowDays);

        /// <summary>
        /// Whether to put the question to this browser now, without it having asked to be asked.
        ///
        /// Everything the decision rests on is weighed here rather than in the browser, and not only
        /// because the install timestamp sits behind authentication while this endpoint has none. A
        /// privacy gate settled against a clock and a licence the caller controls is not a gate; the
        /// browser is told the answer and nothing it could have argued with.
        ///
        /// The one thing this cannot see is a browser holding no token that was shown the dialog and
        /// closed it. There is no row to have written that against, and minting one would be
        /// recording a decision nobody made - so that half is remembered in the browser, and this
        /// answers only for what reached the table.
        /// </summary>
        private bool MayAsk(UsageDataConsent? consent, DateTime now)
        {
            if (!masterSwitch.IsOn())
            {
                return false;
            }

            if (appSettings.GetInstallTimestamp() is not { } installedAt
                || now - installedAt.UtcDateTime < TimeSpan.FromDays(configuration.CurrentValue.AskAfterInstallDays))
            {
                // An instance whose install timestamp could not be established is one whose age
                // nobody knows, and asking an unknown-aged instance is the case this threshold
                // exists to prevent.
                return false;
            }

            if (consent is null)
            {
                return true;
            }

            if (!TheQuestionIsStillOpen(consent.Decision))
            {
                return false;
            }

            // Anchored on the last time the question was actually put, falling back to the answer
            // when it has never been put unprompted. Anchoring on the decision alone would re-ask a
            // browser every session once its window had passed, because closing the dialog leaves
            // the stored decision exactly where it was.
            var lastAsked = consent.AskedAt ?? consent.DecidedAt;

            return now - lastAsked >= TimeSpan.FromDays(configuration.CurrentValue.ReAskAfterDays);
        }

        /// <summary>
        /// Whether there is anything left to ask this browser, ever - before any question of timing.
        ///
        /// It depends on the licence, which is why it is settled here rather than anywhere a caller
        /// could see: the state endpoint needs no authentication, and the tier is not something an
        /// anonymous caller may learn.
        /// </summary>
        private bool TheQuestionIsStillOpen(UsageDataDecision? decision)
        {
            // Someone who already agreed is not asked again; there is nothing left to ask them.
            if (decision == UsageDataDecision.Granted)
            {
                return false;
            }

            // A Premium refusal is final - that is what the tier buys. Everything else is still
            // open: a Community refusal comes back on a cadence, and a withdrawal leaves the door
            // open on either tier, because changing your mind once must not cost you the chance to
            // change it back.
            var refusalIsFinal =
                decision == UsageDataDecision.Declined && licenseService.CanUsePremiumFeatures();

            return !refusalIsFinal;
        }
    }
}
