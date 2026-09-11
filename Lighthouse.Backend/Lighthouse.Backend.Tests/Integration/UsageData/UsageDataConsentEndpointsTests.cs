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

        private const string Pending = "pending: epic 5733 slice 01 (#5834)";

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
            var withoutToken = await ReadStateAsync(token: null);
            var withUnknownToken = await ReadStateAsync(token: "a-token-this-instance-never-minted");

            Assert.That(withUnknownToken, Is.EqualTo(withoutToken),
                "an endpoint that answers differently for a token it has never seen is an oracle: "
                + "it would let anyone test whether a given token exists on this instance");
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
                Assert.That(properties.Any(p => p.Contains("licen", StringComparison.OrdinalIgnoreCase)
                        || p.Contains("premium", StringComparison.OrdinalIgnoreCase)
                        || p.Contains("tier", StringComparison.OrdinalIgnoreCase)),
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

        [Test]
        [Ignore(Pending + " — the consent endpoint does not exist yet")]
        public async Task PostConsent_IsRateLimited()
        {
            HttpStatusCode? refused = null;

            for (var attempt = 0; attempt < 200 && refused is null; attempt++)
            {
                var response = await Client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" });
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    refused = response.StatusCode;
                }
            }

            Assert.That(refused, Is.EqualTo(HttpStatusCode.TooManyRequests),
                "the endpoint is unauthenticated and writes a durable row per call. Unlimited, a "
                + "single caller could plant a granted row and keep an instance emitting for a "
                + "whole liveness window");
        }

        [Test]
        [Ignore(Pending + " — the revoke endpoint does not exist yet")]
        public async Task DeleteConsent_WithTheBrowsersOwnToken_StopsTheInstanceSending()
        {
            var token = await ReadTokenAsync(await Client.PostAsJsonAsync(ConsentRoute, new { decision = "granted" }));

            var revoke = await SendWithTokenAsync(HttpMethod.Delete, ConsentRoute, token);
            var afterwards = await ReadStateAsync(token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(revoke.IsSuccessStatusCode, Is.True);
                Assert.That(afterwards, Does.Not.Contain("\"sending\":true"),
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

            Assert.That(unknown.StatusCode, Is.EqualTo(real.StatusCode),
                "distinguishing the two turns revoke into a token-existence oracle");
        }

        [Test]
        [Ignore(Pending + " — the endpoints do not exist yet")]
        public async Task TheConsentEndpoints_RequireNoAuthentication_BecauseMostInstancesHaveNone()
        {
            var state = await Client.GetAsync(StateRoute);
            var consent = await Client.PostAsJsonAsync(ConsentRoute, new { decision = "declined" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(state.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(consent.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized),
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
