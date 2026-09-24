using System.Net;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.UsageData
{
    /// <summary>
    /// Epic 5733 slice 01c (ADO #5980) - what happens to the one setting an operator is invited to
    /// touch when they get it wrong.
    ///
    /// Where to send is the only thing about this feature anybody configures, and the way to get it
    /// wrong is to write the host and leave the scheme off. Both of the signals this subsystem
    /// raises when it is not sending are skipped on that path: the line about a build nobody
    /// published is only reached when no address was named, and naming one deliberately lifts the
    /// version rule. So without something here, the most likely mistake there is disables usage data
    /// for the life of the process and looks exactly like an instance where nobody agreed.
    ///
    /// At this seam rather than through a host, because what has to be shown is that nothing was
    /// posted at all - and an integration fixture can only observe what a client was asked to send,
    /// which is the same nothing whether the address was refused or a request was made and failed.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataMistypedCollectorAddressTests
    {
        private const string AnAddressAnythingCanPostTo = "https://collector.usage-data-tests.invalid/";

        private RecordingLogger<PostHogUsageDataPublisher> logger = null!;

        [SetUp]
        public void SetUp()
        {
            logger = new RecordingLogger<PostHogUsageDataPublisher>();
        }

        /// <summary>
        /// Three ways of writing something that is not a web address. The first is the one that will
        /// actually happen - the host on its own - and the third is here because a scheme nothing
        /// can post to reads perfectly well as an address, so "it parsed" is not the question.
        ///
        /// Nothing is thrown either. Whoever calls this treats a failure as a batch to drop, so an
        /// address that threw would be reported once per batch at a level nobody reads.
        /// </summary>
        [TestCase("collector.usage-data-tests.invalid")]
        [TestCase("not an address at all")]
        [TestCase("ftp://collector.usage-data-tests.invalid")]
        public async Task AnAddressNothingCanPostTo_SendsNothingAndSaysSoOnce(string whatSomebodyTyped)
        {
            var posted = new WhatWasPosted();
            var wayOut = AWayOutToldToSendTo(whatSomebodyTyped, posted);

            await wayOut.PublishAsync(APermit(), ABatch(), CancellationToken.None);
            await wayOut.PublishAsync(APermit(), ABatch(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(posted.Requests, Is.Zero,
                    "something was posted to an address that cannot be posted to, so what this "
                    + "instance does with usage data is not what the setting says");
                Assert.That(logger.Warnings, Has.Count.EqualTo(1),
                    "expected exactly one warning naming the setting. None means the feature is off "
                    + "for the life of the process with nobody told, which is indistinguishable from "
                    + "an instance where nobody agreed. More than one means a line per batch, which a "
                    + "left-open browser turns into an instance filling its own disk");
                Assert.That(logger.Warnings[0], Does.Contain(whatSomebodyTyped),
                    "the line does not quote what was actually set, so an operator reading it cannot "
                    + "tell which of their settings is the wrong one");
            }
        }

        /// <summary>
        /// The precondition the scenario above is worthless without: a way out that posted nothing
        /// anywhere would satisfy it too, and would have switched the feature off for everybody
        /// rather than for whoever mistyped something.
        /// </summary>
        [Test]
        public async Task AnAddressSomethingCanPostTo_IsPostedTo()
        {
            var posted = new WhatWasPosted();
            var wayOut = AWayOutToldToSendTo(AnAddressAnythingCanPostTo, posted);

            await wayOut.PublishAsync(APermit(), ABatch(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(posted.Requests, Is.EqualTo(1),
                    "an address anything can post to was not posted to, so refusing the ones that "
                    + "cannot be says nothing about a pipe that was never running");
                Assert.That(logger.Warnings, Is.Empty,
                    "an address that works was complained about, which is how an operator learns to "
                    + "stop reading these lines");
            }
        }

        private PostHogUsageDataPublisher AWayOutToldToSendTo(string named, WhatWasPosted posted)
        {
            var configuration = new Mock<IOptionsMonitor<UsageDataConfiguration>>();
            configuration
                .SetupGet(options => options.CurrentValue)
                .Returns(new UsageDataConfiguration { CollectorBaseUrl = named, ProjectApiKey = "a-key" });

            // A published release, so that nothing here turns on the rule that keeps unpublished
            // builds out of the figures - an address was named, and naming one lifts that rule.
            var instance = new Mock<IUsageDataInstanceProperties>();
            instance
                .Setup(properties => properties.Describe())
                .Returns(new UsageDataInstanceFacts(
                    "v26.9.13",
                    UsageDataDeploymentMode.Docker,
                    "Free",
                    AuthenticationEnabled: false,
                    IsPublishedRelease: true));

            var clock = new Mock<ILighthouseClock>();
            clock.SetupGet(reading => reading.Now).Returns(DateTimeOffset.UnixEpoch);

            return new PostHogUsageDataPublisher(
                new TheOnlyClient(posted), configuration.Object, instance.Object, clock.Object, logger);
        }

        private static UsageDataEmitPermit APermit()
        {
            return new UsageDataEmitPermit("a-pseudonym-this-browser-is-counted-under");
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

        /// <summary>
        /// Answers in place of the network and keeps the count. Nothing named here resolves, so a
        /// request that got past this would fail rather than quietly reach somewhere.
        /// </summary>
        private sealed class WhatWasPosted : HttpMessageHandler
        {
            public int Requests { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        private sealed class TheOnlyClient(HttpMessageHandler handler) : IHttpClientFactory
        {
            // The way out disposes what it is handed, and the handler has to outlive that: a
            // scenario here sends twice and reads the count afterwards.
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }
    }
}
