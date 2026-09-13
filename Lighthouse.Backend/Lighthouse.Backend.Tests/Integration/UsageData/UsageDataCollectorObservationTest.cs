using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// The one place that knows how to watch what leaves this instance, shared by every fixture that
    /// needs to. The real application over real HTTP on real SQLite, with a recorder standing in for
    /// the network in front of every client the framework hands out - which is what makes "it went
    /// out" and "it did not" two different observations rather than an inference from a double that
    /// was not called.
    ///
    /// It exists because the guarantee below cannot be allowed to live in two files. There is
    /// exactly one real collector project and it is the live census the maintainer reads; a test
    /// that reached it would invent events in the real numbers and would do it silently, because
    /// sending degrades without complaint by specification. Every fixture built on this is pointed
    /// at an address that cannot resolve and is checked on the way out. Copied per fixture, that
    /// check drifts: somebody tightens one and the other quietly stops guarding anything.
    /// </summary>
    public abstract class UsageDataCollectorObservationTest
    {
        protected const string EventsRoute = "/api/latest/usagedata/events";
        protected const string ConsentRoute = "/api/latest/usagedata/consent";
        protected const string StateRoute = "/api/latest/usagedata/state";
        protected const string ConsentTokenHeader = "X-Lighthouse-UsageData-Token";
        protected const string JsonMediaType = "application/json";

        /// <summary>
        /// Where a fixture built on this is told to send. A <c>.invalid</c> address can never
        /// resolve, and the recorder answers in place of the network in any case - two independent
        /// reasons nothing can reach anywhere real.
        /// </summary>
        protected const string CollectorAddress = "https://collector.usage-data-tests.invalid/";

        protected const string CollectorHost = "collector.usage-data-tests.invalid";

        /// <summary>
        /// The address a Lighthouse that was never told where to send would fall back to. Named so
        /// that every scenario can be shown not to have reached it.
        /// </summary>
        protected const string TheLiveCensusHost = "posthog.com";

        /// <summary>
        /// The single product event this Epic carries, and the tab label that has to survive a page
        /// address naming one of the customer's own Teams.
        /// </summary>
        protected const string TabOpened = "TeamOrPortfolioTabOpened";

        protected const string TeamMetricsTab = "TeamDetail_Metrics";

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;
        protected CapturedOutboundRequests Outbound = null!;

        [SetUp]
        public void StartWatchingWhatLeaves()
        {
            RootFactory = new TestWebApplicationFactory<Program>();
            Outbound = new CapturedOutboundRequests();

            Factory = RootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                        new OutboundRequestRecordingFilter(Outbound));

                    AlsoRegister(services);
                });

                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["UsageData:CollectorBaseUrl"] = CollectorAddress,
                    });
                });
            });

            Client = Factory.CreateClient();

            using var scope = Factory.Services.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            databaseContext.Database.EnsureDeleted();
            databaseContext.Database.EnsureCreated();
        }

        /// <summary>
        /// Runs after every scenario, whatever it was about, and fails it for having contacted the
        /// live census. Unconditional on purpose: a scenario that reaches the real collector has
        /// already done the damage by the time anybody reads its other assertions.
        /// </summary>
        [TearDown]
        public void StopWatchingAndCheckNothingReachedTheCensus()
        {
            var reachedTheCensus = Outbound.ThatReached(TheLiveCensusHost);

            using (var scope = Factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();

            Assert.That(reachedTheCensus, Is.Empty,
                "a scenario in this fixture contacted the real collector. The host is told to send "
                + "somewhere that cannot exist, so reaching it means something ignored where it was "
                + "told to send - and the events it invented are now in the live numbers");
        }

        /// <summary>
        /// For a fixture that needs more of the host than the recorder. Called while the host is
        /// being built, so anything registered here is in place before the first request.
        /// </summary>
        protected virtual void AlsoRegister(IServiceCollection services)
        {
        }

        protected async Task<string> ABrowserThatAgreedAsync() => await ADecisionRecordedAsync("granted");

        protected async Task<string> ABrowserThatRefusedAsync() => await ADecisionRecordedAsync("declined");

        protected async Task<string> ADecisionRecordedAsync(string decision)
        {
            using var response = await Client.PostAsJsonAsync(ConsentRoute, new { decision });
            var body = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("token", out var token)
                ? token.GetString() ?? string.Empty
                : string.Empty;
        }

        /// <summary>
        /// A browser handing in what it saw. The caller disposes the response, because several
        /// scenarios read the body to show it carries nothing.
        /// </summary>
        protected async Task<HttpResponseMessage> HandInAsync(string? token, string body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EventsRoute)
            {
                Content = new StringContent(body, Encoding.UTF8, JsonMediaType),
            };

            if (token is not null)
            {
                request.Headers.Add(ConsentTokenHeader, token);
            }

            return await Client.SendAsync(request);
        }

        /// <summary>
        /// Everything the collector was sent, as one piece of text, after emptying what is waiting.
        /// The drain is asked for rather than waited out: this host runs no background work, so
        /// waiting would make every scenario pay a fixed budget long enough to be reliable on a
        /// loaded build agent - including every scenario whose whole point is that nothing was sent,
        /// which is most of them.
        /// </summary>
        protected async Task<string> EverythingTheCollectorReceived()
        {
            await Factory.Services.GetRequiredService<UsageDataForwardingService>()
                .SendWhatIsWaitingAsync(CancellationToken.None);

            return Outbound.EverythingSentTo(CollectorHost);
        }

        protected async Task<JsonElement> TheStateThisBrowserIsToldAsync(string? token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, StateRoute);

            if (token is not null)
            {
                request.Headers.Add(ConsentTokenHeader, token);
            }

            using var response = await Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }

        protected static string ABatchOf(string name, string route)
        {
            return $"{{\"events\":[{{\"name\":\"{name}\",\"route\":\"{route}\",\"offsetMs\":0,\"sequence\":0}}]}}";
        }

        /// <summary>
        /// Engages or lifts the switch that stops usage data for a whole instance. It lives here
        /// rather than in one fixture because it is now the gate every event has to be shown to
        /// inherit, and a second copy is how one of them quietly stops guarding anything.
        /// </summary>
        protected void StoreTheVeto(bool engaged)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            var existing = repository.GetByPredicate(feature => feature.Key == UsageDataMasterSwitch.Key);

            if (existing is null)
            {
                repository.Add(new OptionalFeature
                {
                    Id = 0,
                    Key = UsageDataMasterSwitch.Key,
                    Name = "Never send usage data",
                    Description = "Seeded by a scenario; the product's own wording is asserted elsewhere.",
                    Enabled = engaged,
                    IsPremium = true,
                });
            }
            else
            {
                existing.Enabled = engaged;
            }

            repository.Save().GetAwaiter().GetResult();
        }
    }
}
