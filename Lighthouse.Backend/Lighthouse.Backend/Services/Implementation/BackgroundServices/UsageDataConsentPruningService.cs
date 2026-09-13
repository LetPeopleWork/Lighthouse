using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// Forgets browsers that stopped visiting.
    ///
    /// The consent table grows by one row for every browser ever shown the dialog, refusals
    /// included, and until the dialog started arriving uninvited that meant only the handful of
    /// people who went looking for a footer icon. It does not any more, which is why this exists in
    /// the slice that created the problem rather than the one that happened to write the query.
    ///
    /// Nothing here decides what may be forgotten. Both thresholds are worked out from configuration
    /// and handed down, because one of them - how long a refusal is owed its promised question - is
    /// a promise the dialog's copy makes to a reader, and a promise is not a repository's to weigh.
    /// </summary>
    public sealed class UsageDataConsentPruningService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<UsageDataConfiguration> configuration,
        TimeProvider timeProvider,
        ILogger<UsageDataConsentPruningService> logger) : BackgroundService
    {
        private static readonly TimeSpan BetweenPasses = TimeSpan.FromDays(1);

        /// <summary>
        /// Runs one pass and returns. Kept separate from the loop below so that whoever needs the
        /// table tidied at a moment they choose - a test above all, since the test host runs no
        /// background work at all - can have exactly that instead of waiting out a schedule it
        /// cannot see. The same shape the forwarding service uses, for the same reason.
        /// </summary>
        public async Task<int> PruneNowAsync(CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var settings = configuration.CurrentValue;

            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUsageDataConsentRepository>();

            return await repository.PruneStaleAsync(
                lastSeenBefore: now - TimeSpan.FromDays(settings.ConsentRetentionDays),
                owedNothingSince: now - TimeSpan.FromDays(settings.ReAskAfterDays),
                cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var forgotten = await PruneNowAsync(stoppingToken);

                    if (forgotten > 0)
                    {
                        logger.LogInformation(
                            "Usage data: forgot {Forgotten} browsers that had stopped visiting.", forgotten);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception housekeepingFailed)
                {
                    // Housekeeping that cannot run is a table that grows, not an instance that
                    // misbehaves, so this reports and waits rather than stopping. A service that
                    // died here would take its own next attempt with it.
                    logger.LogError(housekeepingFailed,
                        "Usage data: could not forget browsers that had stopped visiting. "
                        + "Nothing is lost and nothing extra is sent; the table keeps its rows until "
                        + "the next attempt.");
                }

                try
                {
                    await Task.Delay(BetweenPasses, timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
