using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.UsageData
{
    /// <summary>
    /// What actually goes into the message, and what happens when the far end will not take it.
    /// The scenarios beside this one show that something was posted and that no identifier rode
    /// along; these are about the parts of the message a recipient reads as a number - when it
    /// happened above all - and about the one answer from the collector that must not be shrugged
    /// off, because everything downstream of it is built on a failure being noticed.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataPublishedMessageTests
    {
        private const string AnAddressAnythingCanPostTo = "https://collector.usage-data-tests.invalid/";

        private const int HowLongTheBrowserSaidItHadBeenWaiting = 2500;

        private static readonly DateTimeOffset WhenItWasHandedIn =
            new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

        private RecordingLogger<PostHogUsageDataPublisher> logger = null!;

        [SetUp]
        public void SetUp()
        {
            logger = new RecordingLogger<PostHogUsageDataPublisher>();
        }

        /// <summary>
        /// A browser holds on to what it saw and hands the lot over later, saying how long ago each
        /// one was. That gap has to be subtracted, not added: added, every event this product ever
        /// reports is stamped in the future, and a collector asked when people use Lighthouse
        /// answers with a shape nobody can read - which is the whole of what these figures are for.
        /// </summary>
        [Test]
        public async Task AnEventTheBrowserHeldOnToForAWhile_IsReportedAsHavingHappenedThatLongAgo()
        {
            var posted = new WhatWasPosted();
            var wayOut = AWayOutToldToSendTo(AnAddressAnythingCanPostTo, posted);

            await wayOut.PublishAsync(
                APermit(), ABatch(offsetMs: HowLongTheBrowserSaidItHadBeenWaiting), CancellationToken.None);

            var reportedAt = TheOneMessageIn(posted).GetProperty("timestamp").GetDateTimeOffset();

            Assert.That(reportedAt, Is.EqualTo(WhenItWasHandedIn.AddMilliseconds(-HowLongTheBrowserSaidItHadBeenWaiting)),
                "the delay the browser reported was not taken off the moment it handed the batch in, "
                + "so this event is recorded at a time it did not happen - and if it was added "
                + "instead of subtracted, at a time that has not happened yet");
        }

        /// <summary>
        /// The precondition for the scenario above being about the browser's delay rather than about
        /// the clock: an event handed over the instant it happened is stamped now.
        /// </summary>
        [Test]
        public async Task AnEventHandedOverTheMomentItHappened_IsReportedAsHavingHappenedNow()
        {
            var posted = new WhatWasPosted();
            var wayOut = AWayOutToldToSendTo(AnAddressAnythingCanPostTo, posted);

            await wayOut.PublishAsync(APermit(), ABatch(offsetMs: 0), CancellationToken.None);

            Assert.That(TheOneMessageIn(posted).GetProperty("timestamp").GetDateTimeOffset(),
                Is.EqualTo(WhenItWasHandedIn),
                "an event reported with no delay at all did not arrive stamped with the moment it "
                + "was handed in, so the clock this reads is not the one it claims to read");
        }

        /// <summary>
        /// Everything the caller does about a batch that did not arrive - putting the day's
        /// allowance back, telling an operator once - hangs on this being noticed. A collector that
        /// answers with a refusal and is treated as having accepted is the worst outcome the whole
        /// subsystem has: the data is gone, the allowance is spent on it, and nothing is said.
        /// </summary>
        [Test]
        public void ACollectorThatRefusesTheBatch_IsAFailureRatherThanASilentSuccess()
        {
            var posted = new WhatWasPosted(HttpStatusCode.InternalServerError);
            var wayOut = AWayOutToldToSendTo(AnAddressAnythingCanPostTo, posted);

            Assert.That(
                async () => await wayOut.PublishAsync(APermit(), ABatch(), CancellationToken.None),
                Throws.InstanceOf<HttpRequestException>(),
                "the collector refused the batch and this reported success, so the allowance stays "
                + "spent on data that was never stored and nobody is ever told");
        }

        /// <summary>
        /// Publishing without one of these would mean either sending under no pseudonym at all or
        /// sending an empty message; both would be found later, in the figures, as a gap nobody can
        /// explain.
        /// </summary>
        [Test]
        public void PublishingWithNothingToPublish_IsRefusedAtTheDoor()
        {
            var wayOut = AWayOutToldToSendTo(AnAddressAnythingCanPostTo, new WhatWasPosted());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await wayOut.PublishAsync(null!, ABatch(), CancellationToken.None),
                    Throws.ArgumentNullException,
                    "a batch was sent with nothing saying which browser it belongs to");
                Assert.That(
                    async () => await wayOut.PublishAsync(APermit(), null!, CancellationToken.None),
                    Throws.ArgumentNullException,
                    "a message was built for a batch that does not exist");
            }
        }

        /// <summary>
        /// Once means from the first one, not from the second. A latch that stays quiet the first
        /// time and speaks the second still writes exactly one line over two batches, and is
        /// therefore indistinguishable from a working one by counting - but an instance that hands
        /// in one batch and then stops says nothing at all, which is the case this exists for.
        /// </summary>
        [Test]
        public async Task ABuildNobodyPublished_SaysSoOnTheVeryFirstBatch()
        {
            var wayOut = AWayOutToldToSendTo(named: null, new WhatWasPosted(), published: false);

            await wayOut.PublishAsync(APermit(), ABatch(), CancellationToken.None);

            Assert.That(logger.Warnings, Has.Count.EqualTo(1),
                "the first batch a build nobody published tries to send passed in silence, so an "
                + "instance that sends one and stops looks exactly like one where nobody agreed");
        }

        /// <summary>
        /// The same latch, and the same reason: a mistyped address disables the feature for the life
        /// of the process, and the line saying so has to be there after the first batch rather than
        /// after the second.
        /// </summary>
        [Test]
        public async Task AnAddressNothingCanPostTo_SaysSoOnTheVeryFirstBatch()
        {
            var wayOut = AWayOutToldToSendTo("collector.usage-data-tests.invalid", new WhatWasPosted());

            await wayOut.PublishAsync(APermit(), ABatch(), CancellationToken.None);

            Assert.That(logger.Warnings, Has.Count.EqualTo(1),
                "the first batch after somebody mistyped the address passed in silence, so an "
                + "operator who checks the log once and sees nothing concludes it is working");
        }

        /// <summary>
        /// The client is registered beside the address rather than in the composition root, and the
        /// time limit is the whole reason it is registered at all: without it a collector that
        /// accepts a connection and then says nothing holds the sending loop for a hundred seconds
        /// per batch, which is how usage data stops being something nobody notices.
        /// </summary>
        [Test]
        public void TheClientTheWayOutSendsThrough_GivesUpLongBeforeAnythingElseWould()
        {
            var services = new ServiceCollection();
            services.AddUsageDataPublishing();

            using var provider = services.BuildServiceProvider();
            using var client = provider.GetRequiredService<IHttpClientFactory>()
                .CreateClient(PostHogUsageDataPublisher.HttpClientName);

            Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(10)),
                "the way out is sending through a client with whatever limit happens to be the "
                + "default, so a collector that stalls rather than refusing holds this instance's "
                + "sending loop for as long as it likes");
        }

        [Test]
        public void RegisteringTheWayOutIntoNothing_IsRefusedAtTheDoor()
        {
            Assert.That(
                () => UsageDataPublishing.AddUsageDataPublishing(null!),
                Throws.ArgumentNullException,
                "registration against nothing was accepted, so a composition root with a mistake in "
                + "it starts an instance whose way out is silently missing");
        }

        private static JsonElement TheOneMessageIn(WhatWasPosted posted)
        {
            Assert.That(posted.Bodies, Has.Count.EqualTo(1),
                "exactly one post was expected, and reading the message below only means something "
                + "if that is what happened");

            using var message = JsonDocument.Parse(posted.Bodies[0]);

            return message.RootElement[0].Clone();
        }

        private PostHogUsageDataPublisher AWayOutToldToSendTo(
            string? named, WhatWasPosted posted, bool published = true)
        {
            var configuration = new Mock<IOptionsMonitor<UsageDataConfiguration>>();
            configuration
                .SetupGet(options => options.CurrentValue)
                .Returns(new UsageDataConfiguration { CollectorBaseUrl = named, ProjectApiKey = "a-key" });

            var instance = new Mock<IUsageDataInstanceProperties>();
            instance
                .Setup(properties => properties.Describe())
                .Returns(new UsageDataInstanceFacts(
                    published ? "v26.9.13" : "unreleased",
                    UsageDataDeploymentMode.Docker,
                    "Community",
                    AuthenticationEnabled: false,
                    IsPublishedRelease: published));

            var clock = new Mock<ILighthouseClock>();
            clock.SetupGet(reading => reading.Now).Returns(WhenItWasHandedIn);

            return new PostHogUsageDataPublisher(
                new TheOnlyClient(posted), configuration.Object, instance.Object, clock.Object, logger);
        }

        private static UsageDataEmitPermit APermit()
        {
            return new UsageDataEmitPermit("a-pseudonym-this-browser-is-counted-under");
        }

        private static AcceptedUsageDataBatch ABatch(int offsetMs = 0)
        {
            return new AcceptedUsageDataBatch(
                "a-token-a-browser-presented",
                [new UsageDataEventReported(
                    UsageDataEventName.TeamTabOpened,
                    UsageDataRouteKey.TeamDetail_Metrics,
                    WorkTrackingSystem: null,
                    offsetMs,
                    Sequence: 0)]);
        }

        /// <summary>
        /// Answers in place of the network and keeps what it was handed. Nothing named in this
        /// fixture resolves, so a request that got past this would fail rather than quietly reach
        /// somewhere real.
        /// </summary>
        private sealed class WhatWasPosted(HttpStatusCode answer = HttpStatusCode.OK) : HttpMessageHandler
        {
            public List<string> Bodies { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(request);

                if (request.Content is { } content)
                {
                    Bodies.Add(await content.ReadAsStringAsync(cancellationToken));
                }

                return new HttpResponseMessage(answer);
            }
        }

        private sealed class TheOnlyClient(HttpMessageHandler handler) : IHttpClientFactory
        {
            // The way out disposes what it is handed, and the handler has to outlive that: what was
            // posted is read after the call returns.
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }
    }
}
