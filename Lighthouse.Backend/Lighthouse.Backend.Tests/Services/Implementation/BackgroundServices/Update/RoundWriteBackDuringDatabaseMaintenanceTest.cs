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

        private static readonly RefreshRoundSummary TheRefresh = new("Portfolio", "Checkout", 1200, true);

        private const string TheRefreshLine = "Update completed | Portfolio 'Checkout' | duration=1200ms | success=True";

        private static readonly WriteBackFieldUpdate AForecastDate = new() { WorkItemId = "PLAT-1", TargetFieldReference = "customfield_10042", Value = "2026-11-30" };

        private InProcessUpdateStatusStore store = null!;

        private DatabaseMaintenanceGate gate = null!;

        private WriteBackRoundContext roundContext = null!;

        private WriteBackThatWaits writeBack = null!;

        private UpdateQueueService queue = null!;

        private Mock<ILogger<UpdateQueueService>> log = null!;

        [SetUp]
        public void SetUp()
        {
            log = new Mock<ILogger<UpdateQueueService>>();
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

        [Test]
        public async Task What_a_round_let_go_by_cancelled_work_resolved_is_reported_in_one_line_once_its_write_back_is_sent()
        {
            queue.EnqueueUpdate(UpdateType.Features, 3, async _ =>
            {
                var round = roundContext.Current!;
                round.Stage(Connection, [AForecastDate]);
                round.ReportRefresh(TheRefresh);
                queue.EnqueueUpdate(UpdateType.Forecasts, 3, _ => Task.CompletedTask);
                round.Leave();

                await queue.CancelAllAsync();
            });

            var writing = await writeBack.HasStarted();
            writeBack.Finish();
            await queue.DrainAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(writing, Is.True, "The round's write-back was never sent.");
                Assert.That(Wrote(LogLevel.Information, TheRefreshLine), Is.True,
                    "The round's write-back was sent by the queue, but nobody said how the round went.");
            }
        }

        [Test]
        public async Task A_round_whose_write_back_fails_after_its_work_was_let_go_still_frees_the_database_and_says_how_it_went()
        {
            var asked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var failingWriteBack = new Mock<IWriteBackService>();
            failingWriteBack
                .Setup(service => service.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .Callback(() => asked.TrySetResult())
                .ThrowsAsync(new HttpRequestException("the tracker hung up"));
            queue.Dispose();
            queue = AQueue(failingWriteBack.Object);

            queue.EnqueueUpdate(UpdateType.Features, 4, async _ =>
            {
                var round = roundContext.Current!;
                round.Stage(Connection, [AForecastDate]);
                round.ReportRefresh(TheRefresh);
                queue.EnqueueUpdate(UpdateType.Forecasts, 4, _ => Task.CompletedTask);
                round.Leave();

                await queue.CancelAllAsync();
            });

            await asked.Task.WaitAsync(TheLongestAWaitTakes);
            await queue.DrainAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gate.IsBlocked, Is.False, "A write-back that failed kept the database blocked for good.");
                Assert.That(Wrote(LogLevel.Error, "Writing back what a refresh round had resolved failed after its last execution left without running: the tracker hung up"), Is.True,
                    "A write-back that failed was not reported, so an operator cannot tell the tracker never got the round's values.");
                Assert.That(Wrote(LogLevel.Information, TheRefreshLine), Is.True, "A round whose write-back failed still has to say how it went.");
            }
        }

        [Test]
        public async Task A_round_let_go_while_a_restore_holds_the_database_says_it_was_not_written_back_and_how_it_went()
        {
            var restoreTook = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var theTeam = new UpdateKey(UpdateType.Team, 5);
            var goAhead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            writeBack.Finish();

            queue.EnqueueUpdate(UpdateType.Features, 5, async _ =>
            {
                await goAhead.Task;

                var round = roundContext.Current!;
                round.Stage(Connection, [AForecastDate]);
                round.ReportRefresh(TheRefresh);
                queue.HoldUntilQueuedWorkClears(new UpdateKey(UpdateType.Forecasts, 5), [theTeam], () =>
                {
                    restoreTook.TrySetResult(gate.TryAcquire(DatabaseOperationType.Restore, "restore-before-the-release").Acquired);
                    queue.EnqueueUpdate(UpdateType.Forecasts, 5, _ => Task.CompletedTask);
                });
                round.Leave();
            });
            queue.EnqueueUpdate(theTeam.UpdateType, theTeam.Id, _ => Task.CompletedTask);
            goAhead.TrySetResult();

            var tookTheDatabase = await restoreTook.Task.WaitAsync(TheLongestAWaitTakes);
            await queue.DrainAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tookTheDatabase, Is.True, "The restore never took the database.");
                Assert.That(Wrote(LogLevel.Warning, "What a refresh round resolved was not written back, because a database Restore operation is active."), Is.True,
                    "Values the round resolved were dropped without a word.");
                Assert.That(Wrote(LogLevel.Information, TheRefreshLine), Is.True, "A round whose write-back was dropped still has to say how it went.");
            }
        }

        [Test]
        public async Task Work_let_go_as_Lighthouse_shuts_down_does_not_leave_the_database_blocked()
        {
            await queue.DrainAsync();

            queue.EnqueueUpdate(UpdateType.Team, 6, _ => Task.CompletedTask);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gate.IsBlocked, Is.False,
                    "A write-back that was never sent is still counted as owed, so no backup, restore or clear could ever start.");
                Assert.That(Wrote(LogLevel.Warning, "What a refresh round resolved was not written back, because Lighthouse is shutting down."), Is.True);
            }
        }

        private bool Wrote(LogLevel level, string message)
            => log.Invocations.Any(call => call.Method.Name == nameof(ILogger.Log)
                && Equals(call.Arguments[0], level)
                && call.Arguments[2]?.ToString() == message);

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

        private UpdateQueueService AQueue() => AQueue(writeBack);

        private UpdateQueueService AQueue(IWriteBackService service)
        {
            var hubContext = new Mock<IHubContext<UpdateNotificationHub>>();
            hubContext.Setup(context => context.Clients.Group(It.IsAny<string>())).Returns(new Mock<IClientProxy>().Object);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(provider => provider.GetService(typeof(IWriteBackService))).Returns(service);
            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(scope => scope.ServiceProvider).Returns(serviceProvider.Object);
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory.Setup(factory => factory.CreateScope()).Returns(serviceScope.Object);

            return new UpdateQueueService(
                log.Object,
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
