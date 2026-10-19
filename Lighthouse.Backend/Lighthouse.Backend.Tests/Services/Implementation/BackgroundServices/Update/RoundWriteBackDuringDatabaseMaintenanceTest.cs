using System.Collections.Concurrent;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// When the last work out of a refresh round leaves without running, the queue sends the round's
    /// write-back itself, after that work's key has already left the status store. A backup, restore or
    /// clear replaces the database, so it must neither start while that write is still owed nor see the
    /// write happen once it has started.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-05")]
    [Category("error")]
    public class RoundWriteBackDuringDatabaseMaintenanceTest
    {
        private static readonly TimeSpan TheLongestAWaitTakes = TimeSpan.FromSeconds(10);

        private static readonly WorkTrackingSystemConnection Connection = new() { Id = 42, Name = "Write-back", WorkTrackingSystem = WorkTrackingSystems.Jira };

        private static readonly WriteBackFieldUpdate AForecastDate = new() { WorkItemId = "PLAT-1", TargetFieldReference = "customfield_10042", Value = "2026-11-30" };

        private InProcessUpdateStatusStore store = null!;

        private DatabaseMaintenanceGate gate = null!;

        private WriteBackRoundContext roundContext = null!;

        private WriteBackThatWaits writeBack = null!;

        private UpdateQueueService queue = null!;

        [SetUp]
        public void SetUp()
        {
            store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            gate = new DatabaseMaintenanceGate(store);
            roundContext = new WriteBackRoundContext();
            writeBack = new WriteBackThatWaits();
            queue = AQueue();
        }

        [TearDown]
        public void LetGoOfTheQueue()
        {
            writeBack.Finish();
            queue.Dispose();
        }

        [Test]
        public async Task A_restore_cannot_take_the_database_while_a_round_left_by_cancelled_work_is_still_being_written_back()
        {
            queue.EnqueueUpdate(UpdateType.Features, 1, async _ =>
            {
                var round = roundContext.Current!;
                round.Stage(Connection, [AForecastDate]);
                queue.EnqueueUpdate(UpdateType.Forecasts, 1, _ => Task.CompletedTask);
                round.Leave();

                await queue.CancelAllAsync();
            });

            var writing = await writeBack.HasStarted();
            var restore = gate.TryAcquire(DatabaseOperationType.Restore, "restore-while-writing");
            writeBack.Finish();
            var freedOnceWritten = await TheGateFrees();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writing, Is.True, "The round's write-back was never sent, so there was nothing for the restore to wait for.");
                Assert.That(restore.Acquired, Is.False,
                    "The restore took the database while the round's write-back was still being sent to the tracker and saved.");
                Assert.That(freedOnceWritten, Is.True, "The database stayed blocked after the round's write-back had finished.");
            }
        }

        [Test]
        public async Task A_round_let_go_after_a_restore_took_the_database_writes_nothing_back()
        {
            var restoreTook = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var theTeam = new UpdateKey(UpdateType.Team, 2);
            var goAhead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            writeBack.Finish();

            queue.EnqueueUpdate(UpdateType.Features, 2, async _ =>
            {
                await goAhead.Task;

                var round = roundContext.Current!;
                round.Stage(Connection, [AForecastDate]);
                queue.HoldUntilQueuedWorkClears(new UpdateKey(UpdateType.Forecasts, 2), [theTeam], () =>
                {
                    restoreTook.TrySetResult(gate.TryAcquire(DatabaseOperationType.Restore, "restore-before-the-release").Acquired);
                    queue.EnqueueUpdate(UpdateType.Forecasts, 2, _ => Task.CompletedTask);
                });
                round.Leave();
            });
            queue.EnqueueUpdate(theTeam.UpdateType, theTeam.Id, _ => Task.CompletedTask);
            goAhead.TrySetResult();

            var tookTheDatabase = await restoreTook.Task.WaitAsync(TheLongestAWaitTakes);
            await queue.DrainAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tookTheDatabase, Is.True, "The restore never took the database, so there was nothing for the write-back to stand down for.");
                Assert.That(writeBack.WasAsked, Is.False,
                    "The round's write-back was sent to the tracker and saved while a restore was replacing the database.");
            }
        }

        private async Task<bool> TheGateFrees()
        {
            var deadline = DateTime.UtcNow + TheLongestAWaitTakes;

            while (DateTime.UtcNow < deadline)
            {
                if (!gate.IsBlocked)
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return false;
        }

        private UpdateQueueService AQueue()
        {
            var hubContext = new Mock<IHubContext<UpdateNotificationHub>>();
            hubContext.Setup(context => context.Clients.Group(It.IsAny<string>())).Returns(new Mock<IClientProxy>().Object);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(provider => provider.GetService(typeof(IWriteBackService))).Returns(writeBack);
            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(scope => scope.ServiceProvider).Returns(serviceProvider.Object);
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory.Setup(factory => factory.CreateScope()).Returns(serviceScope.Object);

            return new UpdateQueueService(
                Mock.Of<ILogger<UpdateQueueService>>(),
                hubContext.Object,
                new UpdateSubstrate(store, new InProcessUpdateExecutionLock(), new InProcessUpdateCompletionNotifier(), new InProcessUpdateCancellationNotifier()),
                serviceScopeFactory.Object,
                gate,
                roundContext,
                new UpdateCancellationContext());
        }

        /// <summary>Stands in for the tracker: it says when it was asked, and answers only when told to.</summary>
        private sealed class WriteBackThatWaits : IWriteBackService
        {
            private readonly TaskCompletionSource asked = new(TaskCreationOptions.RunContinuationsAsynchronously);

            private readonly TaskCompletionSource answer = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool WasAsked => asked.Task.IsCompleted;

            public async Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
            {
                asked.TrySetResult();
                await answer.Task;
                return new WriteBackResult();
            }

            public async Task<bool> HasStarted()
            {
                return await Task.WhenAny(asked.Task, Task.Delay(TheLongestAWaitTakes)) == asked.Task;
            }

            public void Finish() => answer.TrySetResult();
        }
    }
}
