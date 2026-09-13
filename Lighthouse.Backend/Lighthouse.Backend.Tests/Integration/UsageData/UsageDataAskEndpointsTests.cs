using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Tests.TestHelpers;

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
        private const string StateRoute = "/api/latest/usagedata/state";
        private const string ConsentRoute = "/api/latest/usagedata/consent";
        private const string AskedRoute = "/api/latest/usagedata/asked";
        private const string ConsentTokenHeader = "X-Lighthouse-UsageData-Token";

        // Written out rather than referenced. The gate holds this string privately, and a test that
        // reached for it would be asserting the product against itself: rename both together and the
        // administrator's switch silently stops governing anything while this stays green.
        private const string MasterSwitchKey = "UsageData";

        private static readonly string[] LicenceDisclosureNeedles = ["licen", "premium", "tier"];

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
                + "Which of the two applies is a licence question, and the endpoint already answers "
                + "it separately through willAskAgain without naming the tier");
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

                // The same guard slice 01 put on willAskAgain, restated because this field is derived
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
            var token = await ReadTokenAsync(await Client.PostAsJsonAsync(ConsentRoute, new { decision = "declined" }));

            // Three months on, the same browser is shown the dialog again and closes it without
            // answering. Nothing about its stored decision changed, so without this the server keeps
            // saying it is due and it is asked once per session for ever - the nag this slice exists
            // to rule out.
            var acknowledged = await SendWithTokenAsync(HttpMethod.Post, AskedRoute, token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(acknowledged.IsSuccessStatusCode, Is.True);
                Assert.That(await MayAskAsync(token), Is.False);
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
            }
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
