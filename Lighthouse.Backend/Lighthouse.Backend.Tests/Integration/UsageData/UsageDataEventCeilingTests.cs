using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.Tests.TestHelpers.ForwardedHeaders;
using Microsoft.AspNetCore.Hosting;
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
    /// Epic 5733 slice 01c (ADO #5980) - the ceilings, which bound different things and fail in
    /// opposite directions.
    ///
    /// The first bounds one browser against one instance and refuses when it is passed. Underneath
    /// it sits a far larger one on the address a request is seen from, which is what a browser
    /// cannot invent its way out of - the handle the first counts by is claimed rather than proved,
    /// so on its own it bounds only callers who are willing to be counted. The last bounds the whole
    /// world against one allowance the maintainer pays for, shared by every instance there is -
    /// forty honest instances can exhaust it without any of them misbehaving - and it drops silently
    /// rather than refusing, because there is nothing useful a browser could do with a refusal
    /// except try again.
    ///
    /// Serial, and on the parallelization allowlist, for the reason the consent limiter fixture
    /// beside this one already carries: exhausting a limiter whose window is process-wide would
    /// refuse every other test that touched the same endpoint for the length of that window.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataEventCeilingTests
    {
        private const string EventsRoute = "/api/latest/usagedata/events";
        private const string ConsentRoute = "/api/latest/usagedata/consent";
        private const string ConsentTokenHeader = "X-Lighthouse-UsageData-Token";
        private const string JsonMediaType = "application/json";
        // This host is told to send somewhere that cannot exist. There is one real collector project
        // and it is the live census, and a ceiling scenario hands in dozens of invented events.
        private const string CollectorAddress = "https://collector.usage-data-tests.invalid/";
        private const string CollectorHost = "collector.usage-data-tests.invalid";

        private const string OneOffice = "203.0.113.42";
        private const string TrustedProxy = "127.0.0.1";

        private const int PermitLimit = 5;
        private const int WindowSeconds = 60;
        private const int DailyAllowance = 3;

        // The allowance scenario below gives every hand-in a browser of its own, which is more
        // consent calls in a minute than any real browser makes. Set here rather than inherited so
        // that a ceiling this fixture is not about cannot be the thing that stops it: refused
        // consent comes back as no token at all, which would read as a broken allowance.
        private const int ConsentPermitLimit = 200;

        // Enough attempts to pass the ceiling on one address, which is a multiple of the per-browser
        // one, without running forever if that ceiling is not there at all.
        private const int AttemptsNoRealBrowserWouldMake = 600;

        private CapturedOutboundRequests outbound = null!;
        private CapturedLogMessages capturedLogs = null!;

        /// <summary>
        /// A policy name that has no entry in configuration resolves to no limiter at all, silently,
        /// so declaring one and forgetting to configure it ships an unlimited anonymous endpoint
        /// that looks limited in the source. This is what catches that.
        /// </summary>
        [Test]
        public async Task HandingInEvents_IsActuallyLimitedRatherThanOnlyDeclaredToBe()
        {
            using var host = BuildHost();
            using var client = host.CreateClient();
            await FreshDatabaseAsync(host);

            var token = await ABrowserThatAgreedAsync(client);
            var taken = 0;
            HttpStatusCode? refused = null;

            for (var attempt = 0; attempt < 200 && refused is null; attempt++)
            {
                using var answer = await HandInAsync(client, token, OneOffice);
                if (answer.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    refused = answer.StatusCode;
                }
                else
                {
                    taken++;
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(taken, Is.GreaterThan(0),
                    "a ceiling that refuses the first message is not a ceiling, it is an outage");
                Assert.That(refused, Is.EqualTo(HttpStatusCode.TooManyRequests),
                    "this endpoint is anonymous and reachable by anyone who can reach the instance; "
                    + "unlimited, one caller can spend the maintainer's whole allowance");
            }
        }

        /// <summary>
        /// Fifty colleagues behind one office address share that address. Bounding them together
        /// would mean the busiest instances - the ones worth hearing from - throttle themselves.
        /// </summary>
        [Test]
        public async Task TwoBrowsersBehindOneAddress_DoNotSpendEachOthersAllowance()
        {
            using var host = BuildHost();
            using var client = host.CreateClient();
            await FreshDatabaseAsync(host);

            var oneColleague = await ABrowserThatAgreedAsync(client);
            var another = await ABrowserThatAgreedAsync(client);

            HttpStatusCode? refusedTheFirst = null;
            for (var attempt = 0; attempt < 200 && refusedTheFirst is null; attempt++)
            {
                using var answer = await HandInAsync(client, oneColleague, OneOffice);
                if (answer.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    refusedTheFirst = answer.StatusCode;
                }
            }

            using var theOtherOne = await HandInAsync(client, another, OneOffice);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusedTheFirst, Is.EqualTo(HttpStatusCode.TooManyRequests),
                    "the first browser was never bounded, so the second one being served says "
                    + "nothing about whether they are counted apart");
                Assert.That(theOtherOne.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests),
                    "counted by address rather than by browser, one busy colleague silences the "
                    + "whole office");
            }
        }

        /// <summary>
        /// The handle a browser presents is claimed, not proved. Counting by it alone means a caller
        /// who invents a new one for every request is a new browser every time, so the per-browser
        /// ceiling never refuses any of them - and each one still reaches the database twice before
        /// being dropped, on a product that is commonly run on a single-writer database and reachable
        /// from the internet without credentials.
        ///
        /// Nothing leaks and nothing is collected on this path: no consent resolves, so every one of
        /// these is refused where it matters. What is at stake is whether the instance stays up.
        /// </summary>
        [Test]
        public async Task OneAddressInventingANewBrowserEachTime_IsStillBounded()
        {
            using var host = BuildHost();
            using var client = host.CreateClient();
            await FreshDatabaseAsync(host);

            var taken = 0;
            HttpStatusCode? refused = null;

            for (var attempt = 0; attempt < AttemptsNoRealBrowserWouldMake && refused is null; attempt++)
            {
                using var answer = await HandInAsync(client, Guid.NewGuid().ToString(), OneOffice);
                if (answer.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    refused = answer.StatusCode;
                }
                else
                {
                    taken++;
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(taken, Is.GreaterThan(PermitLimit),
                    "the per-browser ceiling is what stopped this, which means the handles were not "
                    + "being counted apart and the scenario above is passing for the wrong reason");
                Assert.That(refused, Is.EqualTo(HttpStatusCode.TooManyRequests),
                    "a made-up handle bought a fresh allowance every time, so one anonymous caller "
                    + "can keep this instance answering database queries for as long as it likes");
            }
        }

        /// <summary>
        /// Silent to the browser, because a refusal only invites a retry. Not silent to the operator,
        /// because an instance that exhausts its allowance is either unusually busy or being abused
        /// and those look identical from outside. Once a day, not once a drop: a line per drop is a
        /// flood mechanism wearing a monitoring costume.
        ///
        /// A browser of its own for every hand-in, rather than one browser handing in over and over.
        /// The per-browser ceiling counts each of them separately, so not one of them is anywhere
        /// near it, and the only thing left that can stop this instance at the allowance is the
        /// allowance. Under a single browser, nothing here could tell the two ceilings apart.
        /// </summary>
        [Test]
        public async Task PastTheDailyAllowance_EventsAreDroppedQuietlyAndSaidOutLoudExactlyOnce()
        {
            using var host = BuildHost();
            using var client = host.CreateClient();
            await FreshDatabaseAsync(host);

            var browsers = new List<string>();
            for (var browser = 0; browser < DailyAllowance * 4; browser++)
            {
                browsers.Add(await ABrowserThatAgreedAsync(client));
            }

            capturedLogs.Clear();

            var answers = new List<HttpStatusCode>();
            foreach (var browser in browsers)
            {
                using var answer = await HandInAsync(client, browser, OneOffice);
                answers.Add(answer.StatusCode);
            }

            // Emptied before anything is read, because the allowance is spent where data leaves
            // rather than where it arrives. Nothing has been counted against it - and nothing has
            // been said about it - while the batches are still waiting.
            var received = await EverythingTheCollectorReceived(host);

            var saidOutLoud = capturedLogs.At(LogEventLevel.Warning)
                .Where(line => line.Contains("usage", StringComparison.OrdinalIgnoreCase))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answers, Has.None.EqualTo(HttpStatusCode.TooManyRequests),
                    "refusing tells a browser to try again, and trying again is the last thing an "
                    + "exhausted allowance needs");
                Assert.That(answers, Has.All.EqualTo(HttpStatusCode.NoContent),
                    "the answer is the same past the allowance as inside it, or the endpoint has "
                    + "become a way to measure how much the instance has already sent");
                Assert.That(received, Is.Not.Empty,
                    "nothing was forwarded even inside the allowance, so counting what went out "
                    + "past it measures a pipe that was never running");
                Assert.That(NumberOfMessagesIn(received), Is.EqualTo(DailyAllowance),
                    "dropping is the whole point of the allowance; answering politely and sending "
                    + "anyway spends exactly the thing it exists to protect");
                Assert.That(saidOutLoud, Has.Count.EqualTo(1),
                    "one line per drop lets whoever triggered it fill the disk, and no line at all "
                    + "leaves an operator unable to tell a busy day from an attack");
            }
        }

        /// <summary>
        /// Everything the collector was sent, as one piece of text, after emptying what is waiting.
        /// The drain is asked for rather than waited out: this host runs no background work, so
        /// waiting would have meant a fixed budget long enough to be reliable on a loaded build
        /// agent, paid by every scenario that uses it.
        /// </summary>
        private async Task<string> EverythingTheCollectorReceived(WebApplicationFactory<Program> host)
        {
            await host.Services.GetRequiredService<UsageDataForwardingService>()
                .SendWhatIsWaitingAsync(CancellationToken.None);

            return outbound.EverythingSentTo(CollectorHost);
        }

        private static int NumberOfMessagesIn(string received)
        {
            return received.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length;
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

        private static async Task<HttpResponseMessage> HandInAsync(HttpClient client, string token, string fromAddress)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EventsRoute)
            {
                Content = new StringContent(
                    "{\"events\":[{\"name\":\"TeamOrPortfolioTabOpened\",\"route\":\"TeamDetail_Metrics\",\"offsetMs\":0,\"sequence\":0}]}",
                    Encoding.UTF8,
                    JsonMediaType),
            };
            request.Headers.Add(ConsentTokenHeader, token);
            request.Headers.Add("X-Forwarded-For", fromAddress);

            return await client.SendAsync(request);
        }

        private static async Task FreshDatabaseAsync(WebApplicationFactory<Program> host)
        {
            using var scope = host.Services.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await databaseContext.Database.EnsureDeletedAsync();
            await databaseContext.Database.EnsureCreatedAsync();
        }

        // This fixture builds its own host rather than deriving from the integration base, because
        // the ceilings only exist when they are configured and the base host does not configure them.
        private WebApplicationFactory<Program> BuildHost()
        {
            outbound = new CapturedOutboundRequests();
            capturedLogs = new CapturedLogMessages();

            var root = new TestWebApplicationFactory<Program>();
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(
                        new ForwardedHeadersTestStartupFilter(IPAddress.Parse(TrustedProxy)));

                    services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                        new OutboundRequestRecordingFilter(outbound));

                    services.RemoveAll<ILoggerFactory>();
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                        new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(capturedLogs).CreateLogger(),
                        dispose: true));
                });

                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "false",
                        ["Authentication:TrustedProxies:0"] = TrustedProxy,
                        ["RateLimits:Enabled"] = "true",
                        ["RateLimits:Policies:UsageDataIngest:PermitLimit"] =
                            PermitLimit.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:UsageDataIngest:WindowSeconds"] =
                            WindowSeconds.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:UsageDataIngest:QueueLimit"] = "0",
                        ["RateLimits:Policies:UsageDataConsent:PermitLimit"] =
                            ConsentPermitLimit.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:UsageDataConsent:WindowSeconds"] =
                            WindowSeconds.ToString(CultureInfo.InvariantCulture),
                        ["RateLimits:Policies:UsageDataConsent:QueueLimit"] = "0",
                        ["UsageData:DailyEventBudget"] =
                            DailyAllowance.ToString(CultureInfo.InvariantCulture),
                        ["UsageData:CollectorBaseUrl"] = CollectorAddress,
                    });
                });
            });
        }
    }
}
