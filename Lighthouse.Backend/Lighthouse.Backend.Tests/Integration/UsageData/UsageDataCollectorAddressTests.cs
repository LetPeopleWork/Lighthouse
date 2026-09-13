using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
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
    /// The guard is about configuration rather than about environments: a Lighthouse that was never
    /// told where to send does not send. Every real deployment tells it - the chart renders the
    /// value, Docker and standalone set it - and no test start does, including the installed macOS
    /// bundle, which is the one start that cannot be given a per-step override at all.
    ///
    /// Both halves are here on purpose. A guard that refuses everything satisfies the first scenario
    /// and ships a feature that never sends anything to anybody.
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

        // Somewhere that cannot exist, which is what an address supplied on purpose looks like here.
        // Never an empty value: empty means "fall back to the built-in default", which is exactly the
        // failure this whole fixture is about.
        private const string AnAddressSomebodySupplied = "https://collector.usage-data-tests.invalid/";
        private const string TheHostSomebodySupplied = "collector.usage-data-tests.invalid";

        private const string NoForwarderYet =
            "Pending: needs the forwarder and the address guard (Epic 5733 slice 01c, ADO #5980).";

        private CapturedOutboundRequests outbound = null!;
        private CapturedLogMessages capturedLogs = null!;
        private WebApplicationFactory<Program> builtHost = null!;

        /// <summary>
        /// The one that keeps synthetic traffic out of the real numbers. It is not enough for this
        /// to be dropped the way any other send failure is dropped: silent is indistinguishable from
        /// working, so an instance that would have sent to the built-in address and did not has to
        /// say so where somebody looking would find it.
        /// </summary>
        [Test]
        public async Task AnInstanceNobodyToldWhereToSend_DoesNotSendToTheLiveCensusAndSaysWhyNot()
        {
            using var host = BuildHost(collectorAddress: null);
            using var client = host.CreateClient();
            await FreshDatabaseAsync(host);

            var token = await ABrowserThatAgreedAsync(client);
            capturedLogs.Clear();

            using var accepted = await HandInAsync(client, token);
            await TheForwarderHasHadItsChance();

            var saidSo = capturedLogs.AtOrAbove(LogEventLevel.Warning)
                .Where(line => line.Contains("usage", StringComparison.OrdinalIgnoreCase))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "the batch has to have been taken in, or this scenario is about an endpoint "
                    + "that refused rather than about an address that was never supplied");
                Assert.That(outbound.ThatReached(TheLiveCensusHost), Is.Empty,
                    "an instance that was never told where to send fell back to the built-in "
                    + "address, which is the live census. Every test run that opens a Team tab now "
                    + "invents events in the real numbers");
                Assert.That(saidSo, Is.Not.Empty,
                    "refusing quietly looks exactly like working, and a usage event dropping "
                    + "silently is normal here by design. Whoever is running an instance that "
                    + "cannot send has to be able to find out that it cannot");
            }
        }

        /// <summary>
        /// The other half, and it matters as much. Without it, refusing every address passes the
        /// scenario above and ships a pipe that is permanently silent - which nothing else in this
        /// suite would notice, because everything else asserts that things do not get sent.
        /// </summary>
        [Test]
        public async Task AnInstanceToldWhereToSend_SendsThere()
        {
            using var host = BuildHost(AnAddressSomebodySupplied);
            using var client = host.CreateClient();
            await FreshDatabaseAsync(host);

            var token = await ABrowserThatAgreedAsync(client);

            using var accepted = await HandInAsync(client, token);
            await TheForwarderHasHadItsChance();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(outbound.ThatReached(TheHostSomebodySupplied), Is.Not.Empty,
                    "an address was supplied and nothing was sent to it, so the guard refuses "
                    + "everything and the feature is dead rather than careful");
                Assert.That(outbound.ThatReached(TheLiveCensusHost), Is.Empty,
                    "the supplied address was ignored in favour of the built-in one, which is the "
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

        private WebApplicationFactory<Program> BuildHost(string? collectorAddress)
        {
            outbound = new CapturedOutboundRequests();
            capturedLogs = new CapturedLogMessages();

            var root = new TestWebApplicationFactory<Program>();
            builtHost = root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                        new OutboundRequestRecordingFilter(outbound));

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
