using System.Collections.Concurrent;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using StackExchange.Redis;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class ScalabilityTests
    {
        [Test]
        public async Task NoRedisOneHost_BehaviourAndCodePathIdenticalToToday()
        {
            await using var host = new TestWebApplicationFactory<Program>();

            var statusStore = host.Services.GetRequiredService<IUpdateStatusStore>();
            var executionLock = host.Services.GetRequiredService<IUpdateExecutionLock>();
            var redisMultiplexer = host.Services.GetService<IConnectionMultiplexer>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(statusStore, Is.TypeOf<InProcessUpdateStatusStore>(),
                    "with no ConnectionStrings:Redis the status store is the in-process ConcurrentDictionary adapter — " +
                    "the same code path as today (US-07 AC4 / D1 standalone gate)");
                Assert.That(executionLock, Is.TypeOf<InProcessUpdateExecutionLock>(),
                    "with no Redis the per-entity lock degrades to a no-op — no advisory lock is acquired");
                Assert.That(redisMultiplexer, Is.Null,
                    "no Redis multiplexer is registered without ConnectionStrings:Redis, so nothing connects to Redis");
            }
        }

        [Test]
        public async Task RedisThreeHosts_SingleSyncPerEntity_TimerAndManualRefresh()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var connectorCalls = new ConcurrentDictionary<UpdateKey, int>();
            var releaseInFlightWork = new TaskCompletionSource();

            var pods = Enumerable.Range(0, 3)
                .Select(_ => CreatePod(multiplexer, postgres.GetConnectionString()))
                .ToList();

            var key = new UpdateKey(UpdateType.Team, 1);
            Task SyncEntity(IServiceProvider _)
            {
                connectorCalls.AddOrUpdate(key, 1, (_, count) => count + 1);
                return releaseInFlightWork.Task;
            }

            var concurrentTriggers = pods
                .SelectMany(pod => new[]
                {
                    Task.Run(() => pod.EnqueueUpdate(UpdateType.Team, 1, SyncEntity)),
                    Task.Run(() => pod.EnqueueUpdate(UpdateType.Team, 1, SyncEntity)),
                })
                .ToArray();

            await Task.WhenAll(concurrentTriggers);
            releaseInFlightWork.SetResult();
            await Task.WhenAll(pods.Select(pod => pod.DrainAsync()));

            Assert.That(connectorCalls.GetValueOrDefault(key), Is.EqualTo(1),
                "with the timer and manual refresh both targeting the same entity concurrently across 3 pods sharing " +
                "one Redis store + Postgres lock, the entity is synced exactly once: HSETNX admission dedups the " +
                "enqueue across the fleet and the per-entity advisory lock is the hard single-active-lifecycle backstop " +
                "(no N× connector calls, no racing writes / US-07 AC1 / INV-4)");
        }

        [Test]
        [Ignore("pending — DELIVER (epic-5305-k8s-readiness slice-07)")]
        public async Task RedisBackplane_NotificationOnPodA_ReachesClientOnPodB()
        {
            await Task.CompletedTask;
            Assert.Fail(
                "Scenario #42 (US-07 AC2, @requires-docker). " +
                "Given 2 hosts wired to one Redis SignalR backplane, with a client connected to pod A, " +
                "When an update notification is raised on pod B, " +
                "Then the client on pod A receives it (cross-pod fan-out via .AddStackExchangeRedis). " +
                "Seed: RedisContainerFixture; two WebApplicationFactory<Program> hosts sharing ConnectionStrings:Redis; a SignalR client against pod A's hub.");
        }

        [Test]
        public async Task GetUpdateStatus_ConsistentAcrossPods()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var podA = CreatePod(multiplexer, postgres.GetConnectionString());
            var podB = CreatePod(multiplexer, postgres.GetConnectionString());

            var key = new UpdateKey(UpdateType.Team, 1);
            var inFlightReached = new TaskCompletionSource();
            var releaseInFlightWork = new TaskCompletionSource();

            podA.EnqueueUpdate(UpdateType.Team, 1, _ =>
            {
                inFlightReached.TrySetResult();
                return releaseInFlightWork.Task;
            });
            await inFlightReached.Task;

            var statusReaderOnPodB = new RedisUpdateStatusStore(multiplexer);
            statusReaderOnPodB.TryGet(key, out var observedOnPodB);

            var crossPodAwait = podB.EnqueueAndAwaitAsync(UpdateType.Team, 1, _ => Task.CompletedTask);
            var releasedWhileInFlight = crossPodAwait.IsCompleted;

            releaseInFlightWork.SetResult();
            var crossPodAwaitReleased = await Task.WhenAny(crossPodAwait, Task.Delay(5000)) == crossPodAwait;

            await podA.DrainAsync();
            await podB.DrainAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observedOnPodB?.Status, Is.EqualTo(UpdateProgress.InProgress),
                    "pod B reads the in-flight status for the UpdateKey from the shared Redis store — GetUpdateStatus " +
                    "is consistent across pods (US-07 AC3, bounded-stale / INV-2)");
                Assert.That(releasedWhileInFlight, Is.False,
                    "pod B's EnqueueAndAwaitAsync for the same entity does not resolve while pod A is still running it");
                Assert.That(crossPodAwaitReleased, Is.True,
                    "pod B's cross-pod await is released once pod A advances the entity to terminal and publishes the " +
                    "completion over the dedicated Redis pub/sub channel");
            }
        }

        private static UpdateQueueService CreatePod(IConnectionMultiplexer multiplexer, string postgresConnectionString)
        {
            var hubContext = new Mock<IHubContext<UpdateNotificationHub>>();
            var clientProxy = new Mock<IClientProxy>();
            clientProxy
                .Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            hubContext.Setup(context => context.Clients.Group(It.IsAny<string>())).Returns(clientProxy.Object);

            var serviceProvider = new Mock<IServiceProvider>();
            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(scope => scope.ServiceProvider).Returns(serviceProvider.Object);
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory.Setup(factory => factory.CreateScope()).Returns(serviceScope.Object);

            var maintenanceGate = new DatabaseMaintenanceGate(
                new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>()));
            var sharedStatusStore = new RedisUpdateStatusStore(multiplexer);
            var executionLock = new PostgresUpdateExecutionLock(
                Options.Create(new DatabaseConfiguration { Provider = "Postgresql", ConnectionString = postgresConnectionString }));
            var completionNotifier = new RedisUpdateCompletionNotifier(multiplexer);

            return new UpdateQueueService(
                Mock.Of<ILogger<UpdateQueueService>>(),
                hubContext.Object,
                sharedStatusStore,
                executionLock,
                completionNotifier,
                serviceScopeFactory.Object,
                maintenanceGate);
        }
    }
}
