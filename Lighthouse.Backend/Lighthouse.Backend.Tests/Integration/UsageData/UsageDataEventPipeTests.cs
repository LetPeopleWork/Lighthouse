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
    /// Epic 5733 slice 01c (ADO #5980) - the event pipe, black box over HTTP.
    ///
    /// Black box for the reason the slice 01 fixture beside this one already gives: C# is compiled,
    /// so a pending test naming the ingest message, the gate, the queue or the publisher would break
    /// the whole test assembly's build. A broken build is not a failing test - it stops every other
    /// suite in the project from running and it trips the zero-warning gate. Everything here names
    /// only types that exist today, so the assembly builds and each scenario fails on its own
    /// assertion the moment it is un-ignored.
    ///
    /// What this host is: the real application through the real HTTP surface, the real database over
    /// SQLite, and a recorder in front of every client the framework hands out - which is how
    /// "nothing was sent" becomes something a test can see rather than something it infers from a
    /// double that was not called.
    ///
    /// Two things the implementation has to supply before the ignored scenarios can pass, and they
    /// are not oversights here:
    ///
    /// 1. The integration host removes every background service, so whatever drains the accepted
    ///    batches does not run inside it. The design already names the seam the controller writes to
    ///    for exactly this reason: give the drain a trigger a test can pull and replace
    ///    <see cref="EverythingTheCollectorReceived"/> with it. Polling is the fallback, not the aim.
    /// 2. This host is told, in configuration, to send somewhere that cannot exist. There is exactly
    ///    one real collector project and it is the live census the maintainer reads, so a test that
    ///    reached it would invent events in the real numbers - and would do it silently, because
    ///    sending degrades without complaint by specification. Every scenario is checked against
    ///    that on the way out, whatever it was about.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataEventPipeTests
    {
        private const string EventsRoute = "/api/latest/usagedata/events";
        private const string ConsentRoute = "/api/latest/usagedata/consent";
        private const string ConsentTokenHeader = "X-Lighthouse-UsageData-Token";
        private const string JsonMediaType = "application/json";

        // Where this host is told to send. There is exactly one real collector project and it is the
        // live census, so no test may aim at it even by accident: a .invalid address can never
        // resolve, and the recorder answers in place of the network in any case.
        private const string CollectorAddress = "https://collector.usage-data-tests.invalid/";
        private const string CollectorHost = "collector.usage-data-tests.invalid";

        // The address a Lighthouse that was never told where to send would fall back to. Named here
        // so that every scenario in this fixture can be shown not to have reached it.
        private const string TheLiveCensusHost = "posthog.com";

        // The single product event this slice carries, and the one label that has to survive a page
        // address which identifies one of the customer's own Teams.
        private const string TabOpened = "TeamOrPortfolioTabOpened";
        private const string TeamMetricsTab = "TeamDetail_Metrics";

        private const string NoIngestEndpointYet =
            "Pending: the ingest endpoint this asserts against does not exist yet (Epic 5733 slice 01c, ADO #5980).";

        private static readonly string[] EverythingRecordingAnAnswerHandsBack = ["token"];

        // Widened deliberately for slice 02, and again for slice 03. mayAsk is a derived boolean
        // carrying nothing an anonymous caller could not infer by waiting to be asked;
        // reAskAfterDays is a single configuration number, identical for every caller, and says
        // nothing about this browser or this instance's tier. administratorDisabled says that
        // somebody who runs this instance stopped usage data - which AC-06.7 requires be disclosed,
        // because without it a reader who agreed and was overruled reads the silence as their own
        // refusal. It names no tier and no person. Anything else arriving here should fail this
        // test again rather than be added to the list.
        private static readonly string[] EverythingTheStateAnswerCarries =
            ["sending", "decision", "mayAsk", "reAskAfterDays", "administratorDisabled"];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private CapturedOutboundRequests outbound = null!;
        private CapturedLogMessages capturedLogs = null!;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            outbound = new CapturedOutboundRequests();
            capturedLogs = new CapturedLogMessages();

            factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                        new OutboundRequestRecordingFilter(outbound));

                    // Serilog is the pipeline here, so a logging provider added instead of the
                    // factory would be dropped and every log assertion below would read an empty
                    // list and pass.
                    services.RemoveAll<ILoggerFactory>();
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                        new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(capturedLogs).CreateLogger(),
                        dispose: true));
                });

                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["UsageData:CollectorBaseUrl"] = CollectorAddress,
                    });
                });
            });

            client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            databaseContext.Database.EnsureDeleted();
            databaseContext.Database.EnsureCreated();
        }

        /// <summary>
        /// Runs after every scenario in this fixture, whatever it was about. There is one real
        /// collector project and it is the live census - a test that reached it would put invented
        /// events into the numbers the maintainer reads, and would do it silently, because sending
        /// degrades without complaint by specification.
        /// </summary>
        [TearDown]
        public void Cleanup()
        {
            var reachedTheCensus = outbound.ThatReached(TheLiveCensusHost);

            using (var scope = factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();

            Assert.That(reachedTheCensus, Is.Empty,
                "a scenario in this fixture contacted the real collector. This host is told to send "
                + "somewhere that cannot exist, so reaching it means something ignored where it was "
                + "told to send - and the events it invented are now in the live numbers");
        }

        /// <summary>
        /// Not ignored, and deliberately so. Every scenario below that asserts nothing was sent is
        /// worthless if this harness is blind, and a blind harness looks exactly like an application
        /// that sends nothing. This is what tells the two apart.
        /// </summary>
        [Test]
        public async Task NothingWasSent_IsSomethingThisFixtureCanActuallyTell()
        {
            var httpClientFactory = factory.Services.GetRequiredService<IHttpClientFactory>();
            using var outboundClient = httpClientFactory.CreateClient("Default");

            using var answer = await outboundClient.PostAsync(
                new Uri($"{CollectorAddress}i/v0/e/"),
                new StringContent("{\"event\":\"a message only this test sends\"}", Encoding.UTF8, JsonMediaType));

            var received = outbound.EverythingSentTo(CollectorHost);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outbound.SawAnything, Is.True,
                    "the recorder saw no request at all, so every 'nothing reached the collector' "
                    + "assertion in this fixture would hold no matter what the application did");
                Assert.That(received, Does.Contain("a message only this test sends"),
                    "the recorder saw the request but not what it carried, so no claim about what "
                    + "is in a forwarded message could be checked against it");
                Assert.That(answer.IsSuccessStatusCode, Is.True,
                    "the recorder answers in place of the network; a caller handed an error would "
                    + "take a failure path no real caller takes");
            }
        }

        [Test]
        public async Task ABrowserThatAgreed_CanHandInWhatItSawWithoutBeingToldAnything()
        {
            var token = await ABrowserThatAgreedAsync();

            using var accepted = await HandInAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var body = await accepted.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "the endpoint answers the same way whatever it decided, so a caller cannot use "
                    + "it to find out whether the token it presented is one this instance minted");
                Assert.That(body, Is.Empty,
                    "a body would carry the decision the status code deliberately withholds");
            }
        }

        /// <summary>
        /// The scenario the whole design exists for. A page address like /teams/42/metrics names one
        /// of the customer's own Teams, and the tab beside it is one of the few things worth
        /// knowing - so this has to lose the first, keep the second, and never have trusted the
        /// browser with either.
        /// </summary>
        [Test]
        public async Task ATabOpenedOnATeam_ReachesTheCollectorNamingTheTabAndNeverTheTeam()
        {
            var token = await ABrowserThatAgreedAsync();

            using var accepted = await HandInAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var received = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(received, Is.Not.Empty,
                    "nothing reached the collector at all, so the two claims below would hold for a "
                    + "pipe that simply does not work");
                Assert.That(received, Does.Contain("metrics").IgnoreCase,
                    "which view somebody opened is the whole question this event answers; without "
                    + "the tab it is a page view with a longer name");
                Assert.That(received, Does.Not.Match("/teams/[0-9]"),
                    "that is what a customer's Team identifier looks like on the way past. The label "
                    + "is chosen in the browser from a fixed list, so there is no address here to "
                    + "shorten and nothing that could carry one of their entities");
            }
        }

        /// <summary>
        /// The trap this pipe was warned about. Saying "No, thank you" also mints a token, by design
        /// and for a good reason - so a browser that refused holds one, and a check phrased as "is
        /// there a token" sends on behalf of people who said in as many words not to. Every test
        /// written before this one still passes when that mistake is made.
        /// </summary>
        [Test]
        public async Task ABrowserThatRefused_SendsNothingEvenThoughItHoldsAToken()
        {
            var token = await ABrowserThatRefusedAsync();

            using var answer = await HandInAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var received = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(token, Is.Not.Empty,
                    "a refusal that minted no token would make this scenario describe a different "
                    + "browser from the one it is about");
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "refusing the request outright would tell the caller its token was recognised");
                Assert.That(received, Is.Empty,
                    "somebody declined, and this instance sent on their behalf anyway");
            }
        }

        /// <summary>
        /// The browser decides what to show; it does not decide what leaves. This endpoint is
        /// reachable by anyone who can reach the instance, so it has to assume its caller is hostile
        /// and check for itself, on every request.
        /// </summary>
        [Test]
        public async Task ACallerWithNoConsentBehindIt_SendsNothingHoweverWellFormedItsMessageIs()
        {
            using var answer = await HandInAsync(token: null, ABatchOf(TabOpened, TeamMetricsTab));
            var received = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(received, Is.Empty,
                    "hiding a button is a decision about a screen. Anyone can post here, so this is "
                    + "the only place an answer can actually be enforced");
            }
        }

        [Test]
        public async Task ABrowserThatChangedItsMind_SendsNothingOnItsVeryNextEvent()
        {
            var token = await ABrowserThatAgreedAsync();

            using var whileConsenting = await HandInAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var sentWhileConsenting = await EverythingTheCollectorReceived();

            await WithdrawAsync(token);
            outbound.Clear();

            using var afterWithdrawing = await HandInAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var sentAfterWithdrawing = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whileConsenting.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(sentWhileConsenting, Is.Not.Empty,
                    "nothing was sent while consent was live either, so the claim below would hold "
                    + "for a pipe that never worked - which is the reading this scenario exists to "
                    + "rule out");
                Assert.That(afterWithdrawing.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(sentAfterWithdrawing, Is.Empty,
                    "no restart, no cycle boundary, nothing remembered in between: the record is "
                    + "read again on the very next batch");
            }
        }

        /// <summary>
        /// The window a queue opens. A batch taken in a moment before somebody withdrew must not be
        /// sent afterwards, which is why nothing on this path is written down anywhere that could
        /// outlive the decision.
        /// </summary>
        [Test]
        public async Task AWithdrawalWhileABatchIsStillWaiting_StopsThatBatchToo()
        {
            var token = await ABrowserThatAgreedAsync();

            using var accepted = await HandInAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            await WithdrawAsync(token);

            var received = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "the batch has to have been taken in for this to be about waiting at all");
                Assert.That(received, Is.Empty,
                    "anything held on to survives a withdrawal and sends afterwards, which is the "
                    + "one thing somebody pressing that button is entitled to assume cannot happen");
            }
        }

        /// <summary>
        /// This held in the previous slice for the wrong reason: nothing could reach the collector
        /// because no client to it existed, so there was nothing for the check to find. It starts
        /// meaning something only once one does.
        /// </summary>
        [Test]
        public async Task AnInstanceNobodyHasAnsweredOn_ReachesTheCollectorNotOnce()
        {
            using var answer = await HandInAsync(token: null, ABatchOf(TabOpened, TeamMetricsTab));
            var received = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(received, Is.Empty,
                    "before anybody has agreed, an instance that contacts the collector has already "
                    + "broken the only promise this feature makes");
            }
        }

        [Test]
        public async Task AMessageCarryingARealPageAddress_DoesNotEvenParse()
        {
            var token = await ABrowserThatAgreedAsync();

            using var refused = await HandInAsync(token, ABatchOf(TabOpened, "/teams/42/metrics"));
            var received = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                    "the route is a label from a closed list rather than text. An address is not a "
                    + "member of that list, so this is a message the server cannot read at all, not "
                    + "one it reads and then tidies up");
                Assert.That(received, Is.Empty,
                    "a refused message that still reached the collector would mean the address "
                    + "existed inside this process long enough for something to copy it out");
            }
        }

        [Test]
        public async Task AnEventThisReleaseDoesNotDescribe_CannotBeSent()
        {
            var token = await ABrowserThatAgreedAsync();

            using var refused = await HandInAsync(token, ABatchOf("SomethingNobodyWroteDown", TeamMetricsTab));

            Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                "an event that ships ahead of the page describing it means the product sends data "
                + "it has not declared. A closed list makes that a refusal rather than something "
                + "somebody has to catch in review");
        }

        [Test]
        [TestCase("", TestName = "AMalformedToken_IsAnsweredExactlyAsAnUnknownOneIs(empty)")]
        [TestCase("aa", TestName = "AMalformedToken_IsAnsweredExactlyAsAnUnknownOneIs(truncated)")]
        [TestCase("not base64url!!", TestName = "AMalformedToken_IsAnsweredExactlyAsAnUnknownOneIs(wrong alphabet)")]
        public async Task AMalformedToken_IsAnsweredExactlyAsAnUnknownOneIs(string malformed)
        {
            using var unknown = await HandInAsync("neverMintedHere", ABatchOf(TabOpened, TeamMetricsTab));
            using var answer = await HandInAsync(malformed, ABatchOf(TabOpened, TeamMetricsTab));

            var unknownBody = await unknown.Content.ReadAsStringAsync();
            var answerBody = await answer.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "the baseline has to be the real answer before anything is compared against it");
                Assert.That(answer.StatusCode, Is.EqualTo(unknown.StatusCode),
                    "the moment a badly shaped token is answered differently from an unrecognised "
                    + "one, the endpoint has told a caller what a real token looks like - and shape "
                    + "plus a rate limit is where guessing starts");
                Assert.That(answerBody, Is.EqualTo(unknownBody));
            }
        }

        [Test]
        [TestCase("{\"events\":[{\"route\":\"TeamDetail_Metrics\"}]}", TestName = "AMessageMissingAPart_IsRefusedRatherThanGuessed(no event named)")]
        [TestCase("{\"events\":[{\"name\":\"TeamOrPortfolioTabOpened\"}]}", TestName = "AMessageMissingAPart_IsRefusedRatherThanGuessed(no route named)")]
        [TestCase("{\"events\":[]}", TestName = "AMessageMissingAPart_IsRefusedRatherThanGuessed(nothing at all)")]
        public async Task AMessageMissingAPart_IsRefusedRatherThanGuessed(string body)
        {
            var token = await ABrowserThatAgreedAsync();

            using var refused = await HandInAsync(token, body);

            Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                "a whole-number field left out of a message arrives as zero unless it is written to "
                + "allow absence, and zero is a real member of both of these lists - so the message "
                + "would be read as naming something nobody sent");
        }

        [Test]
        [TestCase("{\"events\":[{\"name\":999,\"route\":42}]}", TestName = "AHostileMessage_IsRefusedWithoutTheInstanceFalling(outside both lists)")]
        [TestCase("{\"events\":[{\"name\":\"TeamOrPortfolioTabOpened\",\"route\":\"TeamDetail_Metrics\",\"offsetMs\":-2147483648}]}", TestName = "AHostileMessage_IsRefusedWithoutTheInstanceFalling(impossible offset)")]
        [TestCase("not a message at all", TestName = "AHostileMessage_IsRefusedWithoutTheInstanceFalling(not a message)")]
        public async Task AHostileMessage_IsRefusedWithoutTheInstanceFalling(string body)
        {
            var token = await ABrowserThatAgreedAsync();

            using var answer = await HandInAsync(token, body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)answer.StatusCode, Is.LessThan(500),
                    "this is new anonymous write surface on a product that is often reachable from "
                    + "the internet; a message that knocks the request over is a way to make it say "
                    + "things about itself");
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        /// <summary>
        /// The facts about the instance are attached here rather than asked of the browser, so a
        /// caller cannot choose them and a page cannot leak them. A test host is not a published
        /// release, which is what makes the version assertion below meaningful rather than lucky.
        /// </summary>
        [Test]
        public async Task TheFactsAboutTheInstance_AreAddedHereAndNeverAskedOfTheBrowser()
        {
            var token = await ABrowserThatAgreedAsync();

            using var accepted = await HandInAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var received = await EverythingTheCollectorReceived();
            var aboutTheCaller = EverythingSaidAboutTheCaller();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(received, Is.Not.Empty,
                    "nothing was forwarded, so every claim about what it carried is vacuous");
                Assert.That(aboutTheCaller, Is.Not.Empty,
                    "nothing forwarded was a message this could read the two instructions out of, "
                    + "so the two claims about them would hold for a message that gave neither");
                Assert.That(received, Does.Contain("unreleased"),
                    "a version built from somebody's working tree is close to unique, and the people "
                    + "running one are the people most likely to be recognisable from it. A test "
                    + "host is exactly such a build");
                Assert.That(
                    aboutTheCaller.Select(message => message.Address), Is.All.EqualTo(JsonValueKind.Null),
                    "the message carries its own instruction to discard the address, so the promise "
                    + "does not depend on a setting in somebody else's console staying where it was. "
                    + "What it is set to is the whole of the instruction: a message handing over a "
                    + "real address writes the very same field, under the very same name");
                Assert.That(
                    aboutTheCaller.Select(message => message.LocationLookup), Is.All.EqualTo(JsonValueKind.True),
                    "location lookup is switched off in the message itself for the same reason, and "
                    + "reads the same way round - asked for rather than refused, the field is still "
                    + "there and still called this");
            }
        }

        /// <summary>
        /// A browser that withdrew and left a tab open flushes forever. One line per flush would
        /// fill a disk, and the instance most likely to fill one is the instance whose owner already
        /// withdrew - so the outcome is counted and an operator reads a daily total.
        /// </summary>
        [Test]
        public async Task ABrowserThatWithdrewAndKeepsFlushing_AddsNoLinePerFlush()
        {
            var token = await ABrowserThatAgreedAsync();
            await WithdrawAsync(token);
            capturedLogs.Clear();

            var flushes = 20;
            var accepted = 0;
            for (var flush = 0; flush < flushes; flush++)
            {
                using var answer = await HandInAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
                if (answer.StatusCode == HttpStatusCode.NoContent)
                {
                    accepted++;
                }
            }

            // Lines this application wrote, not every line that happens to mention the route. The
            // endpoint is /usagedata/events and the controller is UsageDataController, so the
            // framework's own request pipeline writes twenty-odd matches per request before we log
            // anything at all - counting those would measure ASP.NET, not us.
            var aboutUsageData = capturedLogs.AtOrAbove(LogEventLevel.Verbose)
                .Where(line => line.Contains("Usage data:", StringComparison.Ordinal))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accepted, Is.EqualTo(flushes),
                    "the flushes have to have reached the endpoint for their absence from the log "
                    + "to say anything about the log");
                Assert.That(aboutUsageData, Has.Count.LessThanOrEqualTo(1),
                    "one line per suppressed flush turns the defence into the flood, and whoever "
                    + "can trigger it is whoever already withdrew. The bound is the one total an "
                    + "operator is meant to read, not twenty flushes minus one: anything that "
                    + "grows with how often a withdrawn browser flushes is the thing this forbids. "
                    + "Counted across every level, because demoting the line to Debug does not "
                    + "stop it filling a disk");
            }
        }

        /// <summary>
        /// The value that lets the collector tell a repeat visit from a new one belongs to this
        /// browser, is worked out on the server from the token it presents, and is never a field in
        /// any message in either direction. Not ignored, because it can be broken today: the moment
        /// it appears in one of these answers, a caller can choose it - and emit under somebody
        /// else's, or poison a group with one of their own.
        ///
        /// Asserting the whole set rather than the absence of one field, because the way it would
        /// arrive is somebody adding it to the browser's own answer for convenience, under a name
        /// nobody thought to forbid.
        /// </summary>
        [Test]
        public async Task TheValueThatTellsARepeatVisitFromANewOne_NeverCrossesTheWire()
        {
            using var recorded = await client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" });
            var handedBack = await recorded.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(handedBack);
            var token = document.RootElement.GetProperty("token").GetString() ?? string.Empty;

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/latest/usagedata/state");
            request.Headers.Add(ConsentTokenHeader, token);
            using var state = await client.SendAsync(request);
            var stateBody = await state.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(EveryFieldIn(handedBack), Is.EquivalentTo(EverythingRecordingAnAnswerHandsBack),
                    "recording an answer hands back something beyond the token. Anything else here "
                    + "is in the browser's hands, and the one value that must stay on the server is "
                    + "the one this endpoint is most likely to be asked to return");
                Assert.That(EveryFieldIn(stateBody), Is.EquivalentTo(EverythingTheStateAnswerCarries),
                    "the browser's own status answer has grown a field. It is anonymous and it is "
                    + "read hourly from every open tab, so whatever is added here is readable by "
                    + "anyone who can reach the instance");
            }
        }

        private static List<string> EveryFieldIn(string body)
        {
            using var document = JsonDocument.Parse(body);
            return [.. document.RootElement.EnumerateObject().Select(property => property.Name)];
        }

        /// <summary>
        /// What each message that reached the collector said about the caller it came from, read out
        /// of the bytes that were actually sent.
        ///
        /// Read as values rather than looked for as names, because a name is written out whatever it
        /// holds: a message handing the caller's address over and a message telling the collector to
        /// throw it away are the same text apart from what comes after the colon. A part that is
        /// missing altogether reads as nothing said, which is the promise not made - left to itself
        /// the collector takes the address from the connection and looks up where it is.
        /// </summary>
        private List<WhatAMessageSaidAboutTheCaller> EverythingSaidAboutTheCaller()
        {
            return [.. outbound.ThatReached(CollectorHost)
                .SelectMany(request => WhatWasSaidIn(request.Body))];
        }

        private static List<WhatAMessageSaidAboutTheCaller> WhatWasSaidIn(string body)
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind is not JsonValueKind.Array)
            {
                return [];
            }

            return [.. document.RootElement.EnumerateArray().Select(message =>
                new WhatAMessageSaidAboutTheCaller(
                    WhatWasSetIn(message, "properties", "$ip"),
                    WhatWasSetIn(message, "properties", "$geoip_disable")))];
        }

        private static JsonValueKind WhatWasSetIn(JsonElement message, string carrier, string instruction)
        {
            if (message.ValueKind is not JsonValueKind.Object
                || !message.TryGetProperty(carrier, out var carried)
                || carried.ValueKind is not JsonValueKind.Object)
            {
                return JsonValueKind.Undefined;
            }

            return carried.TryGetProperty(instruction, out var value) ? value.ValueKind : JsonValueKind.Undefined;
        }

        private sealed record WhatAMessageSaidAboutTheCaller(JsonValueKind Address, JsonValueKind LocationLookup);

        private async Task<string> ABrowserThatAgreedAsync()
        {
            return await ADecisionRecordedAsync("granted");
        }

        private async Task<string> ABrowserThatRefusedAsync()
        {
            return await ADecisionRecordedAsync("declined");
        }

        private async Task<string> ADecisionRecordedAsync(string decision)
        {
            using var response = await client.PostAsJsonAsync(ConsentRoute, new { decision });
            var body = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("token", out var token)
                ? token.GetString() ?? string.Empty
                : string.Empty;
        }

        private async Task WithdrawAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, ConsentRoute);
            request.Headers.Add(ConsentTokenHeader, token);

            using var response = await client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                "the withdrawal did not go through, so whatever happens next is not about having "
                + "withdrawn");
        }

        private async Task<HttpResponseMessage> HandInAsync(string? token, string body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EventsRoute)
            {
                Content = new StringContent(body, Encoding.UTF8, JsonMediaType),
            };

            if (token is not null)
            {
                request.Headers.Add(ConsentTokenHeader, token);
            }

            return await client.SendAsync(request);
        }

        /// <summary>
        /// Everything the collector was sent, as one piece of text, after emptying what is waiting.
        /// The drain is asked for here rather than waited out: this host runs no background work, so
        /// waiting would have meant every scenario paying a fixed budget long enough to be reliable
        /// on a loaded build agent - including every scenario whose whole point is that nothing was
        /// sent, which is most of them.
        /// </summary>
        private async Task<string> EverythingTheCollectorReceived()
        {
            await factory.Services.GetRequiredService<UsageDataForwardingService>()
                .SendWhatIsWaitingAsync(CancellationToken.None);

            return outbound.EverythingSentTo(CollectorHost);
        }

        private static string ABatchOf(string name, string route)
        {
            return $"{{\"events\":[{{\"name\":\"{name}\",\"route\":\"{route}\",\"offsetMs\":0,\"sequence\":0}}]}}";
        }
    }
}
