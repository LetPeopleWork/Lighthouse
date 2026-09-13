using System.Collections.Concurrent;
using System.Collections.Frozen;
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
        // Matches the throttle the consent service already applies, so an emitting browser keeps its
        // own consent alive without a write per flush.
        private const int TouchesPerWindow = 100;

        private static readonly FrozenSet<UsageDataSuppressionReason> ReasonsThatMeanSomethingIsWrong =
            FrozenSet.ToFrozenSet(
                [
                    UsageDataSuppressionReason.EvaluationFailed,
                    UsageDataSuppressionReason.BudgetExhausted,
                    UsageDataSuppressionReason.SendFailed,
                ]);

        private readonly ConcurrentDictionary<UsageDataSuppressionReason, int> suppressedSoFar = new();

        // Guards everything below that resets when the date changes - the tallies and the spent
        // allowance alike, which have to turn over together or a reader cannot tell which day either
        // of them is describing.
        private readonly Lock everythingTheDayIsCounting = new();

        private DateOnly dayBeingCounted;
        private Exception? lastFailure;
        private int eventsTheDayHasSpent;
        private bool alreadySaidTheAllowanceIsSpent;
        private bool alreadySaidNothingIsGettingThrough;

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
            TurnTheDayOverIfItHas();

            var budget = configuration.CurrentValue.DailyEventBudget;

            lock (everythingTheDayIsCounting)
            {
                if (eventsTheDayHasSpent + events <= budget)
                {
                    eventsTheDayHasSpent += events;
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
                // Stryker disable once String: what is checked about this is that an operator is
                // told once, at a level they see, and told which number was reached. Those are the
                // behaviour; the sentence explaining them to a reader is not.
                "Usage data: the day's allowance of {Budget} event(s) is spent after {Count} on {Day}; "
                + "everything further today is dropped rather than refused",
                budget,
                eventsTheDayHasSpent,
                dayBeingCounted);
        }

        /// <summary>
        /// Puts back what never left. The allowance is taken before the send rather than after it,
        /// so that nothing can be told there is room for the last of it twice over - which means a
        /// send that failed has taken something it did not use. Left taken, a collector that is
        /// unreachable for an hour empties a whole day's allowance onto the floor, and the instance
        /// then stays silent until midnight even after the collector comes back.
        ///
        /// The other half is being able to tell the two days apart. An allowance emptied by sending
        /// and an allowance emptied by failing are the same number; only this says which one
        /// happened, and it says it where an operator sees it.
        /// </summary>
        public void GiveBackWhatCouldNotBeSent(int events, Exception failure)
        {
            TurnTheDayOverIfItHas();

            lock (everythingTheDayIsCounting)
            {
                eventsTheDayHasSpent = Math.Max(0, eventsTheDayHasSpent - events);
                SayNothingIsGettingThroughOnce(failure);
            }

            Suppress(UsageDataSuppressionReason.SendFailed);
        }

        /// <summary>
        /// Once a day, like the line above and for the same reason: a collector that refuses one
        /// batch refuses all of them, and a browser that agreed and left a tab open hands one in
        /// forever, so a line per failure is somebody else deciding how much this instance writes to
        /// its own disk. Louder than the rest of what this file counts, though, because a batch
        /// nobody could send is the only outcome here where somebody agreed, there was room, and the
        /// data still did not arrive - and from outside that is indistinguishable from a working day.
        /// </summary>
        private void SayNothingIsGettingThroughOnce(Exception failure)
        {
            if (alreadySaidNothingIsGettingThrough)
            {
                return;
            }

            alreadySaidNothingIsGettingThrough = true;

            logger.LogWarning(
                failure,
                // Stryker disable once String: what is checked here is that a collector nobody could
                // reach is reported once, at a level an operator sees, carrying the failure. Those
                // three are the behaviour; the sentence explaining them to a reader is not.
                "Usage data: what was sent on {Day} did not arrive. It is dropped rather than kept for later, "
                + "and the day's allowance is not charged for it, so sending picks up again by itself once "
                + "whatever is in the way clears. Reported once rather than once per batch.",
                dayBeingCounted);
        }

        private async Task<UsageDataEmitPermit?> ResolveAsync(string? token, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();

            if (!TheAdministratorAllowsThis(scope))
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
            var consent = await repository.FindByTokenHashAsync(
                UsageDataConsentToken.HashOf(token), cancellationToken);

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

        // Built here rather than resolved, because what is being reused is a rule and not a
        // collaborator: "no row means nothing is vetoed" has to give the same answer on the emit
        // path as it does where the dialog asks whether it may appear, or an administrator who
        // stopped the feature would stop one of the two and not the other.
        private static bool TheAdministratorAllowsThis(IServiceScope scope)
        {
            var features = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            return new UsageDataMasterSwitch(features).IsAllowed();
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
            TurnTheDayOverIfItHas();

            suppressedSoFar.AddOrUpdate(reason, 1, (_, sofar) => sofar + 1);
        }

        private void TurnTheDayOverIfItHas()
        {
            var today = clock.Today;

            lock (everythingTheDayIsCounting)
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
                eventsTheDayHasSpent = 0;
                alreadySaidTheAllowanceIsSpent = false;
                alreadySaidNothingIsGettingThrough = false;
            }
        }

        private void Report(UsageDataSuppressionReason reason, int count)
        {
            // Only one of these means the feature is broken rather than switched off, so only that
            // one arrives at a level an operator sees without going looking, and it carries what
            // went wrong with it.
            var somethingIsWrong = ReasonsThatMeanSomethingIsWrong.Contains(reason);

            logger.Log(
                somethingIsWrong ? LogLevel.Warning : LogLevel.Debug,
                reason == UsageDataSuppressionReason.EvaluationFailed ? lastFailure : null,
                "Usage data: {Count} batch(es) were not forwarded on {Day}, because {Reason}",
                count,
                dayBeingCounted,
                reason);
        }
    }
}
