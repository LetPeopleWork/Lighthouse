using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// Epic 5733 slice 01 (#5834) — the three consent endpoints, black box over HTTP.
    ///
    /// Black box because C# is compiled: a test that referenced the consent entity, the gate or the
    /// publisher would break the whole test assembly's build, which is BROKEN rather than RED and
    /// also trips the zero-warning gate. Everything here names only types that already exist, so the
    /// assembly builds today and each scenario fails on a status-code or payload assertion once it
    /// is un-ignored. The store-level guarantees (conditional revoke, the throttled liveness touch,
    /// the multi-replica heartbeat compare-and-swap) need the entity and are authored in DELIVER,
    /// against Testcontainers.PostgreSql per the ATDD infrastructure policy.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataConsentEndpointsTests : IntegrationTestBase
    {
        private const string StateRoute = "/api/latest/usagedata/state";
        private const string ConsentRoute = "/api/latest/usagedata/consent";
        private const string ConsentTokenHeader = "X-Lighthouse-UsageData-Token";

        private const string Pending = "pending: epic 5733 slice 01a (#5834)";

        private static readonly string[] LicenceDisclosureNeedles = ["licen", "premium", "tier"];

        [Test]
        [Ignore(Pending + " — the state endpoint does not exist yet")]
        public async Task GetState_WithNoTokenAtAll_AnswersRatherThanRefusing()
        {
            var response = await Client.GetAsync(StateRoute);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "a browser that has never decided still has to be told what the instance is doing, "
                + "and on an auth-off instance there is nothing to authenticate against");
        }

        [Test]
        [Ignore(Pending + " — the state endpoint does not exist yet")]
        public async Task GetState_WithAnUnknownToken_AnswersExactlyAsItDoesWithNoToken()
        {
            var baseline = await Client.GetAsync(StateRoute);
            var withoutToken = await baseline.Content.ReadAsStringAsync();
            var withUnknownToken = await ReadStateAsync(token: "a-token-this-instance-never-minted");

            using (Assert.EnterMultipleScope())
            {
                // Anchor first. Without it this test compares two responses to each other and
                // nothing else, so two empty 404 bodies are equal and it passes before the
                // endpoint exists - and would keep passing if the oracle ever leaked through the
                // status code or a header rather than the body.
                Assert.That(baseline.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(withoutToken, Does.Contain("willAskAgain").IgnoreCase,
                    "the baseline must actually be the state document before comparing anything to it");

                Assert.That(withUnknownToken, Is.EqualTo(withoutToken),
                    "an endpoint that answers differently for a token it has never seen is an oracle: "
                    + "it would let anyone test whether a given token exists on this instance");
            }
        }

        [Test]
        [Ignore(Pending + " — the state endpoint does not exist yet")]
        public async Task GetState_TellsTheBrowserWhetherItWillBeAskedAgain_WithoutNamingTheLicence()
        {
            var state = await ReadStateAsync(token: null);

            using var document = JsonDocument.Parse(state);
            var properties = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(properties.Any(p => string.Equals(p, "willAskAgain", StringComparison.OrdinalIgnoreCase)),
                    Is.True,
                    "the dialog's copy depends on it, and deriving it server-side is what keeps the "
                    + "licence tier off an endpoint that needs no authentication");
                // Over the whole serialised body, not just its top-level property names: a nested
                // object such as cadence: { tier: "premium" } discloses the tier and would pass a
                // top-level-only check.
                Assert.That(
                    LicenceDisclosureNeedles.Any(needle =>
                        state.Contains(needle, StringComparison.OrdinalIgnoreCase)),
                    Is.False,
                    "this endpoint is anonymous; disclosing the licence tier through it would widen "
                    + "what an unauthenticated caller learns about the instance");
            }
        }

        [Test]
        [Ignore(Pending + " — the state endpoint does not exist yet")]
        public async Task GetState_ForbidsCaching()
        {
            var response = await Client.GetAsync(StateRoute);

            Assert.That(response.Headers.CacheControl?.NoStore, Is.True,
                "a caching proxy in front of this endpoint would serve one browser's state to "
                + "another, and would suppress the liveness touch so consent decays under an active "
                + "user without anyone doing anything");
        }

        [Test]
        [Ignore(Pending + " — the consent endpoint does not exist yet")]
        public async Task PostConsent_WhenTheBrowserAgrees_MintsATokenForIt()
        {
            var response = await Client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(await ReadTokenAsync(response), Is.Not.Empty,
                    "the token is the consent record's only handle; without it the browser cannot "
                    + "later revoke, and the decision is unrevokable by the person who made it");
            }
        }

        [Test]
        [Ignore(Pending + " — the consent endpoint does not exist yet")]
        public async Task PostConsent_WhenTheBrowserDeclines_AlsoMintsAToken()
        {
            var response = await Client.PostAsJsonAsync(ConsentRoute, new { decision = "declined" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(await ReadTokenAsync(response), Is.Not.Empty,
                    "a refusal has to be remembered or the browser is asked again on every load. "
                    + "The token whose only content is the user's own choice is what makes "
                    + "remembering a refusal legitimate without prior consent");
            }
        }

        [Test]
        [Ignore(Pending + " — the consent endpoint does not exist yet")]
        public async Task PostConsent_MintsADifferentTokenEveryTime()
        {
            var first = await ReadTokenAsync(await Client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" }));
            var second = await ReadTokenAsync(await Client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" }));

            Assert.That(second, Is.Not.EqualTo(first),
                "two browsers must not be able to collide, and a guessable token would let one "
                + "browser revoke another's consent or keep an instance emitting");
        }

        /// <remarks>
        /// BEFORE UN-IGNORING: move this to its own fixture with its own
        /// <c>TestWebApplicationFactory</c> overriding PermitLimit and WindowSeconds, and a forwarded
        /// client IP - the shape <c>S6_RateLimitingTests</c> already uses. As written it saturates the
        /// bucket on the fixture's single shared host, and the default window is 60 seconds, so every
        /// other PostConsent test in this class would then red deterministically. <c>[SetUp]</c> resets
        /// the database, not the limiter. A serial fixture also needs an allowlist entry in
        /// <c>BackendTestParallelizationGuardTest</c>.
        /// </remarks>
        [Test]
        [Ignore(Pending + " — the consent endpoint does not exist yet; see the remarks before un-ignoring")]
        public async Task PostConsent_IsRateLimited()
        {
            var permitted = 0;
            HttpStatusCode? refused = null;

            for (var attempt = 0; attempt < 200 && refused is null; attempt++)
            {
                var response = await Client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" });
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    refused = response.StatusCode;
                }
                else
                {
                    permitted++;
                }
            }

            using (Assert.EnterMultipleScope())
            {
                // Without this, a policy misconfigured to PermitLimit 0 - refusing the very first
                // call - satisfies the test that exists to prove the endpoint still works.
                Assert.That(permitted, Is.GreaterThan(0),
                    "a limiter that refuses everyone is not a limiter, it is an outage");
                Assert.That(refused, Is.EqualTo(HttpStatusCode.TooManyRequests),
                    "the endpoint is unauthenticated and writes a durable row per call. Unlimited, a "
                    + "single caller could plant a granted row and keep an instance emitting for a "
                    + "whole liveness window");
            }
        }

        /// <remarks>
        /// This asserts what the state endpoint answers after a revoke, which is all that can be
        /// asserted while nothing emits. The criterion it looks like it covers - that the next emit
        /// does not happen, observed at the emit path - is a different assertion against a component
        /// that does not exist until the emitter ships, and it is owed there. An earlier name for
        /// this test claimed the stronger thing and would have let that assertion be marked covered
        /// by a test that never goes near an emitter.
        /// </remarks>
        [Test]
        [Ignore(Pending + " — the revoke endpoint does not exist yet")]
        public async Task DeleteConsent_WithTheBrowsersOwnToken_MakesTheStateEndpointReportNotSending()
        {
            var token = await ReadTokenAsync(await Client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" }));

            var revoke = await SendWithTokenAsync(HttpMethod.Delete, ConsentRoute, token);
            var afterwards = await ReadStateAsync(token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(revoke.IsSuccessStatusCode, Is.True);

                // Parsed, not a raw-substring negative: Does.Not.Contain("\"sending\":true") also
                // passes on indented JSON, on a renamed property, on an error status and on an
                // empty body - every way the behaviour can be absent.
                using var afterDocument = JsonDocument.Parse(afterwards);
                Assert.That(afterDocument.RootElement.TryGetProperty("sending", out var sending), Is.True,
                    "the state document must say whether the instance is sending before its value means anything");
                Assert.That(sending.GetBoolean(), Is.False,
                    "revocation takes effect immediately and server-side — not at the next restart, "
                    + "and not at the next daily emit");
            }
        }

        [Test]
        [Ignore(Pending + " — the revoke endpoint does not exist yet")]
        public async Task DeleteConsent_WithATokenThisInstanceNeverMinted_AnswersExactlyAsARealRevokeDoes()
        {
            var token = await ReadTokenAsync(await Client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" }));

            var real = await SendWithTokenAsync(HttpMethod.Delete, ConsentRoute, token);
            var unknown = await SendWithTokenAsync(HttpMethod.Delete, ConsentRoute, "never-minted-here");

            using (Assert.EnterMultipleScope())
            {
                // Same anchor as the state oracle: on an unrouted path both calls are 405 and
                // equal, so the comparison alone is green before anything is implemented.
                Assert.That(token, Is.Not.Empty, "the grant must have minted a token to revoke");
                Assert.That(real.IsSuccessStatusCode, Is.True,
                    "the real revoke must succeed before its status means anything as a baseline");

                Assert.That(unknown.StatusCode, Is.EqualTo(real.StatusCode),
                    "distinguishing the two turns revoke into a token-existence oracle");
            }
        }

        [Test]
        [Ignore(Pending + " — the endpoints do not exist yet")]
        public async Task TheConsentEndpoints_RequireNoAuthentication_BecauseMostInstancesHaveNone()
        {
            var state = await Client.GetAsync(StateRoute);
            var consent = await Client.PostAsJsonAsync(ConsentRoute, new { decision = "declined" });

            using (Assert.EnterMultipleScope())
            {
                // Asserting "not 401" is not enough: a 404 is not 401 either, so the weaker form
                // passed against a product with no endpoints at all.
                Assert.That(state.IsSuccessStatusCode, Is.True);
                Assert.That(consent.IsSuccessStatusCode, Is.True,
                    "with auth off every caller is the same subject, so account-scoped consent would "
                    + "collapse to one decision the first person makes for everybody. The browser is "
                    + "the only unit of consent that behaves the same in every deployment shape");
            }
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
