using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// What "the update queue has nothing left to do" means, in the one place every acceptance harness
    /// can read it from, because it is no longer what the status store alone answers.
    ///
    /// A refresh can hold a forecast back until the refreshes feeding it have finished, and work that is
    /// being held is deliberately not in the store - that is what holding it means. A portfolio refresh
    /// holds one every time, so a harness asking only the store reads idle while a forecast is still owed,
    /// tears the host down, and the late forecast runs against a disposed host and a deleted database. It
    /// does not fail the test that caused it; it fails whichever test runs next, and is recorded as though
    /// that test had asked for it.
    ///
    /// Idle also has to hold still rather than be read once: there is a moment where released work has
    /// left the holding pen and has not yet been admitted, and it is neither held nor active in it.
    /// </summary>
    internal static class TheUpdateQueueSettling
    {
        /// <summary>
        /// Enough consecutive readings that the handover between the holding pen and the queue cannot sit
        /// inside them, and few enough that a harness waiting for one is still waiting rather than soaking.
        /// </summary>
        private const int ReadingsThatMakeIdleBelievable = 25;

        private const int PollIntervalMs = 20;

        internal static async Task<bool> SettlesWithin(IServiceProvider services, TimeSpan patience)
        {
            var store = services.GetRequiredService<IUpdateStatusStore>();
            var queue = services.GetRequiredService<IUpdateQueueService>();
            var forecastsThatCouldStillBeOwed = ForecastKeysOfEveryPortfolio(services);

            var deadline = DateTime.UtcNow.Add(patience);
            var consecutiveIdleReadings = 0;

            while (consecutiveIdleReadings < ReadingsThatMakeIdleBelievable)
            {
                if (DateTime.UtcNow > deadline)
                {
                    return false;
                }

                await Task.Delay(PollIntervalMs);

                var stillBusy = store.HasActiveWork() || forecastsThatCouldStillBeOwed.Exists(queue.IsHeld);
                consecutiveIdleReadings = stillBusy ? 0 : consecutiveIdleReadings + 1;
            }

            return true;
        }

        private static List<UpdateKey> ForecastKeysOfEveryPortfolio(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            return [.. scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>()
                .GetAll()
                .Select(portfolio => new UpdateKey(UpdateType.Forecasts, portfolio.Id))];
        }
    }
}
