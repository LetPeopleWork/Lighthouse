using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Services.Implementation.UsageData
{
    /// <summary>
    /// Asks the record, every time, whether this browser is agreeing right now. Nothing is remembered
    /// between requests on purpose: a browser flushes at most twice a minute and the lookup rides a
    /// unique index, so there is no cost worth avoiding - and the only thing remembering could buy is
    /// sending after somebody withdrew, which is the one outcome this exists to prevent.
    ///
    /// Every uncertainty comes back as "do not send". That is the opposite of how the permission
    /// summary the screens use behaves, and deliberately so: there, a question nobody can answer
    /// leaves a button visible; here it stops data leaving a customer's instance.
    /// </summary>
    public sealed class UsageDataGate(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<UsageDataConfiguration> configuration,
        ILighthouseClock clock,
        ILogger<UsageDataGate> logger) : IUsageDataGate
    {
        /// <summary>
        /// The operator's switch for the whole feature. Nothing writes this row yet - a Lighthouse
        /// that has never been told otherwise sends, subject to consent, which is what makes turning
        /// it off later a smaller change than turning it on.
        /// </summary>
        private const string MasterSwitchKey = "UsageData";

        // Matches the throttle the consent service already applies, so an emitting browser keeps its
        // own consent alive without a write per flush.
        private const int TouchesPerWindow = 100;

        private static readonly UsageDataSuppressionReason[] ReasonsThatMeanSomethingIsWrong =
            [UsageDataSuppressionReason.EvaluationFailed, UsageDataSuppressionReason.BudgetExhausted];

        private readonly ConcurrentDictionary<UsageDataSuppressionReason, int> suppressedSoFar = new();
        private readonly Lock reporting = new();

        private DateOnly dayBeingCounted;
        private Exception? lastFailure;
        private int eventsSentToday;
        private bool alreadySaidTheAllowanceIsSpent;

        public async Task<UsageDataEmitPermit?> RequestPermitAsync(string? token, CancellationToken cancellationToken)
        {
            try
            {
                return await ResolveAsync(token, cancellationToken);
            }
            catch (Exception failure)
            {
                // Nothing is rethrown and nothing is logged here. This sits on a request somebody
                // else is waiting on, and a browser handing in what it saw must not be told that
                // this instance's database is unwell - nor have its page fail over it.
                lastFailure = failure;
                Suppress(UsageDataSuppressionReason.EvaluationFailed);
                return null;
            }
        }

        public async Task<UsageDataEmitPermit?> RequestPermitToSendAsync(
            string? token, int events, CancellationToken cancellationToken)
        {
            var permit = await RequestPermitAsync(token, cancellationToken);

            if (permit is null)
            {
                return null;
            }

            return TheDaysAllowanceCovers(events) ? permit : null;
        }

        /// <summary>
        /// Takes this batch out of what the day has left, or reports that it does not fit. A batch is
        /// all-or-nothing: sending the first few events of one and dropping the rest would leave the
        /// collector holding half of somebody's visit, which reads as a person who left rather than a
        /// number that ran out.
        /// </summary>
        private bool TheDaysAllowanceCovers(int events)
        {
            ReportWhatTheDayJustEndedCounted();

            var budget = configuration.CurrentValue.DailyEventBudget;

            lock (reporting)
            {
                if (eventsSentToday + events <= budget)
                {
                    eventsSentToday += events;
                    return true;
                }

                SayTheAllowanceIsSpentOnce(budget);
            }

            Suppress(UsageDataSuppressionReason.BudgetExhausted);
            return false;
        }

        /// <summary>
        /// Once a day, not once a drop. Silent to the browser is not silent to the operator - an
        /// instance that empties its allowance is either unusually busy or being abused, and those
        /// are indistinguishable from outside unless it says something. But a line per drop is a
        /// flood mechanism wearing a monitoring costume, and whoever triggered it could then fill
        /// the disk of the instance they are already abusing.
        /// </summary>
        private void SayTheAllowanceIsSpentOnce(int budget)
        {
            if (alreadySaidTheAllowanceIsSpent)
            {
                return;
            }

            alreadySaidTheAllowanceIsSpent = true;

            logger.LogWarning(
                "Usage data: the day's allowance of {Budget} event(s) is spent after {Count} on {Day}; "
                + "everything further today is dropped rather than refused",
                budget,
                eventsSentToday,
                dayBeingCounted);
        }

        private async Task<UsageDataEmitPermit?> ResolveAsync(string? token, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();

            if (!TheFeatureIsSwitchedOn(scope))
            {
                Suppress(UsageDataSuppressionReason.MasterSwitchOff);
                return null;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                Suppress(UsageDataSuppressionReason.NoLiveConsent);
                return null;
            }

            var repository = scope.ServiceProvider.GetRequiredService<IUsageDataConsentRepository>();
            var consent = await repository.FindByTokenHashAsync(Hash(token), cancellationToken);

            if (!IsAgreeingNow(consent))
            {
                Suppress(UsageDataSuppressionReason.NoLiveConsent);
                return null;
            }

            await KeepThisBrowsersConsentAliveAsync(repository, consent!, cancellationToken);

            return PermitFor(consent!);
        }

        /// <summary>
        /// Agreed, and seen recently enough to still count. Nothing else on the record is read until
        /// this has come back true: withdrawing leaves the pseudonym a browser built up history under
        /// sitting on the row, and reading it before the answer is settled - even only to write it to
        /// a log line - hands that history back to somebody who asked to stop being counted.
        /// </summary>
        private bool IsAgreeingNow(UsageDataConsent? consent)
        {
            if (consent?.Decision != UsageDataDecision.Granted)
            {
                return false;
            }

            return consent.LastSeenAt > clock.Now.UtcDateTime - LivenessWindow;
        }

        private UsageDataEmitPermit? PermitFor(UsageDataConsent consent)
        {
            if (string.IsNullOrWhiteSpace(consent.AnalyticsId))
            {
                Suppress(UsageDataSuppressionReason.NoLiveConsent);
                return null;
            }

            return new UsageDataEmitPermit(consent.AnalyticsId);
        }

        private Task<int> KeepThisBrowsersConsentAliveAsync(
            IUsageDataConsentRepository repository, UsageDataConsent consent, CancellationToken cancellationToken)
        {
            var now = clock.Now.UtcDateTime;

            return repository.TouchAsync(
                consent.TokenHash, now, now - (LivenessWindow / TouchesPerWindow), cancellationToken);
        }

        private static bool TheFeatureIsSwitchedOn(IServiceScope scope)
        {
            var features = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            return features.GetByPredicate(feature => feature.Key == MasterSwitchKey)?.Enabled != false;
        }

        private TimeSpan LivenessWindow => TimeSpan.FromDays(configuration.CurrentValue.ConsentLivenessWindowDays);

        /// <summary>
        /// Counted rather than written down. A browser that withdrew and left a tab open goes on
        /// flushing forever, so a line per suppressed batch would make the instance whose owner
        /// already withdrew the one that fills its own disk. An operator gets one total per reason
        /// per day instead.
        /// </summary>
        private void Suppress(UsageDataSuppressionReason reason)
        {
            ReportWhatTheDayJustEndedCounted();

            suppressedSoFar.AddOrUpdate(reason, 1, (_, sofar) => sofar + 1);
        }

        private void ReportWhatTheDayJustEndedCounted()
        {
            var today = clock.Today;

            lock (reporting)
            {
                if (today == dayBeingCounted)
                {
                    return;
                }

                foreach (var (reason, count) in suppressedSoFar)
                {
                    Report(reason, count);
                }

                suppressedSoFar.Clear();
                lastFailure = null;
                dayBeingCounted = today;

                // Held in memory and nowhere else. A restart starts the day over, which is the same
                // residue the per-browser limiter beside this one already carries - and a counter
                // written to a customer's database would outlive the thing it counts.
                eventsSentToday = 0;
                alreadySaidTheAllowanceIsSpent = false;
            }
        }

        private void Report(UsageDataSuppressionReason reason, int count)
        {
            // Only one of these means the feature is broken rather than switched off, so only that
            // one arrives at a level an operator sees without going looking, and it carries what
            // went wrong with it.
            var somethingIsWrong = Array.IndexOf(ReasonsThatMeanSomethingIsWrong, reason) >= 0;

            logger.Log(
                somethingIsWrong ? LogLevel.Warning : LogLevel.Debug,
                reason == UsageDataSuppressionReason.EvaluationFailed ? lastFailure : null,
                "Usage data: {Count} batch(es) were not forwarded on {Day}, because {Reason}",
                count,
                dayBeingCounted,
                reason);
        }

        private static string Hash(string token)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
