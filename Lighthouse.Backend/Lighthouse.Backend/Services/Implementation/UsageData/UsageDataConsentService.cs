using System.Security.Cryptography;
using System.Text;
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
        IAppSettingService appSettingService,
        ILicenseService licenseService,
        IOptionsMonitor<UsageDataConfiguration> configuration,
        TimeProvider timeProvider,
        ILogger<UsageDataConsentService> logger) : IUsageDataConsentService
    {
        // The liveness stamp is refreshed at most this often per browser. Without a throttle a
        // read-shaped request becomes a write on every page load, and SQLite serialises writers across
        // the whole process - a few open tabs would then contend with the background refreshes.
        private const int TouchesPerWindow = 100;

        // 256 bits. The token is the only handle on a consent record and is never re-issued, so it
        // has to be unguessable rather than merely unique.
        private const int TokenByteLength = 32;

        public async Task<UsageDataState> GetStateAsync(string? token, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var window = LivenessWindow;

            var consent = string.IsNullOrWhiteSpace(token)
                ? null
                : await repository.FindByTokenHashAsync(Hash(token), cancellationToken);

            if (consent != null)
            {
                await repository.TouchAsync(
                    consent.TokenHash, now, now - (window / TouchesPerWindow), cancellationToken);
            }

            var sending = await repository.AnyLiveGrantAsync(now - window, cancellationToken);

            // A token this instance never minted lands here with consent still null, which is exactly
            // where a browser holding no token lands. The two answers are identical by construction
            // rather than by a rule somebody has to remember.
            return new UsageDataState(
                Sending: sending,
                Decision: consent?.Decision.ToString(),
                WillAskAgain: WillAskAgain(consent?.Decision));
        }

        public async Task<string> RecordDecisionAsync(UsageDataDecision decision, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            // The identifier is written first and on its own. Minting it in the same save as the
            // consent row would mean a failure writing either one loses both - and the one that must
            // not be lost is the person's answer.
            //
            // Which is also why this cannot be allowed to throw. The answer is the thing the person
            // gave us; an instance that ends up consenting but unnamed simply sends nothing until the
            // identifier is minted on a later grant, and that is a far better outcome than refusing
            // somebody's decision because of a write they know nothing about.
            if (decision == UsageDataDecision.Granted)
            {
                try
                {
                    await appSettingService.EnsureUsageDataInstanceId();
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Could not mint the usage data instance identifier. The consent is still being recorded.");
                }
            }

            var token = UrlSafeValue.Generate(TokenByteLength);

            await repository.AddAsync(
                new UsageDataConsent
                {
                    TokenHash = Hash(token),
                    Decision = decision,
                    DecidedAt = now,
                    LastSeenAt = now,
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
            await repository.TryRevokeAsync(Hash(token), now, cancellationToken);
        }

        private TimeSpan LivenessWindow => TimeSpan.FromDays(configuration.CurrentValue.ConsentLivenessWindowDays);

        /// <summary>
        /// Whether this browser can expect to be asked again. Derived here, on the server, so that the
        /// anonymous state endpoint can answer it without disclosing the licence tier it depends on.
        /// </summary>
        private bool WillAskAgain(UsageDataDecision? decision)
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

        private static string Hash(string token)
        {
            // The token is 256 bits of randomness, so a fast digest is the right tool and a password
            // KDF is not: there is no low-entropy secret here to slow an attacker down over.
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
