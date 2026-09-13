using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// Epic 5733 slice 01c (ADO #5980) - what the part that sends promises, at its own seam.
    ///
    /// Not through HTTP like the scenarios beside it, and deliberately: the integration host removes
    /// every background service, so nothing there can say what this does when the thing it calls
    /// throws, or what it leaves behind when it is asked to stop. Those are the two promises that
    /// keep a usage event from becoming either a way to stop the feature for the lifetime of a
    /// process or a way to send after somebody's last chance to withdraw.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataForwardingServiceTests
    {
        private const string APresentedToken = "a-token-a-browser-presented";

        // What every batch handed in below carries, and therefore what the day's allowance is
        // charged for one of them.
        private const int OneEventPerBatch = 1;

        /// <summary>
        /// A collector that is down would otherwise stop usage data for as long as the process runs,
        /// with nothing failing and nobody told - the loop would end on the first exception and
        /// never be entered again.
        /// </summary>
        [Test]
        public async Task AWayOutThatThrowsEveryTime_DoesNotEndTheSending()
        {
            var queue = AQueue();
            var throwing = new ThrowingPublisher();
            var forwarder = AForwarder(queue, throwing, AGateThatAgrees());

            HandIn(queue, howMany: 3);
            await forwarder.SendWhatIsWaitingAsync(CancellationToken.None);

            HandIn(queue, howMany: 1);
            await forwarder.SendWhatIsWaitingAsync(CancellationToken.None);

            Assert.That(throwing.Attempts, Is.EqualTo(4),
                "sending stopped at the first failure, so one unreachable collector has switched the "
                + "feature off until somebody restarts the instance");
        }

        /// <summary>
        /// There is no second attempt, by design: a retry is another chance to send something whose
        /// consent may have changed since the first.
        /// </summary>
        [Test]
        public async Task OneBatchHandedIn_IsOneAttemptToSendAndNoMore()
        {
            var queue = AQueue();
            var counting = new CountingPublisher();
            var forwarder = AForwarder(queue, counting, AGateThatAgrees());

            HandIn(queue, howMany: 1);
            await forwarder.SendWhatIsWaitingAsync(CancellationToken.None);
            var afterTheFirstPass = counting.Attempts;

            await forwarder.SendWhatIsWaitingAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterTheFirstPass, Is.EqualTo(1),
                    "one batch was handed in and the collector heard about it a different number of "
                    + "times, so what it counts is not what people did");
                Assert.That(counting.Attempts, Is.EqualTo(1),
                    "the batch was sent a second time, which is a second chance to send something "
                    + "the person behind it may have withdrawn in between");
            }
        }

        /// <summary>
        /// Emptying the queue on the way out would mean sending after the last moment anybody could
        /// withdraw, and would put a third party's latency on a container's shutdown path.
        /// </summary>
        [Test]
        public async Task StoppingWithABatchStillWaiting_SendsNothingOnTheWayOut()
        {
            var queue = AQueue();
            var counting = new CountingPublisher();
            var forwarder = AForwarder(queue, counting, AGateThatAgrees());

            HandIn(queue, howMany: 1);
            await forwarder.StopAsync(CancellationToken.None);

            Assert.That(counting.Attempts, Is.Zero,
                "what was still waiting was flushed on shutdown, so this instance sent after the "
                + "last moment its owner could have stopped it");
        }

        /// <summary>
        /// The queue is bounded, and the boundary has to give way rather than the request. A browser
        /// is waiting on the call that hands a batch in, so waiting for room is how one instance
        /// being unable to reach a third party turns into the product feeling slow.
        /// </summary>
        [Test]
        public void MoreHandedInThanCanWait_IsDroppedRatherThanHeldUp()
        {
            var queue = AQueue();
            var morePerHand = 50;

            var everythingWasTakenIn = Task
                .Run(() => HandIn(queue, UsageDataEventQueue.MostThatCanWait + morePerHand))
                .Wait(TimeSpan.FromSeconds(5));

            var waiting = 0;
            while (queue.TryTakeNext(out _))
            {
                waiting++;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(everythingWasTakenIn, Is.True,
                    "handing a batch in waited for room, so a collector that stops answering would "
                    + "be felt by every browser that has ever agreed");
                Assert.That(waiting, Is.EqualTo(UsageDataEventQueue.MostThatCanWait),
                    "the bound was not the bound: more was kept than may be kept, on an instance "
                    + "that needs its memory for the job somebody actually runs it for");
            }
        }

        /// <summary>
        /// The allowance is taken before the call and this is the only thing that puts it back. A
        /// collector that is down for an hour would otherwise spend a whole day of it on batches
        /// that never arrived, and this instance would stay quiet until midnight even after the
        /// collector came back - with the only line about it saying the allowance was spent, which
        /// reads as a busy day rather than an outage.
        /// </summary>
        [Test]
        public async Task AWayOutThatThrows_PutsTheAllowanceBackAndSaysWhy()
        {
            var queue = AQueue();
            var gate = AGateThatAgrees();
            var forwarder = AForwarder(queue, new ThrowingPublisher(), gate);

            HandIn(queue, howMany: 1);
            await forwarder.SendWhatIsWaitingAsync(CancellationToken.None);

            Mock.Get(gate).Verify(
                giving => giving.GiveBackWhatCouldNotBeSent(OneEventPerBatch, It.IsAny<Exception>()),
                Times.Once,
                "what was charged for a batch that never arrived was not given back, so an "
                + "unreachable collector spends the day's allowance on nothing and nobody is told");
        }

        /// <summary>
        /// The scenarios above pull the drain by hand, because the test host runs no background work
        /// - which leaves the loop that actually does the pulling in production unexercised by all of
        /// them. A loop that never runs is the whole feature switched off in a way nothing else here
        /// can see: every browser keeps agreeing, keeps handing things in, and the queue simply fills
        /// until it starts dropping.
        /// </summary>
        [Test]
        public async Task ABatchWaitingWhenTheServiceStarts_IsSentWithoutAnybodyPullingTheDrain()
        {
            var queue = AQueue();
            var sending = new TaskCompletionSource();
            var forwarder = AForwarder(queue, new AnnouncingPublisher(sending), AGateThatAgrees());

            HandIn(queue, howMany: 1);
            await forwarder.StartAsync(CancellationToken.None);

            try
            {
                await sending.Task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            }
            catch (TimeoutException)
            {
                Assert.Fail(
                    "nothing was sent while the service was running, so on a real instance usage "
                    + "data stops at the queue and the feature is off with nobody told");
            }
            finally
            {
                await forwarder.StopAsync(CancellationToken.None);
            }
        }

        private static UsageDataEventQueue AQueue()
        {
            return new UsageDataEventQueue(Mock.Of<ILogger<UsageDataEventQueue>>());
        }

        private static IUsageDataGate AGateThatAgrees()
        {
            var gate = new Mock<IUsageDataGate>();
            gate.Setup(agreeing => agreeing.RequestPermitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsageDataEmitPermit("a-pseudonym-this-browser-is-counted-under"));

            // Agreeing to both questions: still consenting, and the day's allowance has room. The
            // scenarios here are about what sending does when a collector misbehaves, so a gate that
            // withheld either answer would make them pass for the wrong reason.
            gate.Setup(agreeing => agreeing.RequestPermitToSendAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsageDataEmitPermit("a-pseudonym-this-browser-is-counted-under"));

            return gate.Object;
        }

        private static UsageDataForwardingService AForwarder(
            IUsageDataEventQueue queue, IUsageDataPublisher wayOut, IUsageDataGate gate)
        {
            return new UsageDataForwardingService(
                queue, gate, [wayOut], Mock.Of<ILogger<UsageDataForwardingService>>());
        }

        private static void HandIn(UsageDataEventQueue queue, int howMany)
        {
            for (var handed = 0; handed < howMany; handed++)
            {
                queue.HandIn(new AcceptedUsageDataBatch(
                    APresentedToken,
                    [new UsageDataEventReported(
                        UsageDataEventName.TeamTabOpened,
                        UsageDataRouteKey.TeamDetail_Metrics,
                        WorkTrackingSystem: null,
                        OffsetMs: 0,
                        Sequence: handed)]));
            }
        }

        private sealed class CountingPublisher : IUsageDataPublisher
        {
            public int Attempts { get; private set; }

            public Task PublishAsync(
                UsageDataEmitPermit permit, AcceptedUsageDataBatch batch, CancellationToken cancellationToken)
            {
                Attempts++;
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Says when it has been called, so a scenario about the background loop waits on the thing
        /// it is actually about rather than on a length of time somebody guessed.
        /// </summary>
        private sealed class AnnouncingPublisher(TaskCompletionSource sending) : IUsageDataPublisher
        {
            public Task PublishAsync(
                UsageDataEmitPermit permit, AcceptedUsageDataBatch batch, CancellationToken cancellationToken)
            {
                sending.TrySetResult();
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingPublisher : IUsageDataPublisher
        {
            public int Attempts { get; private set; }

            public Task PublishAsync(
                UsageDataEmitPermit permit, AcceptedUsageDataBatch batch, CancellationToken cancellationToken)
            {
                Attempts++;
                throw new HttpRequestException("the collector could not be reached");
            }
        }
    }
}
