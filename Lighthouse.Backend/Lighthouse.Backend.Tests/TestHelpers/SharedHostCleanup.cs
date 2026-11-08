using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// What a scenario must leave behind on a host that serves a whole fixture: nothing queued, nothing
    /// running, nothing remembered under an owner id that the next scenario's fresh database hands out
    /// again. Each fixture decides which of these its host needs and in which order; this class holds
    /// how each one is done.
    /// </summary>
    public static class SharedHostCleanup
    {
        private static readonly TimeSpan PollEvery = TimeSpan.FromMilliseconds(20);

        /// <summary>Waits for the update queue to report nothing active; says whether it did within the time.</summary>
        public static async Task<bool> WaitUntilTheQueueIsIdle(IServiceProvider services, TimeSpan within)
        {
            var store = services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow + within;

            while (store.HasActiveWork() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollEvery);
            }

            return !store.HasActiveWork();
        }

        public static async Task WaitUntilTheQueueIsIdleOrFail(IServiceProvider services, TimeSpan within)
        {
            if (!await WaitUntilTheQueueIsIdle(services, within))
            {
                Assert.Fail("The update queue did not go idle in time; a stalled update would leak into the next test on this host.");
            }
        }

        /// <summary>
        /// Runs the passes a scenario asked the filler for and left queued, so none of them is left to run
        /// against the next scenario's database. They write into the database of the scenario that asked,
        /// which is deleted right after. It also lets their owners be asked for again: the filler only
        /// forgets an owner once a pass for it has ended, and an ask left waiting would otherwise turn the
        /// next scenario's own ask for the same owner id away.
        /// </summary>
        public static async Task DrainTheHistoryFillerOrFail(OverTimeHistoryFiller filler, TimeSpan within)
        {
            var emptied = Task.Run(() => filler.DrainAsync(CancellationToken.None));

            if (await Task.WhenAny(emptied, Task.Delay(within)) != emptied)
            {
                Assert.Fail("The filler's queue did not empty in time; a pass left running would write into the next scenario's database.");
            }

            await emptied;

            var deadline = DateTime.UtcNow + within;
            while (filler.HasPassInFlight && DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollEvery);
            }

            if (filler.HasPassInFlight)
            {
                Assert.Fail("A reconstruction pass was still running at the end of the scenario; it would leak into the next one on this host.");
            }
        }

        /// <summary>
        /// The memo keeps notes per owner id for as long as the host lives; a refreshed event is how it is
        /// told to drop them.
        /// </summary>
        public static async Task ForgetOwners(IServiceProvider services, IEnumerable<(int OwnerId, OwnerType OwnerType)> owners)
        {
            var memo = services.GetRequiredService<ReconstructionMemo>();

            foreach (var (ownerId, ownerType) in owners)
            {
                if (ownerType == OwnerType.Team)
                {
                    await memo.HandleAsync(new TeamDataRefreshed(ownerId), CancellationToken.None);
                }
                else
                {
                    await memo.HandleAsync(new PortfolioFeaturesRefreshed(ownerId), CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// The gate lives as long as the host, so an operation a scenario started and never finished would
        /// hold every later scenario's passes off, and so would an operator the fill turned away, who stays
        /// counted as waiting for a minute of the host's clock - which the next scenario's clock reset puts
        /// back to the start. Releasing the one and letting an operator in once, then out again, is how the
        /// gate itself is told both are over.
        /// </summary>
        public static void ReopenTheMaintenanceGate(IServiceProvider services)
        {
            var gate = services.GetRequiredService<DatabaseMaintenanceGate>();

            if (gate.ActiveOperationId is { } stillRunning)
            {
                gate.Release(stillRunning);
            }

            if (!gate.IsMaintenanceOperationWaiting)
            {
                return;
            }

            const string OperationThatClearsTheWaiting = "gate-reset-between-scenarios";
            var attempt = gate.TryAcquire(DatabaseOperationType.Restore, OperationThatClearsTheWaiting);

            Assert.That(attempt.Acquired, Is.True,
                $"The maintenance gate could not be reopened at the end of the scenario: {attempt.BlockedReason}");

            gate.Release(OperationThatClearsTheWaiting);
        }
    }
}
