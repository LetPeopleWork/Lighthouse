using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    /// <summary>
    /// Reading what a browser posted. Everything here is a number or a choice from a closed list,
    /// and every part is optional on the wire because a whole number left out of a message arrives
    /// as zero rather than as absent - so the reader has to establish that a value was actually sent.
    /// The cost of getting that wrong lands on the smallest values there are, which are also the
    /// commonest: the first event of a visit, reported the instant it happened, is zero twice over.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataControllerTests
    {
        private const string APresentedToken = "a-token-a-browser-presented";

        /// <summary>
        /// Zero is a real answer to both questions. An event reported the moment it happened waited
        /// no time at all, and the first event of a visit is the zeroth in its order - so a reader
        /// that requires these to be positive throws away the opening event of every visit anybody
        /// ever makes, and the figures start one event late for everyone.
        /// </summary>
        [TestCase(0, 0, TestName = "AnEventTakenIn(the first one, reported at once)")]
        [TestCase(0, 4, TestName = "AnEventTakenIn(reported at once, later in the visit)")]
        [TestCase(2500, 0, TestName = "AnEventTakenIn(the first one, held on to for a while)")]
        public async Task AnEventCarryingTheSmallestNumbersThereAre_IsStillAnEvent(int offsetMs, int sequence)
        {
            var queue = new UsageDataEventQueue(Mock.Of<ILogger<UsageDataEventQueue>>());
            var controller = AController(queue, ABrowserThatAgreed());

            var answer = await controller.HandInEvents(
                ABatchReporting(offsetMs, sequence), APresentedToken, TestContext.CurrentContext.CancellationToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer, Is.InstanceOf<NoContentResult>(),
                    "a perfectly ordinary event was refused");
                Assert.That(queue.TryTakeNext(out var waiting), Is.True,
                    "the event was accepted and then not kept, so nothing about the opening moment "
                    + "of anybody's visit ever reaches the figures");
                Assert.That(waiting!.Events[0].OffsetMs, Is.EqualTo(offsetMs),
                    "how long the browser held on to the event was not read as sent");
                Assert.That(waiting.Events[0].Sequence, Is.EqualTo(sequence),
                    "where the event sat in the visit was not read as sent");
            }
        }

        /// <summary>
        /// A number no browser of ours can produce means the message was not written by our page, and
        /// there is nothing to be gained from guessing what was meant.
        /// </summary>
        [TestCase(-1, 0, TestName = "AnEventRefused(a delay that ran backwards)")]
        [TestCase(0, -1, TestName = "AnEventRefused(a position before the first)")]
        public async Task AnEventCarryingANumberNoBrowserCouldHaveSent_IsRefusedRatherThanGuessed(
            int offsetMs, int sequence)
        {
            var queue = new UsageDataEventQueue(Mock.Of<ILogger<UsageDataEventQueue>>());
            var controller = AController(queue, ABrowserThatAgreed());

            var answer = await controller.HandInEvents(
                ABatchReporting(offsetMs, sequence), APresentedToken, TestContext.CurrentContext.CancellationToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer, Is.InstanceOf<BadRequestResult>(),
                    "a message our own page could not have written was accepted");
                Assert.That(queue.TryTakeNext(out _), Is.False,
                    "it was refused and kept anyway, so what the collector is told came from "
                    + "something other than this product's own page");
            }
        }

        /// <summary>
        /// The only answer here that says anything back. Our own page is the only thing that posts
        /// to it, so an answer it cannot read is our bug rather than somebody probing - and the two
        /// words it will accept are the whole of what whoever is debugging needs.
        /// </summary>
        [TestCase("maybe")]
        [TestCase("")]
        [TestCase("GRANTED?")]
        public async Task AnAnswerThatIsNeitherOfTheTwo_SaysWhichTwoItWouldHaveTaken(string given)
        {
            var controller = AController(new UsageDataEventQueue(Mock.Of<ILogger<UsageDataEventQueue>>()), null);

            var answer = await controller.RecordDecision(
                new UsageDataConsentRequestDto(given), TestContext.CurrentContext.CancellationToken);

            var refusal = answer.Result as BadRequestObjectResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal, Is.Not.Null, "an answer that is neither of the two was recorded");
                Assert.That(refusal!.Value?.ToString(), Does.Contain("granted").And.Contains("declined"),
                    "the refusal does not name the two answers it would have taken, so whoever is "
                    + "looking at a page that stopped working has nothing to go on");
            }
        }

        private static UsageDataEventBatchDto ABatchReporting(int offsetMs, int sequence)
        {
            return new UsageDataEventBatchDto(
                [new UsageDataEventDto(
                    UsageDataEventName.TeamTabOpened,
                    UsageDataRouteKey.TeamDetail_Metrics,
                    WorkTrackingSystem: null,
                    offsetMs,
                    sequence)]);
        }

        private static UsageDataEmitPermit ABrowserThatAgreed()
        {
            return new UsageDataEmitPermit("a-pseudonym-this-browser-is-counted-under");
        }

        private static UsageDataController AController(IUsageDataEventQueue queue, UsageDataEmitPermit? permit)
        {
            var gate = new Mock<IUsageDataGate>();
            gate.Setup(agreeing => agreeing.RequestPermitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(permit);

            var consents = new Mock<IUsageDataConsentService>();
            consents
                .Setup(service => service.RecordDecisionAsync(
                    It.IsAny<UsageDataDecision>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("a-token-this-instance-minted");

            return new UsageDataController(consents.Object, gate.Object, queue);
        }
    }
}
