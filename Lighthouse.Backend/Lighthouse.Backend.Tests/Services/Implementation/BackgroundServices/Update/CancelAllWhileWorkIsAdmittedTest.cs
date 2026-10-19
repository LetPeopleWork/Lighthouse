using System.Collections.Concurrent;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Cancel all stops what the instance holds when the click arrives. Work that is awaited by its caller
    /// (a refresh asked for from a page) becomes visible on the list the moment it is admitted, so a click
    /// landing in that moment has to find something it can stop. The status store here lets the click land
    /// at exactly that point: just after the work was admitted, before anything else happens to it.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-05")]
    [Category("us-04")]
    [Category("ac-05.1")]
    public class CancelAllWhileWorkIsAdmittedTest
    {
        private static readonly TimeSpan TheLongestTheStopMayTake = TimeSpan.FromSeconds(10);

        private static readonly TimeSpan LongerThanAnyTestWaits = TimeSpan.FromSeconds(30);

        [Test]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Cancel_all_landing_just_as_awaited_work_is_admitted_stops_that_work()
        {
            var cancellationContext = new UpdateCancellationContext();
            var store = new ClickLandsWhileAdmitting(new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc));
            var queue = AQueueOver(store, cancellationContext);
            store.TheClick = () => queue.CancelAllAsync().GetAwaiter().GetResult();

            var stoppedByTheClick = false;
            var awaited = queue.EnqueueAndAwaitAsync(UpdateType.Team, 7, async _ =>
            {
                try
                {
                    await Task.Delay(LongerThanAnyTestWaits, cancellationContext.Current);
                }
                catch (OperationCanceledException)
                {
                    stoppedByTheClick = true;
                    throw;
                }
            });

            var settled = await Task.WhenAny(awaited, Task.Delay(TheLongestTheStopMayTake)) == awaited;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(store.TheClickLanded, Is.True, "The click never landed while the work was being admitted.");
                Assert.That(settled, Is.True,
                    "Work admitted just as Cancel all arrived had no way to be stopped, so it ran on.");
                Assert.That(stoppedByTheClick || awaited.IsCompleted, Is.True);
            }
        }

        [Test]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Two_callers_asking_for_the_same_refresh_at_once_leave_the_one_that_runs_stoppable()
        {
            var cancellationContext = new UpdateCancellationContext();
            var inner = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            var store = new AnotherCallerSlipsInWhileAdmitting(inner);
            using var queue = AQueueOver(store, cancellationContext);
            var key = new UpdateKey(UpdateType.Team, 8);

            var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Func<IServiceProvider, Task> refresh = async _ =>
            {
                try
                {
                    await Task.Delay(LongerThanAnyTestWaits, cancellationContext.Current);
                }
                catch (OperationCanceledException)
                {
                    stopped.TrySetResult();
                    throw;
                }
            };

            store.TheOtherCaller = () => queue.EnqueueUpdate(key.UpdateType, key.Id, refresh);
            queue.EnqueueUpdate(key.UpdateType, key.Id, refresh);

            var running = await TheStatusReaches(inner, key, UpdateProgress.InProgress);
            await queue.CancelAllAsync();

            var stoppedInTime = await Task.WhenAny(stopped.Task, Task.Delay(TheLongestTheStopMayTake)) == stopped.Task;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(store.TheOtherCallerAsked, Is.True, "The second caller never asked while the first was being admitted.");
                Assert.That(running, Is.True, "The refresh never started, so there was nothing running to stop.");
                Assert.That(stoppedInTime, Is.True,
                    "The caller that lost the race threw away the cancellation the running refresh was using, so Cancel all could not stop it.");
            }
        }

        private static async Task<bool> TheStatusReaches(InProcessUpdateStatusStore store, UpdateKey key, UpdateProgress reached)
        {
            var deadline = DateTime.UtcNow + TheLongestTheStopMayTake;

            while (DateTime.UtcNow < deadline)
            {
                if (store.TryGet(key, out var status) && status?.Status == reached)
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return false;
        }

        private static UpdateQueueService AQueueOver(IUpdateStatusStore store, UpdateCancellationContext cancellationContext)
        {
            var hubContext = new Mock<IHubContext<UpdateNotificationHub>>();
            hubContext.Setup(context => context.Clients.Group(It.IsAny<string>())).Returns(new Mock<IClientProxy>().Object);

            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(scope => scope.ServiceProvider).Returns(new Mock<IServiceProvider>().Object);
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory.Setup(factory => factory.CreateScope()).Returns(serviceScope.Object);

            return new UpdateQueueService(
                Mock.Of<ILogger<UpdateQueueService>>(),
                hubContext.Object,
                new UpdateSubstrate(store, new InProcessUpdateExecutionLock(), new InProcessUpdateCompletionNotifier(), new InProcessUpdateCancellationNotifier()),
                serviceScopeFactory.Object,
                new DatabaseMaintenanceGate(new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc)),
                new WriteBackRoundContext(),
                cancellationContext);
        }

        /// <summary>
        /// The production in-process store, with a second caller asking for the same work while the first is
        /// part-way through being admitted. The second caller gets a moment to finish first; if the queue makes
        /// it wait for the first instead, the moment passes and the first carries on.
        /// </summary>
        private sealed class AnotherCallerSlipsInWhileAdmitting(IUpdateStatusStore inner) : IUpdateStatusStore
        {
            private static readonly TimeSpan TheMomentItGets = TimeSpan.FromSeconds(1);

            private int calls;

            public Action TheOtherCaller { get; set; } = () => { };

            public bool TheOtherCallerAsked { get; private set; }

            public bool TryAdmit(UpdateKey key, UpdateStatus status)
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    TheOtherCallerAsked = true;
                    _ = Task.Run(TheOtherCaller).Wait(TheMomentItGets);
                }

                return inner.TryAdmit(key, status);
            }

            public UpdateStatus? Advance(UpdateKey key, UpdateProgress to) => inner.Advance(key, to);

            public UpdateStatus? CancelIfStillWaiting(UpdateKey key) => inner.CancelIfStillWaiting(key);

            public void Requeue(UpdateKey key) => inner.Requeue(key);

            public bool TryGet(UpdateKey key, out UpdateStatus? status) => inner.TryGet(key, out status);

            public void Remove(UpdateKey key) => inner.Remove(key);

            public IReadOnlyList<UpdateStatus> GetAdmittedWork() => inner.GetAdmittedWork();

            public bool HasActiveWork() => inner.HasActiveWork();

            public bool HasQueuedWork(IReadOnlyCollection<UpdateKey> keys) => inner.HasQueuedWork(keys);
        }

        /// <summary>The production in-process store, with Cancel all clicked once, right after the first admission.</summary>
        private sealed class ClickLandsWhileAdmitting(IUpdateStatusStore inner) : IUpdateStatusStore
        {
            public Action TheClick { get; set; } = () => { };

            public bool TheClickLanded { get; private set; }

            public bool TryAdmit(UpdateKey key, UpdateStatus status)
            {
                var admitted = inner.TryAdmit(key, status);

                if (admitted && !TheClickLanded)
                {
                    TheClickLanded = true;
                    TheClick();
                }

                return admitted;
            }

            public UpdateStatus? Advance(UpdateKey key, UpdateProgress to) => inner.Advance(key, to);

            public UpdateStatus? CancelIfStillWaiting(UpdateKey key) => inner.CancelIfStillWaiting(key);

            public void Requeue(UpdateKey key) => inner.Requeue(key);

            public bool TryGet(UpdateKey key, out UpdateStatus? status) => inner.TryGet(key, out status);

            public void Remove(UpdateKey key) => inner.Remove(key);

            public IReadOnlyList<UpdateStatus> GetAdmittedWork() => inner.GetAdmittedWork();

            public bool HasActiveWork() => inner.HasActiveWork();

            public bool HasQueuedWork(IReadOnlyCollection<UpdateKey> keys) => inner.HasQueuedWork(keys);
        }
    }
}
