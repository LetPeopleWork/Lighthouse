using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// Epic 5733 slice 02 (#5835) — the unprompted ask, black box over HTTP.
    ///
    /// Black box for the reason slice 01's endpoint tests give: C# is compiled, so naming a type
    /// this slice has not built yet breaks the whole test assembly rather than reddening one test.
    /// Everything here names only types that exist today and asserts on the serialised state
    /// document, so the assembly builds and each scenario fails on a payload or status assertion
    /// once it is un-ignored.
    ///
    /// What this file deliberately cannot cover: the cadence arithmetic itself — that a Community
    /// refusal comes back due after about three months. Moving a clock three months forward is not
    /// something an HTTP call can do, and the service method that derives it takes a field this
    /// slice adds. That assertion is owed as a unit test in DELIVER, against the injected
    /// TimeProvider, and is named in the feature delta so it cannot be quietly dropped.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataAskEndpointsTests : IntegrationTestBase
    {
        public UsageDataAskEndpointsTests()
            : base(new HostThatDoesNotThrottleConsent())
        {
        }

        /// <summary>
        /// The consent endpoints share one bucket of twenty anonymous requests a minute, the window
        /// is process-wide, and <c>[SetUp]</c> resets the database but not the limiter - so a class
        /// of a dozen tests that each make two or three calls starts refusing partway through, and
        /// the failure arrives as an empty body rather than as anything about usage data.
        ///
        /// That the limit exists and bites is asserted in <c>UsageDataConsentRateLimitTests</c>,
        /// which builds its own host to saturate it deliberately. Nothing here is about the limiter,
        /// so nothing here should be competing with it.
        /// </summary>
        private sealed class HostThatDoesNotThrottleConsent : TestWebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["RateLimits:Policies:UsageDataConsent:PermitLimit"] = "1000",
                    }));
            }
        }

        private const string StateRoute = "/api/latest/usagedata/state";
        private const string ConsentRoute = "/api/latest/usagedata/consent";
        private const string AskedRoute = "/api/latest/usagedata/asked";
        private const string ConsentTokenHeader = "X-Lighthouse-UsageData-Token";

        // Written out rather than referenced. The gate holds this string privately, and a test that
        // reached for it would be asserting the product against itself: rename both together and the
        // administrator's switch silently stops governing anything while this stays green.
        private const string MasterSwitchKey = "UsageData";

        private static readonly string[] LicenceDisclosureNeedles = ["licen", "premium", "tier"];

        // The shipped default. Read from configuration rather than assumed, so a test that backdates
        // a row past "the window" keeps meaning that after somebody tunes the number.
        private int ReAskAfterDaysInTests =>
            ServiceProvider.GetRequiredService<IOptionsMonitor<UsageDataConfiguration>>()
                .CurrentValue.ReAskAfterDays;

        [Test]
        public async Task GetState_OnAnInstanceInstalledMomentsAgo_SaysTheBrowserIsNotDue()
        {
            await InstalledDaysAgo(0);

            Assert.That(await MayAskAsync(), Is.False,
                "the question is meant to arrive after somebody has actually used Lighthouse. "
                + "Asking on first launch is the version of this that gets dismissed reflexively, "
                + "which produces a number that means nothing");
        }

        [Test]
        public async Task GetState_OnceTheInstanceHasBeenInstalledLongEnough_SaysAnUndecidedBrowserIsDue()
        {
            await InstalledDaysAgo(14);

            Assert.That(await MayAskAsync(), Is.True,
                "a browser holding no token has never answered, and this is the whole population "
                + "the slice exists to put the question to");
        }

        [Test]
        public async Task GetState_WithTheAdministratorsSwitchOff_SaysNobodyIsDue()
        {
            await InstalledDaysAgo(14);
            await MasterSwitch(enabled: false);

            Assert.That(await MayAskAsync(), Is.False,
                "an administrator who switched usage data off has a policy to enforce, and a dialog "
                + "that still appears is the screenshot that fails their security review");
        }

        [Test]
        public async Task GetState_WithNoSwitchRowAtAll_StillSaysTheBrowserIsDue()
        {
            await InstalledDaysAgo(14);

            // The row does not exist until the slice that introduces the switch. Absent has to mean
            // "may ask", or shipping this slice ahead of that one silently asks nobody at all and
            // the uptake number the Epic rests on comes back zero for a reason nothing reports.
            Assert.That(await MayAskAsync(), Is.True);
        }

        [Test]
        public async Task GetState_ForABrowserThatAgreed_NeverSaysItIsDueAgain()
        {
            await InstalledDaysAgo(14);
            var token = await ReadTokenAsync(await Client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" }));

            Assert.That(await MayAskAsync(token), Is.False,
                "there is nothing left to ask somebody who already agreed, and asking anyway reads "
                + "as the product having forgotten");
        }

        [Test]
        public async Task GetState_ImmediatelyAfterARefusal_DoesNotSayTheBrowserIsDueAgain()
        {
            await InstalledDaysAgo(14);
            var token = await ReadTokenAsync(await Client.PostAsJsonAsync(ConsentRoute, new { decision = "declined" }));

            Assert.That(await MayAskAsync(token), Is.False,
                "whether the refusal is final or merely quiet for a few months, it is not due now. "
                + "Which of the two applies is a licence question, settled on the server so that the "
                + "tier never travels");
        }

        // The browser cannot hold the cadence for a dismissal without knowing how long it runs, and
        // it is the only party that knows a dismissal happened at all - so the number travels. It is
        // the same for everybody, which is what keeps it off the list of things this endpoint
        // discloses about a particular browser or instance.
        [Test]
        public async Task GetState_TellsTheBrowserHowLongADismissalShouldStayQuiet()
        {
            await InstalledDaysAgo(14);

            using var document = JsonDocument.Parse(await ReadStateAsync(token: null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(document.RootElement.TryGetProperty("reAskAfterDays", out var window), Is.True);
                Assert.That(window.GetInt32(), Is.GreaterThan(0),
                    "zero would mean a browser that closed the dialog is asked again on its very next "
                    + "visit, which is the nag this whole cadence exists to prevent");
            }
        }

        [Test]
        public async Task GetState_SaysWhetherToAsk_WithoutNamingTheLicence()
        {
            await InstalledDaysAgo(14);

            var state = await ReadStateAsync(token: null);

            using var document = JsonDocument.Parse(state);
            var properties = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    properties.Any(p => string.Equals(p, "mayAsk", StringComparison.OrdinalIgnoreCase)),
                    Is.True,
                    "the browser cannot work this out: the install timestamp sits behind "
                    + "authentication and the administrator's switch is not its to read");

                // The same guard the consent endpoints carry, restated because this field is derived
                // from the tier too. Over the whole body rather than the top-level names: a nested
                // cadence: { tier: "premium" } would pass a property-name check and still disclose it.
                Assert.That(
                    LicenceDisclosureNeedles.Any(needle =>
                        state.Contains(needle, StringComparison.OrdinalIgnoreCase)),
                    Is.False,
                    "this endpoint needs no authentication, and a second derived field must not be "
                    + "the one that widens what an anonymous caller learns about the instance");
            }
        }

        [Test]
        public async Task PostAsked_WithTheBrowsersOwnToken_StopsItBeingDueStraightAway()
        {
            await InstalledDaysAgo(14);

            // Genuinely due again - three months on from its refusal, which is the only state in
            // which this endpoint changes anything. Asserting against a refusal made a moment ago
            // proves nothing: such a browser is not due either way, so the test passed whether or
            // not the write happened, and mutation testing said so.
            var token = await ABrowserThatDeclinedDaysAgo("asked-me", ReAskAfterDaysInTests + 1);

            var dueBefore = await MayAskAsync(token);

            // It is shown the dialog again and closes it without answering. Nothing about its stored
            // decision changes, so without this the server keeps saying it is due and it is asked
            // once per session for ever - the nag this slice exists to rule out.
            var acknowledged = await SendWithTokenAsync(HttpMethod.Post, AskedRoute, token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dueBefore, Is.True,
                    "the browser has to actually be due, or nothing this endpoint does can show");
                Assert.That(acknowledged.IsSuccessStatusCode, Is.True);
                Assert.That(await MayAskAsync(token), Is.False);
            }
        }

        [Test]
        public async Task PostAsked_WithAWhitespaceToken_ChangesNothing()
        {
            await InstalledDaysAgo(14);
            var token = await ABrowserThatDeclinedDaysAgo("blank-header", ReAskAfterDaysInTests + 1);

            // A header carrying only spaces is not a token. Treating it as one would send a blank
            // digest to the store, and a row is only safe from that by never having hashed to it.
            var response = await SendWithTokenAsync(HttpMethod.Post, AskedRoute, "   ");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.IsSuccessStatusCode, Is.True);
                Assert.That(await MayAskAsync(token), Is.True,
                    "somebody else's blank header must not quiet this browser's question");
            }
        }

        [Test]
        public async Task PostAsked_LeavesEveryOtherBrowsersQuestionWhereItWas()
        {
            await InstalledDaysAgo(14);
            var mine = await ABrowserThatDeclinedDaysAgo("mine", ReAskAfterDaysInTests + 1);
            var theirs = await ABrowserThatDeclinedDaysAgo("theirs", ReAskAfterDaysInTests + 1);

            await SendWithTokenAsync(HttpMethod.Post, AskedRoute, mine);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await MayAskAsync(mine), Is.False);
                Assert.That(await MayAskAsync(theirs), Is.True,
                    "one browser reporting that it was asked must not answer for everybody else - a "
                    + "predicate that matched every row would silence the whole instance at once");
            }
        }

        [Test]
        public async Task PostAsked_WithNoTokenAtAll_IsAcceptedAndRecordsNothing()
        {
            await InstalledDaysAgo(14);

            var response = await Client.PostAsync(AskedRoute, content: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.IsSuccessStatusCode, Is.True,
                    "a browser with no consent row remembers being asked in its own storage and "
                    + "reports it here for symmetry. Refusing the call would make the client branch "
                    + "on something it cannot see");
                Assert.That(await MayAskAsync(), Is.True,
                    "there is no row to write to, so nothing on the server may change - otherwise "
                    + "one anonymous call would silence the dialog for every undecided browser on "
                    + "the instance");
            }
        }

        [Test]
        public async Task PostAsked_WithATokenThisInstanceNeverMinted_AnswersExactlyAsARealOneDoes()
        {
            await InstalledDaysAgo(14);
            var token = await ReadTokenAsync(await Client.PostAsJsonAsync(ConsentRoute, new { decision = "declined" }));

            var real = await SendWithTokenAsync(HttpMethod.Post, AskedRoute, token);
            var unknown = await SendWithTokenAsync(HttpMethod.Post, AskedRoute, "never-minted-here");

            using (Assert.EnterMultipleScope())
            {
                // Anchored, like the revoke oracle in slice 01: on an unrouted path both calls are
                // 405 and equal, so the comparison on its own is green before anything exists.
                Assert.That(real.IsSuccessStatusCode, Is.True,
                    "the real call must succeed before its status means anything as a baseline");
                Assert.That(unknown.StatusCode, Is.EqualTo(real.StatusCode),
                    "answering differently would turn this into a way of discovering which tokens "
                    + "are real on this instance");

                // The status code is not the only thing a caller can read. A header present on one
                // answer and absent on the other tells them the same thing the status would have,
                // and costs an attacker nothing to look at.
                Assert.That(HeadersOf(unknown), Is.EqualTo(HeadersOf(real)),
                    "the two answers have to be indistinguishable in what a caller can actually see, "
                    + "not only in their status line");
            }
        }

        /// <summary>
        /// Everything a caller can read off the response bar the parts that legitimately differ per
        /// request. Date moves with the clock and the trace identifier is unique by design, so
        /// including either would make the comparison fail for reasons that disclose nothing.
        /// </summary>
        private static string[] HeadersOf(HttpResponseMessage response)
        {
            string[] differPerRequestByDesign = ["Date", "traceparent", "Request-Id"];

            return [.. response.Headers
                .Concat(response.Content.Headers)
                .Where(header => !differPerRequestByDesign.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                .Select(header => $"{header.Key}: {string.Join(",", header.Value)}")
                .Order()];
        }

        [Test]
        public async Task PostAsked_NeedsNoAuthentication_LikeEverySiblingItStandsBeside()
        {
            await InstalledDaysAgo(14);

            var response = await Client.PostAsync(AskedRoute, content: null);

            using (Assert.EnterMultipleScope())
            {
                // Not "is not 401": a 404 is not 401 either, and that weaker assertion passes
                // against a product with no such endpoint at all.
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(response.IsSuccessStatusCode, Is.True);
            }
        }

        private async Task InstalledDaysAgo(int days)
        {
            var installedAt = DateTime.UtcNow.AddDays(-days).ToString("O");

            var existing = DatabaseContext.AppSettings
                .SingleOrDefault(setting => setting.Key == AppSettingKeys.InstallTimestamp);

            if (existing is null)
            {
                DatabaseContext.AppSettings.Add(
                    new AppSetting { Key = AppSettingKeys.InstallTimestamp, Value = installedAt });
            }
            else
            {
                existing.Value = installedAt;
            }

            await DatabaseContext.SaveChangesAsync();
        }

        private async Task MasterSwitch(bool enabled)
        {
            DatabaseContext.OptionalFeatures.Add(new OptionalFeature
            {
                Id = 0,
                Key = MasterSwitchKey,
                Name = "Usage Data",
                Description = "May Lighthouse ask this instance's users about usage data.",
                Enabled = enabled,
                IsPremium = true,
            });

            await DatabaseContext.SaveChangesAsync();
        }

        /// <summary>
        /// A browser that declined this many days ago and is therefore due to be asked again.
        ///
        /// Written straight to the store rather than through the consent endpoint, for two reasons.
        /// There is no endpoint that backdates an answer and there should not be - nothing a browser
        /// sends may move its own cadence. And the consent endpoints share one process-wide bucket
        /// of twenty anonymous requests a minute which <c>[SetUp]</c> does not reset, so a class that
        /// minted every fixture over HTTP would start failing on the twenty-first test for a reason
        /// that has nothing to do with what any of them assert.
        ///
        /// The token is hashed the way the server hashes it, which is what makes this the row that
        /// browser really holds rather than one that merely looks like it.
        /// </summary>
        private async Task<string> ABrowserThatDeclinedDaysAgo(string token, int days)
        {
            DatabaseContext.UsageDataConsents.Add(new UsageDataConsent
            {
                TokenHash = UsageDataConsentToken.HashOf(token),
                Decision = UsageDataDecision.Declined,
                DecidedAt = DateTime.UtcNow.AddDays(-days),
                LastSeenAt = DateTime.UtcNow,
            });

            await DatabaseContext.SaveChangesAsync();

            return token;
        }

        private async Task<bool> MayAskAsync(string? token = null)
        {
            using var document = JsonDocument.Parse(await ReadStateAsync(token));

            return document.RootElement.TryGetProperty("mayAsk", out var mayAsk)
                   && mayAsk.GetBoolean();
        }

        private async Task<string> ReadStateAsync(string? token)
        {
            var response = token is null
                ? await Client.GetAsync(StateRoute)
                : await SendWithTokenAsync(HttpMethod.Get, StateRoute, token);

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> SendWithTokenAsync(HttpMethod method, string route, string token)
        {
            using var request = new HttpRequestMessage(method, route);
            request.Headers.Add(ConsentTokenHeader, token);

            return await Client.SendAsync(request);
        }

        private static async Task<string> ReadTokenAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("token", out var token)
                ? token.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
