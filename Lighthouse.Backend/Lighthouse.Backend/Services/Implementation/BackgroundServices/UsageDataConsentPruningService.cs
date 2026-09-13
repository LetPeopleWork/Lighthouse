using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Services.Interfaces.Licensing;
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
    /// Nothing here decides what may be forgotten. The thresholds are worked out from configuration
    /// and handed down, and so is whether a refusal is permanent, because that is a promise the
    /// licence makes to a customer and a promise is not a repository's to weigh.
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

            // Read here rather than in the repository, for the reason the thresholds are: whether a
            // refusal is permanent is a product promise made by the licence, and a query is not the
            // place to decide what the product promised.
            var licensing = scope.ServiceProvider.GetRequiredService<ILicenseService>();

            return await repository.PruneStaleAsync(
                lastSeenBefore: now - TimeSpan.FromDays(settings.ConsentRetentionDays),
                owedNothingSince: now - TimeSpan.FromDays(settings.ReAskAfterDays),
                refusalsAreFinal: licensing.CanUsePremiumFeatures(),
                cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var forgotten = await PruneNowAsync(stoppingToken);

                    // Stryker disable once Equality,Block,Negate: whether a pass that removed
                    // nothing says so is not behaviour - an operator reading "forgot 0 browsers"
                    // every day learns the same thing as one reading nothing. What the count means
                    // is asserted where it is returned.
                    if (forgotten > 0)
                    {
                        // Stryker disable once String,Statement: this line exists to put a number in
                        // front of an operator. That the number is right is asserted on the value
                        // this method returns; the sentence carrying it is not the behaviour.
                        logger.LogInformation(
                            "Usage data: forgot {Forgotten} browsers that had stopped visiting.", forgotten);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Stryker disable once Statement: leaving without this returns to the loop
                    // condition, which is the same cancellation token and exits immediately. The
                    // early return says what is happening; it does not change what happens.
                    return;
                }
                // Stryker disable once Block: emptying this catch leaves the loop doing what it
                // does now - surviving the failure and trying again, which is asserted. All that is
                // lost is the line telling an operator, and that is what the disable below covers.
                catch (Exception housekeepingFailed)
                {
                    // Housekeeping that cannot run is a table that grows, not an instance that
                    // misbehaves, so this reports and waits rather than stopping. A service that
                    // died here would take its own next attempt with it.
                    //
                    // Stryker disable once String,Statement: the behaviour is that the loop survives
                    // a failed pass and tries again, which is asserted. That an operator is told is
                    // worth doing and is not something a test can hold to without pinning a
                    // sentence, which is the thing most likely to be reworded for its own sake.
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
                    // Same as above: the loop condition would end this anyway on the next turn.
                    // Stryker disable once Statement
                    return;
                }
            }
        }
    }
}
