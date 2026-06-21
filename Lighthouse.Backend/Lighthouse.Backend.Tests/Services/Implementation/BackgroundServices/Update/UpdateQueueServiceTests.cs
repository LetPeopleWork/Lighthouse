using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
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
            subject.EnqueueUpdate(updateStatus.UpdateType, updateStatus.Id, _ => Task.CompletedTask);

            while (!wasNotified)
            {
                await Task.Delay(100);
            }

            Assert.That(wasNotified, Is.True);
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

        private UpdateQueueService CreateSubject()
        {
            return new UpdateQueueService(Mock.Of<ILogger<UpdateQueueService>>(), hubContextMock.Object, new InProcessUpdateStatusStore(updateStatuses), new InProcessUpdateExecutionLock(), new InProcessUpdateCompletionNotifier(), serviceScopeFactoryMock.Object, gate);
        }
    }
}
