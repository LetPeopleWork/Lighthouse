using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// Epic 5733 slice 01c (ADO #5980) - which address this instance is allowed to send to.
    ///
    /// There is exactly one collector project, and it is the live census. The free plan allows one;
    /// a paid plan is refused because it would raise the retention floor from one year to seven,
    /// which the shipped disclosure commits us to re-asking every consenting person about. So there
    /// is no second project for continuous integration to send into, and there is not going to be.
    ///
    /// That matters far more than it used to. The event this slice ships fires when somebody opens a
    /// Team or Portfolio tab, and the browser tests open those tabs constantly - so every run is now
    /// a source of invented events aimed at the real numbers. Sending degrades silently by
    /// specification, so this would happen with no error and no red build.
    ///
    /// The address ships inside the product now, so what decides is what this build is rather than
    /// what anybody configured. An address named on purpose takes whatever build it is given - that
    /// is how a fork points at a collector of its own, and how anyone watches the whole path end to
    /// end before shipping. The built-in one takes published releases only: a release is stamped
    /// with the date it was built, and nothing a test host, a working tree or a developer's own
    /// machine calls itself has that shape.
    ///
    /// All three scenarios are here on purpose, and the middle one most of all. A rule that refuses
    /// everything satisfies the first and ships a pipe that is silent in every real deployment -
    /// which nothing else in this suite would notice, because everything else asserts that things do
    /// not get sent.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataCollectorAddressTests
    {
        private const string EventsRoute = "/api/latest/usagedata/events";
        private const string ConsentRoute = "/api/latest/usagedata/consent";
        private const string ConsentTokenHeader = "X-Lighthouse-UsageData-Token";
        private const string JsonMediaType = "application/json";

        private const string TheLiveCensusHost = "posthog.com";

        // Somewhere that cannot exist, which is what an address named on purpose looks like here.
        // Never an empty value: empty is not a name, it is "nobody named one", and that is a
        // different scenario below with a different answer.
        private const string AnAddressSomebodySupplied = "https://collector.usage-data-tests.invalid/";
        private const string TheHostSomebodySupplied = "collector.usage-data-tests.invalid";

        // Nobody named one. Now that the address ships in the product this is how every real
        // deployment runs, so it is an ordinary state rather than the refusal case, and what happens
        // next turns entirely on which kind of build is asking.
        private const string? NobodyNamedACollector = null;

        // Two-digit year, then month, then day. Every release this product has published is stamped
        // with the date it was built, and that stamp is the whole of what makes one recognisable.
        private const string WhatAPublishedReleaseCallsItself = "v26.9.13";

        // What a test host reports about itself, and near enough what anything built from a working
        // tree reports. Not a date, so not a release.
        private const string WhatABuildNobodyPublishedCallsItself = "v1.0.0";

        // The phrase only this one warning carries. Matching the whole sentence would turn a
        // rewording into a missing message, and matching "usage data" would count the other warning
        // this subsystem can raise - the one about a spent daily allowance - as though it were this
        // one.
        private const string SayingNothingIsSent = "not a published release";

        private CapturedOutboundRequests outbound = null!;
        private CapturedLogMessages capturedLogs = null!;
        private WebApplicationFactory<Program> builtHost = null!;

        /// <summary>
        /// The one that keeps invented traffic out of the real numbers - this test suite's own
        /// first of all, and every copy anyone is running from source after that. It is not enough
        /// for these events to be dropped the way any other send failure is dropped: silent is
        /// indistinguishable from working, so an instance holding usage data it will never send has
        /// to say so where somebody looking would find it.
        ///
        /// Saying it once rather than once per batch is half of that. A browser that agreed and
        /// then left a tab open goes on handing batches in for as long as it is open, so a line per
        /// batch is how a developer's own machine fills its own disk.
        /// </summary>
        [Test]
        public async Task ABuildNobodyPublished_WithNoCollectorNamed_SendsNothingAndSaysSoOnce()
        {
            using var host = BuildHost(NobodyNamedACollector, WhatABuildNobodyPublishedCallsItself);
            using var client = host.CreateClient();
            await FreshDatabaseAsync(host);

            var token = await ABrowserThatAgreedAsync(client);
            capturedLogs.Clear();

            using var firstBatch = await HandInAsync(client, token);
            await TheForwarderHasHadItsChance();

            using var secondBatch = await HandInAsync(client, token);
            await TheForwarderHasHadItsChance();

            var saidSo = capturedLogs.AtOrAbove(LogEventLevel.Warning)
                .Where(line => line.Contains(SayingNothingIsSent, StringComparison.OrdinalIgnoreCase))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    new[] { firstBatch.StatusCode, secondBatch.StatusCode },
                    Is.All.EqualTo(HttpStatusCode.NoContent),
                    "both batches have to have been taken in, or this scenario is about an endpoint "
                    + "that refused rather than about a build that is kept out of the figures");
                Assert.That(outbound.ThatReached(TheLiveCensusHost), Is.Empty,
                    "a build nobody published reached the built-in address, which is the live "
                    + "census. Every test run that opens a Team tab now invents events in the real "
                    + "numbers, and so does every copy anyone is running from source");
                Assert.That(saidSo, Has.Count.EqualTo(1),
                    "expected exactly one warning saying why nothing is going out. None means "
                    + "refusing quietly, which looks exactly like working, given that a usage event "
                    + "dropping silently is normal here by design. More than one means a line per "
                    + "batch, which a left-open browser turns into an instance filling its own disk");
            }
        }

        /// <summary>
        /// The one that stops a permanently silent pipe from shipping. The address is inside the
        /// product, so a real deployment names nothing and this is the path every one of them takes.
        /// If it stopped working there would be no setting anywhere for an operator to have got
        /// wrong, and nothing else here would go red, because every other scenario asserts that
        /// something was not sent.
        /// </summary>
        [Test]
        public async Task APublishedRelease_WithNoCollectorNamed_ReachesTheCollectorItShipsWith()
        {
            using var host = BuildHost(NobodyNamedACollector, WhatAPublishedReleaseCallsItself);
            using var client = host.CreateClient();
            await FreshDatabaseAsync(host);

            var token = await ABrowserThatAgreedAsync(client);

            using var accepted = await HandInAsync(client, token);
            await TheForwarderHasHadItsChance();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(outbound.ThatReached(TheLiveCensusHost), Is.Not.Empty,
                    "a published release with nothing configured sent nothing anywhere - and that "
                    + "is every real deployment there is. The rule that keeps unpublished builds "
                    + "out is keeping everybody out, and the feature is dead rather than careful");
            }
        }

        /// <summary>
        /// Naming an address lifts the version rule rather than bending it: a fork, or anyone
        /// watching the path end to end, sends from whatever they happen to be running. Both kinds
        /// of build are here because "whatever this is" is the whole of what naming an address
        /// buys, and only the unpublished one can show the version rule was skipped rather than
        /// satisfied by accident.
        /// </summary>
        [TestCase(WhatAPublishedReleaseCallsItself)]
        [TestCase(WhatABuildNobodyPublishedCallsItself)]
        public async Task AnInstanceToldWhereToSend_SendsThere(string whatThisBuildCallsItself)
        {
            using var host = BuildHost(AnAddressSomebodySupplied, whatThisBuildCallsItself);
            using var client = host.CreateClient();
            await FreshDatabaseAsync(host);

            var token = await ABrowserThatAgreedAsync(client);

            using var accepted = await HandInAsync(client, token);
            await TheForwarderHasHadItsChance();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(outbound.ThatReached(TheHostSomebodySupplied), Is.Not.Empty,
                    "an address was named and nothing was sent to it, so naming one buys nothing "
                    + "and there is no way left to watch this path without joining the real numbers");
                Assert.That(outbound.ThatReached(TheLiveCensusHost), Is.Empty,
                    "the named address was ignored in favour of the built-in one, which is the "
                    + "live census");
            }
        }

        private static async Task<string> ABrowserThatAgreedAsync(HttpClient client)
        {
            using var response = await client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" });
            var body = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("token", out var token)
                ? token.GetString() ?? string.Empty
                : string.Empty;
        }

        private static async Task<HttpResponseMessage> HandInAsync(HttpClient client, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EventsRoute)
            {
                Content = new StringContent(
                    "{\"events\":[{\"name\":\"TeamOrPortfolioTabOpened\",\"route\":\"TeamDetail_Metrics\",\"offsetMs\":0,\"sequence\":0}]}",
                    Encoding.UTF8,
                    JsonMediaType),
            };
            request.Headers.Add(ConsentTokenHeader, token);

            return await client.SendAsync(request);
        }

        private static async Task FreshDatabaseAsync(WebApplicationFactory<Program> host)
        {
            using var scope = host.Services.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await databaseContext.Database.EnsureDeletedAsync();
            await databaseContext.Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// Empties what is waiting and returns. Asked for rather than waited out: this host runs no
        /// background work, so waiting would have meant a fixed budget long enough to be reliable on
        /// a loaded build agent, paid by every scenario including the ones about nothing being sent.
        /// </summary>
        private async Task TheForwarderHasHadItsChance()
        {
            await builtHost.Services.GetRequiredService<UsageDataForwardingService>()
                .SendWhatIsWaitingAsync(CancellationToken.None);
        }

        private WebApplicationFactory<Program> BuildHost(string? collectorAddress, string whatThisBuildCallsItself)
        {
            outbound = new CapturedOutboundRequests();
            capturedLogs = new CapturedLogMessages();

            var releases = new Mock<ILighthouseReleaseService>();
            releases.Setup(service => service.GetCurrentVersion()).Returns(whatThisBuildCallsItself);

            var root = new TestWebApplicationFactory<Program>();
            builtHost = root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                        new OutboundRequestRecordingFilter(outbound));

                    // Said out loud by every scenario rather than left to the host. What the version
                    // looks like is half of what decides whether anything is sent at all, so a test
                    // that does not say which kind of build it is ends up describing whichever one a
                    // test host happens to report - which is not a kind anybody runs.
                    services.RemoveAll<ILighthouseReleaseService>();
                    services.AddScoped(_ => releases.Object);

                    services.RemoveAll<ILoggerFactory>();
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                        new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(capturedLogs).CreateLogger(),
                        dispose: true));
                });

                if (collectorAddress is null)
                {
                    return;
                }

                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["UsageData:CollectorBaseUrl"] = collectorAddress,
                    });
                });
            });

            return builtHost;
        }
    }
}
