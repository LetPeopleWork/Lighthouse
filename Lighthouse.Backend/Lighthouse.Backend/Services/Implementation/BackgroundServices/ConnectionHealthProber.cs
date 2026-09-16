using Lighthouse.Backend.Services.Implementation.ConnectionHealth;
using Lighthouse.Backend.Services.Interfaces.ConnectionHealth;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// Asks the connections nothing else has heard from. Without it a connection that no team and no
    /// portfolio uses is refreshed by nothing, so it reads "not checked" for as long as the process
    /// lives and the only thing that can ever change that is an administrator remembering to press a
    /// button.
    ///
    /// It owns when, and nothing else. What counts as stale, which connections that leaves, and what is
    /// recorded about them all belong to <see cref="IConnectionHealthService"/> — the one writer of a
    /// verdict, so that an administrator never meets two answers to whether a credential works.
    ///
    /// It does not run through the update queue. That queue is a single lane which awaits each refresh
    /// to completion, so a connector wedged for forty minutes would stop health checking for forty
    /// minutes — precisely the situation in which an operator needs to know which connection is the
    /// problem.
    /// </summary>
    public sealed class ConnectionHealthProber(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ConnectionHealthProber> logger) : BackgroundService
    {
        /// <summary>
        /// One pass, then return. Kept separate from the loop below so that whoever needs the stale
        /// connections asked at a moment they choose — a test above all, since the test host runs no
        /// background work at all — can have exactly that, instead of waiting out a schedule it cannot
        /// see.
        /// </summary>
        public async Task CheckWhatHasNotBeenHeardFromAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var health = scope.ServiceProvider.GetRequiredService<IConnectionHealthService>();

            await health.RefreshStaleVerdictsAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await LookWithoutLettingOneFailureEndTheLoop(stoppingToken);

                try
                {
                    await Task.Delay(HowOftenToLook(), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task LookWithoutLettingOneFailureEndTheLoop(CancellationToken stoppingToken)
        {
            try
            {
                await CheckWhatHasNotBeenHeardFromAsync(stoppingToken);
            }
#pragma warning disable CA1031 // an exception that ended this loop would stop health checking for as long as the process lives, with nobody told
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Checking connection health failed");
            }
        }

        private TimeSpan HowOftenToLook()
        {
            using var scope = serviceScopeFactory.CreateScope();

            return scope.ServiceProvider.GetRequiredService<ConnectionHealthCadence>().HowOftenToLook;
        }
    }
}
