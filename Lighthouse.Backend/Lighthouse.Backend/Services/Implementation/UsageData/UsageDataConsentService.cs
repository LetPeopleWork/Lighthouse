using System.Security.Cryptography;
using System.Text;
using Lighthouse.Backend.Configuration;
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
        TimeProvider timeProvider) : IUsageDataConsentService
    {
        // The liveness stamp is refreshed at most this often per browser. Without a throttle a
        // read-shaped request becomes a write on every page load, and SQLite serialises writers across
        // the whole process - a few open tabs would then contend with the background refreshes.
        private const int TouchesPerWindow = 100;

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
            if (decision == UsageDataDecision.Granted)
            {
                await appSettingService.EnsureUsageDataInstanceId();
            }

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

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

            // A Premium refusal is final. A Community refusal comes back on a cadence, and a
            // withdrawal leaves the door open on either tier - otherwise changing your mind once
            // would cost you the chance to change it back.
            return decision != UsageDataDecision.Declined || !licenseService.CanUsePremiumFeatures();
        }

        private static string Hash(string token)
        {
            // The token is 256 bits of randomness, so a fast digest is the right tool and a password
            // KDF is not: there is no low-entropy secret here to slow an attacker down over.
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
