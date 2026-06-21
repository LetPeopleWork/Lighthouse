using System.Collections.Concurrent;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class ScalabilityTests
    {
        [Test]
        [Ignore("pending — DELIVER (epic-5305-k8s-readiness slice-07)")]
        public async Task NoRedisOneHost_BehaviourAndCodePathIdenticalToToday()
        {
            await Task.CompletedTask;
            Assert.Fail(
                "Scenario #39 (US-07 AC4 / D1 standalone gate, @standalone). " +
                "Given no ConnectionStrings:Redis is configured and a single host, " +
                "When background updates and manual refreshes run, " +
                "Then the in-process Channel queue, in-process awaiters and InProcessUpdateStatusStore are used — " +
                "behaviour AND code path identical to today (no advisory lock, no Redis), the lock degrades to a no-op. " +
                "Seed: build the host with no Redis connection string; assert the in-process adapters are resolved.");
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
        [Ignore("pending — DELIVER (epic-5305-k8s-readiness slice-07)")]
        public async Task GetUpdateStatus_ConsistentAcrossPods()
        {
            await Task.CompletedTask;
            Assert.Fail(
                "Scenario #43 (US-07 AC3 / INV-2, @requires-docker). " +
                "Given an update in flight admitted on pod A, with the shared Redis status store, " +
                "When GetUpdateStatus is queried on pod B, " +
                "Then pod B returns a consistent (bounded-stale) answer for that UpdateKey, and an EnqueueAndAwaitAsync " +
                "caller on pod B is released when pod A advances the status to terminal (cross-pod awaiter via Redis pub/sub). " +
                "Seed: PostgresContainerFixture + RedisContainerFixture; two WebApplicationFactory<Program> hosts; await on the follower pod.");
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

            return new UpdateQueueService(
                Mock.Of<ILogger<UpdateQueueService>>(),
                hubContext.Object,
                sharedStatusStore,
                executionLock,
                serviceScopeFactory.Object,
                maintenanceGate);
        }
    }
}
