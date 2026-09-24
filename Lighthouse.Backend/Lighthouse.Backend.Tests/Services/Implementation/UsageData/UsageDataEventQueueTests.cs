using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Tests.Services.Implementation.UsageData
{
    /// <summary>
    /// The memory between a browser handing something in and it being sent. Two of its promises are
    /// invisible from anywhere else: that reaching the ceiling is said out loud rather than passed
    /// over in silence - it is the only sign an operator ever gets that sending has stopped working
    /// - and that being asked to stop is treated as an ordinary end rather than as a failure, which
    /// is what keeps a container shutdown from logging an exception every time.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataEventQueueTests
    {
        private RecordingLogger<UsageDataEventQueue> logger = null!;

        [SetUp]
        public void SetUp()
        {
            logger = new RecordingLogger<UsageDataEventQueue>();
        }

        /// <summary>
        /// Reaching the ceiling means sending has stopped working, because the part that empties this
        /// runs continuously and a browser flushes at most twice a minute. Nothing else notices that:
        /// the browser is answered the same either way by design. So the dropped batch is the one
        /// event here worth a line - and a line written every time a batch is accepted instead would
        /// bury it under one per flush from every browser on the instance.
        /// </summary>
        [Test]
        public void ABatchDroppedBecauseTheQueueIsFull_IsTheOneThingSaidOutLoud()
        {
            var queue = new UsageDataEventQueue(logger);

            for (var handed = 0; handed < UsageDataEventQueue.MostThatCanWait; handed++)
            {
                queue.HandIn(ABatch());
            }

            var saidWhileThereWasStillRoom = logger.Written(LogLevel.Debug).Count;
            queue.HandIn(ABatch());
            var said = logger.Written(LogLevel.Debug);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saidWhileThereWasStillRoom, Is.Zero,
                    "a line was written for a batch that was accepted, so the one line that means "
                    + "something is buried under one per flush from every browser that ever agreed");
                Assert.That(said, Has.Count.EqualTo(1),
                    "a batch was thrown away in silence, so the only sign that sending has stopped "
                    + "working never reaches anybody. Written: " + string.Join(" | ", said));
                Assert.That(said[0], Does.Contain(UsageDataEventQueue.MostThatCanWait.ToString()),
                    "the line does not say how many were already waiting, which is what tells an "
                    + "operator whether the ceiling is too low or the sending is broken");
            }
        }

        /// <summary>
        /// Being asked to stop is not a failure. Treated as one, every container shutdown ends with
        /// an exception in the log that looks like a fault and is not - and whatever is still waiting
        /// is meant to stay where it is rather than be sent on the way out, which would be sending
        /// after the last moment anybody could have withdrawn.
        /// </summary>
        [Test]
        public async Task BeingAskedToStopWhileWaiting_EndsTheWaitRatherThanFailing()
        {
            var queue = new UsageDataEventQueue(logger);
            using var stopping = new CancellationTokenSource();
            await stopping.CancelAsync();

            var somethingIsWaiting = await queue.WaitForSomethingAsync(stopping.Token);

            Assert.That(somethingIsWaiting, Is.False,
                "a queue that was asked to stop reported there is something to send, so the loop "
                + "that reads this goes round again on the way out");
        }

        [Test]
        public async Task ABatchAlreadyWaiting_IsSomethingTheLoopIsToldAbout()
        {
            var queue = new UsageDataEventQueue(logger);
            queue.HandIn(ABatch());

            Assert.That(await queue.WaitForSomethingAsync(CancellationToken.None), Is.True,
                "nothing was reported as waiting while a batch was waiting, so the sending loop "
                + "never wakes and the feature is off with nobody told");
        }

        private static AcceptedUsageDataBatch ABatch()
        {
            return new AcceptedUsageDataBatch(
                "a-token-a-browser-presented",
                [new UsageDataEventReported(
                    UsageDataEventName.TeamTabOpened,
                    UsageDataRouteKey.TeamDetail_Metrics,
                    WorkTrackingSystem: null,
                    OptionalFeature: null,
                    Enabled: null,
                    OffsetMs: 0,
                    Sequence: 0)]);
        }
    }
}
