using System.Collections.Concurrent;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// Cancel all is clicked on whichever replica answers the browser, and the work may be running on
    /// another one, so the click has to travel. Two queues on one real Redis are the only way to see that:
    /// neither a single test host nor an in-process notifier has a second replica for the message to reach.
    /// Running several replicas on Redis is a proof of concept, so this one scenario is all it gets.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-05")]
    [Category("ac-05.6")]
    [Category("requires-docker")]
    [Category("real-io")]
    public class CancelAllMultiReplicaTests
    {
        private static readonly TimeSpan TheLongestTheStopMayTake = TimeSpan.FromSeconds(10);

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Cancel_all_clicked_on_one_replica_stops_the_refresh_running_on_another()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var runningIt = new UpdateCancellationContext();
            using var podRunningTheWork = APod(multiplexer, runningIt);
            using var podTakingTheClick = APod(multiplexer, new UpdateCancellationContext());

            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            podRunningTheWork.EnqueueUpdate(UpdateType.Team, 42, async _ =>
            {
                started.TrySetResult();

                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, runningIt.Current);
                }
                finally
                {
                    stopped.TrySetResult();
                }
            });

            await started.Task.WaitAsync(TheLongestTheStopMayTake);
            await podTakingTheClick.CancelAllAsync();

            Assert.That(async () => await stopped.Task.WaitAsync(TheLongestTheStopMayTake), Throws.Nothing,
                "The replica that took the click holds no token for the work; the refresh on the other replica kept running.");
        }

        private static UpdateQueueService APod(IConnectionMultiplexer multiplexer, UpdateCancellationContext cancellationContext)
        {
            var hubContext = new Mock<IHubContext<UpdateNotificationHub>>();
            var clientProxy = new Mock<IClientProxy>();
            clientProxy
                .Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            hubContext.Setup(context => context.Clients.Group(It.IsAny<string>())).Returns(clientProxy.Object);

            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(scope => scope.ServiceProvider).Returns(new Mock<IServiceProvider>().Object);
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory.Setup(factory => factory.CreateScope()).Returns(serviceScope.Object);

            var maintenanceGate = new DatabaseMaintenanceGate(
                new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc));

            return new UpdateQueueService(
                Mock.Of<ILogger<UpdateQueueService>>(),
                hubContext.Object,
                new UpdateSubstrate(
                    new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc, NullLogger<RedisUpdateStatusStore>.Instance),
                    new InProcessUpdateExecutionLock(),
                    new RedisUpdateCompletionNotifier(multiplexer),
                    new RedisUpdateCancellationNotifier(multiplexer)),
                serviceScopeFactory.Object,
                maintenanceGate,
                new WriteBackRoundContext(),
                cancellationContext);
        }
    }
}
