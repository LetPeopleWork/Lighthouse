using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    [TestFixture]
    public class UpdateQueueServiceTests
    {
        private Mock<IHubContext<UpdateNotificationHub>> hubContextMock;
        private Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private Mock<IClientProxy> clientProxyMock;

        private ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;
        private DatabaseMaintenanceGate gate;

        [SetUp]
        public void SetUp()
        {
            hubContextMock = new Mock<IHubContext<UpdateNotificationHub>>();
            clientProxyMock = new Mock<IClientProxy>();
            hubContextMock.Setup(ctx => ctx.Clients.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

            serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(serviceScopeMock.Object);
            serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

            updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
            gate = new DatabaseMaintenanceGate(new InProcessUpdateStatusStore(updateStatuses));
        }

        [Test]
        public void EnqueueUpdate_NewUpdate_QueuesUpdate()
        {
            var updateType = UpdateType.Team;
            var id = 1;

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateType, id, _ => Task.Delay(300));

            Assert.That(updateStatuses.ContainsKey(new UpdateKey(updateType, id)), Is.True);
        }

        [Test]
        public void EnqueueUpdate_ExistingUpdate_DoesNotQueueUpdate()
        {
            var updateType = UpdateType.Team;
            var id = 1;

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateType, id, _ => Task.Delay(300));
            subject.EnqueueUpdate(updateType, id, _ => Task.CompletedTask);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updateStatuses.ContainsKey(new UpdateKey(updateType, id)), Is.True);
                Assert.That(updateStatuses, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task EnqueueUpdate_ConcurrentSameKey_OnlyOneQueued()
        {
            var updateType = UpdateType.Features;
            var id = 42;
            var updateKey = new UpdateKey(updateType, id);

            var gateSource = new TaskCompletionSource();
            var executionCount = 0;

            var subject = CreateSubject();

            Parallel.For(0, 128, _ =>
            {
                subject.EnqueueUpdate(updateType, id, async _ =>
                {
                    Interlocked.Increment(ref executionCount);
                    await gateSource.Task;
                });
            });

            Assert.That(updateStatuses, Has.Count.EqualTo(1), "Concurrent EnqueueUpdate for same key must dedupe to a single status entry");

            gateSource.SetResult();

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline && !updateStatuses.IsEmpty)
            {
                await Task.Delay(20);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updateStatuses.ContainsKey(updateKey), Is.False, "Update key should be cleared after the single task completes");
                Assert.That(executionCount, Is.EqualTo(1), "Exactly one task should have executed; concurrent dedupe must prevent double-execution");
            }
        }

        [Test]
        public async Task EnqueueUpdate_ExecutesUpdateEventually()
        {
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };
            var updateKey = new UpdateKey(updateStatus.UpdateType, updateStatus.Id);

            bool wasExecuted = false;

            var updateTask = new Func<IServiceProvider, Task>(_ =>
            {
                wasExecuted = true;
                return Task.CompletedTask;
            });

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateStatus.UpdateType, updateStatus.Id, updateTask);

            while (!wasExecuted)
            {
                await Task.Delay(100);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wasExecuted, Is.True);
                Assert.That(updateStatuses.ContainsKey(updateKey), Is.False);
            }
        }

        [Test]
        public async Task EnqueueUpdate_NotifiesAboutQueuedOrInProgress()
        {
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };
            var updateKey = new UpdateKey(updateStatus.UpdateType, updateStatus.Id);

            var wasNotified = false;

            clientProxyMock.Setup(client => client.SendCoreAsync(updateKey.ToString(), It.IsAny<object[]>(), default)).Callback((string key, object[] parameters, CancellationToken token) =>
            {
                updateStatus = (UpdateStatus)parameters[0];
                wasNotified = updateStatus.Status == UpdateProgress.Queued || updateStatus.Status == UpdateProgress.InProgress;
            });

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateKey.UpdateType, updateKey.Id, _ => Task.CompletedTask);

            while (!wasNotified)
            {
                await Task.Delay(100);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wasNotified, Is.True);
                Assert.That(updateStatus.UpdateType, Is.EqualTo(updateKey.UpdateType),
                    "The notified payload must carry the queued item's own type, not a default, so listeners route it to the right entity.");
                Assert.That(updateStatus.Id, Is.EqualTo(updateKey.Id),
                    "The notified payload must carry the queued item's own id, not a default.");
            }
        }

        [Test]
        public async Task EnqueueUpdate_NotifiesAboutCompletion()
        {
            var updateStatus = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };
            var updateKey = new UpdateKey(updateStatus.UpdateType, updateStatus.Id);

            var wasNotified = false;

            clientProxyMock.Setup(client => client.SendCoreAsync(updateKey.ToString(), It.IsAny<object[]>(), default)).Callback((string key, object[] parameters, CancellationToken token) =>
            {
                updateStatus = (UpdateStatus)parameters[0];
                wasNotified = updateStatus.Status == UpdateProgress.Completed;
            });

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateStatus.UpdateType, updateStatus.Id, _ => Task.CompletedTask);

            while (!wasNotified)
            {
                await Task.Delay(100);
            }

            Assert.That(wasNotified, Is.True);
        }

        [Test]
        public async Task ExecuteUpdateAsync_UpdateTaskFails_NotifiesAboutFailureAsync()
        {
            var updateKey = new UpdateKey(UpdateType.Team, 1);
            var updateStatus = new UpdateStatus { UpdateType = updateKey.UpdateType, Id = updateKey.Id, Status = UpdateProgress.Queued };

            var updateTask = new Func<IServiceProvider, Task>(_ => throw new Exception("Test exception"));

            var wasNotified = false;

            clientProxyMock.Setup(client => client.SendCoreAsync(updateKey.ToString(), It.IsAny<object[]>(), default)).Callback((string key, object[] parameters, CancellationToken token) =>
            {
                updateStatus = (UpdateStatus)parameters[0];
                wasNotified = updateStatus.Status == UpdateProgress.Failed;
            });

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateStatus.UpdateType, updateStatus.Id, updateTask);

            while (!wasNotified)
            {
                await Task.Delay(100);
            }

            Assert.That(wasNotified, Is.True);
        }

        [Test]
        public async Task EnqueueUpdate_SendsGlobalNotificationOnQueued()
        {
            var updateType = UpdateType.Team;
            var id = 1;
            var globalNotificationSent = false;

            clientProxyMock.Setup(client => client.SendCoreAsync("GlobalUpdateNotification", It.IsAny<object[]>(), default))
                .Callback(() => globalNotificationSent = true);

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateType, id, _ => Task.CompletedTask);

            while (!globalNotificationSent)
            {
                await Task.Delay(100);
            }

            clientProxyMock.Verify(client => client.SendCoreAsync("GlobalUpdateNotification", It.IsAny<object[]>(), default), Times.AtLeastOnce);
        }

        [Test]
        public async Task EnqueueUpdate_SendsGlobalNotificationOnCompletion()
        {
            var updateType = UpdateType.Team;
            var id = 1;
            var globalNotificationCount = 0;

            clientProxyMock.Setup(client => client.SendCoreAsync("GlobalUpdateNotification", It.IsAny<object[]>(), default))
                .Callback(() => Interlocked.Increment(ref globalNotificationCount));

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateType, id, _ => Task.CompletedTask);

            // Wait for both queued and completed notifications
            while (globalNotificationCount < 2)
            {
                await Task.Delay(100);
            }

            // Should be called at least twice: once when queued, once when completed
            Assert.That(globalNotificationCount, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public async Task EnqueueUpdate_SendsGlobalNotificationOnFailure()
        {
            var updateType = UpdateType.Team;
            var id = 1;
            var globalNotificationCount = 0;

            clientProxyMock.Setup(client => client.SendCoreAsync("GlobalUpdateNotification", It.IsAny<object[]>(), default))
                .Callback(() => Interlocked.Increment(ref globalNotificationCount));

            var subject = CreateSubject();
            subject.EnqueueUpdate(updateType, id, _ => throw new Exception("Test failure"));

            // Wait for both queued and failed notifications
            while (globalNotificationCount < 2)
            {
                await Task.Delay(100);
            }

            // Should be called at least twice: once when queued, once when failed
            Assert.That(globalNotificationCount, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void EnqueueUpdate_DatabaseBackupActive_DoesNotQueueUpdate()
        {
            gate.TryAcquire(DatabaseOperationType.Backup, "db-op-1");

            var subject = CreateSubject();
            subject.EnqueueUpdate(UpdateType.Team, 1, _ => Task.CompletedTask);

            Assert.That(updateStatuses.ContainsKey(new UpdateKey(UpdateType.Team, 1)), Is.False);
        }

        [Test]
        public void EnqueueUpdate_DatabaseRestoreActive_DoesNotQueueUpdate()
        {
            gate.TryAcquire(DatabaseOperationType.Restore, "db-op-2");

            var subject = CreateSubject();
            subject.EnqueueUpdate(UpdateType.Features, 5, _ => Task.CompletedTask);

            Assert.That(updateStatuses.ContainsKey(new UpdateKey(UpdateType.Features, 5)), Is.False);
        }

        [Test]
        public void EnqueueUpdate_DatabaseClearActive_DoesNotQueueUpdate()
        {
            gate.TryAcquire(DatabaseOperationType.Clear, "db-op-3");

            var subject = CreateSubject();
            subject.EnqueueUpdate(UpdateType.Forecasts, 3, _ => Task.CompletedTask);

            Assert.That(updateStatuses.ContainsKey(new UpdateKey(UpdateType.Forecasts, 3)), Is.False);
        }

        [Test]
        public void EnqueueUpdate_DatabaseOperationReleased_QueuesUpdate()
        {
            gate.TryAcquire(DatabaseOperationType.Backup, "db-op-1");
            gate.Release("db-op-1");

            var subject = CreateSubject();
            subject.EnqueueUpdate(UpdateType.Team, 1, _ => Task.Delay(300));

            Assert.That(updateStatuses.ContainsKey(new UpdateKey(UpdateType.Team, 1)), Is.True);
        }

        [Test]
        public async Task EnqueueAndAwaitAsync_ReturnedTaskCompletesAfterWorkRuns()
        {
            var counter = 0;

            var subject = CreateSubject();

            var completion = subject.EnqueueAndAwaitAsync(UpdateType.PortfolioDelete, 7, _ =>
            {
                Interlocked.Increment(ref counter);
                return Task.CompletedTask;
            });

            await completion;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(counter, Is.EqualTo(1));
                Assert.That(completion.IsCompletedSuccessfully, Is.True);
            }
        }

        [Test]
        public async Task EnqueueAndAwaitAsync_SameKeyConcurrent_BothAwaitersShareSameTask()
        {
            var gateSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var executionCount = 0;

            var subject = CreateSubject();

            var first = subject.EnqueueAndAwaitAsync(UpdateType.PortfolioDelete, 11, async _ =>
            {
                Interlocked.Increment(ref executionCount);
                await gateSource.Task;
            });

            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow < deadline && executionCount == 0)
            {
                await Task.Delay(10);
            }

            var second = subject.EnqueueAndAwaitAsync(UpdateType.PortfolioDelete, 11, _ =>
            {
                Interlocked.Increment(ref executionCount);
                return Task.CompletedTask;
            });

            Assert.That(second.IsCompleted, Is.False, "Second awaiter must not complete while first work is still gated");

            gateSource.SetResult(true);

            await Task.WhenAll(first, second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(executionCount, Is.EqualTo(1), "Inner work must run exactly once for two concurrent same-key calls");
                Assert.That(first.IsCompletedSuccessfully, Is.True);
                Assert.That(second.IsCompletedSuccessfully, Is.True);
            }
        }

        [Test]
        public void EnqueueAndAwaitAsync_WorkThrows_ReturnedTaskFaults()
        {
            var subject = CreateSubject();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await subject.EnqueueAndAwaitAsync(UpdateType.PortfolioDelete, 13, _ =>
                    throw new InvalidOperationException("boom")));
        }

        [Test]
        public void EnqueueAndAwaitAsync_CancellableToken_TokenCancelledWhileGated_ReturnedTaskCancels()
        {
            var workGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var subject = CreateSubject();
            using var cts = new CancellationTokenSource();

            var completion = subject.EnqueueAndAwaitAsync(UpdateType.PortfolioDelete, 31, async _ => await workGate.Task, cts.Token);

            cts.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(async () => await completion,
                "Cancelling the caller's token must surface as a cancelled returned task, so a shutting-down request stops awaiting in-flight work.");

            workGate.SetResult(true);
        }

        [Test]
        public async Task EnqueueAndAwaitAsync_CancellableToken_WorkSucceeds_ReturnedTaskCompletes()
        {
            var subject = CreateSubject();
            using var cts = new CancellationTokenSource();
            var executed = false;

            await subject.EnqueueAndAwaitAsync(UpdateType.PortfolioDelete, 32, _ =>
            {
                executed = true;
                return Task.CompletedTask;
            }, cts.Token);

            Assert.That(executed, Is.True,
                "A cancellable-token caller whose token never fires must still observe successful completion through the cancellation-aware observer.");
        }

        [Test]
        public void EnqueueAndAwaitAsync_CancellableToken_WorkThrows_ReturnedTaskFaults()
        {
            var subject = CreateSubject();
            using var cts = new CancellationTokenSource();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await subject.EnqueueAndAwaitAsync(UpdateType.PortfolioDelete, 33, _ =>
                    throw new InvalidOperationException("boom"), cts.Token),
                "A cancellable-token caller must still propagate the original work fault through the cancellation-aware observer, not swallow it.");
        }

        [Test]
        public async Task DrainAsync_InFlightQueuedUpdate_CompletesBeforeReturning()
        {
            var executed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var subject = CreateSubject();

            subject.EnqueueUpdate(UpdateType.Team, 1, _ =>
            {
                executed.TrySetResult(true);
                return Task.CompletedTask;
            });

            await subject.DrainAsync();

            Assert.That(executed.Task.IsCompletedSuccessfully, Is.True, "Queued update must finish before drain returns");
        }

        [Test]
        public async Task DrainAsync_QueueExceedsTimeout_ReturnsWithinBoundWithoutThrowing()
        {
            using var blocked = new ManualResetEventSlim(false);
            var subject = CreateSubject();

            subject.EnqueueUpdate(UpdateType.Team, 1, _ => Task.Run(() => blocked.Wait()));

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await subject.DrainAsync(cts.Token);
            stopwatch.Stop();
            blocked.Set();

            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)), "Drain must be bounded by the shutdown timeout, not block on a stuck update");
        }

        [Test]
        public void EnqueueAndAwaitAsync_DuplicateKey_DistributedSubstrate_AwaitsCrossPodCompletion()
        {
            var updateKey = new UpdateKey(UpdateType.PortfolioDelete, 21);
            updateStatuses[updateKey] = new UpdateStatus { UpdateType = updateKey.UpdateType, Id = updateKey.Id, Status = UpdateProgress.InProgress };

            Action<UpdateKey>? releaseAwaiter = null;
            var notifier = new Mock<IUpdateCompletionNotifier>();
            notifier.SetupGet(n => n.IsDistributed).Returns(true);
            notifier.Setup(n => n.Subscribe(It.IsAny<Action<UpdateKey>>()))
                .Callback<Action<UpdateKey>>(callback => releaseAwaiter = callback)
                .Returns(Mock.Of<IDisposable>());

            var subject = CreateSubject(notifier.Object);

            var completion = subject.EnqueueAndAwaitAsync(updateKey.UpdateType, updateKey.Id, _ => Task.CompletedTask);

            Assert.That(completion.IsCompleted, Is.False,
                "On a distributed substrate a duplicate-key caller must wait for the owning pod's completion signal, not return immediately.");

            releaseAwaiter!(updateKey);

            Assert.That(completion.IsCompletedSuccessfully, Is.True,
                "The cross-pod awaiter must complete once the completion notifier fans the signal back to this pod.");
        }

        [Test]
        public void EnqueueAndAwaitAsync_DuplicateKey_NonDistributedSubstrate_ReturnsImmediately()
        {
            var updateKey = new UpdateKey(UpdateType.PortfolioDelete, 22);
            updateStatuses[updateKey] = new UpdateStatus { UpdateType = updateKey.UpdateType, Id = updateKey.Id, Status = UpdateProgress.InProgress };

            var notifier = new Mock<IUpdateCompletionNotifier>();
            notifier.SetupGet(n => n.IsDistributed).Returns(false);
            notifier.Setup(n => n.Subscribe(It.IsAny<Action<UpdateKey>>())).Returns(Mock.Of<IDisposable>());

            var subject = CreateSubject(notifier.Object);

            var completion = subject.EnqueueAndAwaitAsync(updateKey.UpdateType, updateKey.Id, _ => Task.CompletedTask);

            Assert.That(completion.IsCompletedSuccessfully, Is.True,
                "In-process there is no other pod to wait on: a duplicate-key caller with no local awaiter must return a completed task rather than hang.");
        }

        [Test]
        public async Task EnqueueUpdate_PublishesCompletionToNotifier()
        {
            var notifier = new Mock<IUpdateCompletionNotifier>();
            notifier.Setup(n => n.Subscribe(It.IsAny<Action<UpdateKey>>())).Returns(Mock.Of<IDisposable>());
            notifier.Setup(n => n.PublishCompletionAsync(It.IsAny<UpdateKey>())).Returns(Task.CompletedTask);

            var subject = CreateSubject(notifier.Object);
            var updateKey = new UpdateKey(UpdateType.Team, 1);
            subject.EnqueueUpdate(updateKey.UpdateType, updateKey.Id, _ => Task.CompletedTask);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline && !updateStatuses.IsEmpty)
            {
                await Task.Delay(20);
            }

            notifier.Verify(n => n.PublishCompletionAsync(updateKey), Times.Once,
                "Every terminal update must publish its completion so cross-pod awaiters on other pods are released.");
        }

        [Test]
        public async Task EnqueueAndAwaitAsync_AfterCompletion_RemovesStatusAndPublishesCompletion()
        {
            var notifier = new Mock<IUpdateCompletionNotifier>();
            notifier.Setup(n => n.Subscribe(It.IsAny<Action<UpdateKey>>())).Returns(Mock.Of<IDisposable>());
            notifier.Setup(n => n.PublishCompletionAsync(It.IsAny<UpdateKey>())).Returns(Task.CompletedTask);

            var subject = CreateSubject(notifier.Object);
            var updateKey = new UpdateKey(UpdateType.PortfolioDelete, 41);

            UpdateStatus? completionPayload = null;
            clientProxyMock.Setup(client => client.SendCoreAsync(updateKey.ToString(), It.IsAny<object[]>(), default))
                .Callback((string _, object[] parameters, CancellationToken _) =>
                {
                    var status = (UpdateStatus)parameters[0];
                    if (status.Status == UpdateProgress.Completed)
                    {
                        completionPayload = status;
                    }
                });

            await subject.EnqueueAndAwaitAsync(updateKey.UpdateType, updateKey.Id, _ => Task.CompletedTask);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline && (updateStatuses.ContainsKey(updateKey) || completionPayload is null))
            {
                await Task.Delay(20);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updateStatuses.ContainsKey(updateKey), Is.False,
                    "The awaitable path must remove the status entry after terminal completion, otherwise the key would be permanently blocked from re-admission.");
                Assert.That(completionPayload, Is.Not.Null,
                    "The awaitable path must notify listeners of the terminal status, so a UI awaiting the delete sees it finish.");
                Assert.That(completionPayload!.UpdateType, Is.EqualTo(updateKey.UpdateType));
                Assert.That(completionPayload.Id, Is.EqualTo(updateKey.Id));
            }

            notifier.Verify(n => n.PublishCompletionAsync(updateKey), Times.Once,
                "The awaitable path must also publish completion so cross-pod awaiters on other pods are released, not only the fire-and-forget path.");
        }

        [Test]
        public void Dispose_DisposesCompletionSubscription()
        {
            var subscription = new Mock<IDisposable>();
            var notifier = new Mock<IUpdateCompletionNotifier>();
            notifier.Setup(n => n.Subscribe(It.IsAny<Action<UpdateKey>>())).Returns(subscription.Object);

            var subject = CreateSubject(notifier.Object);
            subject.Dispose();

            subscription.Verify(s => s.Dispose(), Times.Once,
                "Disposing the queue service must release its completion subscription so the distributed pub/sub channel is unsubscribed cleanly.");
        }

        private UpdateQueueService CreateSubject()
        {
            return CreateSubject(new InProcessUpdateCompletionNotifier());
        }

        private UpdateQueueService CreateSubject(IUpdateCompletionNotifier completionNotifier)
        {
            return new UpdateQueueService(Mock.Of<ILogger<UpdateQueueService>>(), hubContextMock.Object, new InProcessUpdateStatusStore(updateStatuses), new InProcessUpdateExecutionLock(), completionNotifier, serviceScopeFactoryMock.Object, gate);
        }
    }
}
