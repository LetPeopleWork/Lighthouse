using System.Globalization;
using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Security
{
    // Epic 5146 slice 02a (#5641) — ADR-129; feature security checklist S2, S7, S10.
    public class S7_EmbedSessionTokenExchangeTests
    {
        private const int RateLimitPermitLimit = 3;
        private const int RateLimitWindowSeconds = 2;

        private EmbedSessionTestHost host = null!;

        [SetUp]
        public void SetUp()
        {
            host = new EmbedSessionTestHost();
            host.SeedSystemAdminAndPortfolios();
        }

        [TearDown]
        public void TearDown()
        {
            host.Dispose();
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the exchange endpoint does not exist yet")]
        public async Task S7_Exchange_ValidApiKey_MintsTokenWithExpiryAndEmbedUrl()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            using var response = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using var document = JsonDocument.Parse(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(document.RootElement.GetProperty("token").GetString(), Is.Not.Null.And.Not.Empty);
                Assert.That(document.RootElement.GetProperty("expiresAt").GetString(), Is.Not.Null.And.Not.Empty);
                Assert.That(document.RootElement.GetProperty("embedUrl").GetString(), Does.Contain(EmbedSessionTestHost.EntryPath));
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the exchange endpoint does not exist yet")]
        public async Task S7_Exchange_WithoutApiKey_Refused()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            using var keyedResponse = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey);
            using var anonymousResponse = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(keyedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "differential control: without it a 401 proves only that the endpoint is absent");
                Assert.That(anonymousResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                    "an anonymous caller must not be able to mint a session-granting token");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the exchange endpoint does not exist yet")]
        public async Task S7_Exchange_UnknownApiKey_Refused()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            using var knownKeyResponse = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey);
            using var unknownKeyResponse = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, "not-a-real-key");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(knownKeyResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "differential control: without it a 401 proves only that the endpoint is absent");
                Assert.That(unknownKeyResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the exchange endpoint does not exist yet")]
        public async Task S7_Exchange_ApiKeyOwnerUnlinked_RefusedWithStructuredReason()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            host.UnlinkEveryApiKeyOwner();

            using var response = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.OK),
                    "an unlinked key carries no stable subject, so every scoped check would fail and the frame would render empty");
                Assert.That((int)response.StatusCode, Is.GreaterThanOrEqualTo(400).And.LessThan(500),
                    "the refusal is the caller's fault, not the server's");
                Assert.That(body, Is.Not.Empty,
                    "the refusal names why the key cannot honour the contract");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the exchange endpoint does not exist yet")]
        public async Task S7_Exchange_AuthenticationDisabled_Absent_WhileEnabledMints()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            using var enabledResponse = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey);
            using var disabledResponse = await EmbedSessionTestHost.ExchangeAsync(host.AuthDisabled, apiKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(enabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "differential control: the same request mints a token when authentication is enabled");
                Assert.That(disabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                    "with authentication disabled there is no session to sign into, so the surface must not exist");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the exchange endpoint does not exist yet")]
        public async Task S7_Exchange_LicenceBlocked_Refused_WhileEnabledMints()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            using var enabledResponse = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey);
            using var blockedResponse = await EmbedSessionTestHost.ExchangeAsync(host.LicenceBlocked, apiKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(enabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "differential control: the same request mints a token on a licensed instance");
                Assert.That(blockedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
                    "D44: BlockedModeFilter refuses first; a session minted in blocked mode would meet a 403 on every data endpoint anyway");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the exchange endpoint does not exist yet")]
        public async Task S7_Exchange_ExceedsRateLimit_Throttled_WithRetryAfter()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var limitedHost = host.WithEmbedRateLimit(RateLimitPermitLimit, RateLimitWindowSeconds);

            var statusesWithinLimit = new List<HttpStatusCode>();
            for (var attempt = 0; attempt < RateLimitPermitLimit; attempt++)
            {
                using var response = await EmbedSessionTestHost.ExchangeAsync(limitedHost, apiKey);
                statusesWithinLimit.Add(response.StatusCode);
            }

            using var throttledResponse = await EmbedSessionTestHost.ExchangeAsync(limitedHost, apiKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(statusesWithinLimit, Has.All.Not.EqualTo(HttpStatusCode.TooManyRequests));
                Assert.That(throttledResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

                var retryAfter = throttledResponse.Headers.TryGetValues("Retry-After", out var values)
                    ? string.Join(",", values)
                    : null;
                Assert.That(retryAfter, Is.Not.Null.And.Not.Empty);
                Assert.That(int.TryParse(retryAfter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds), Is.True);
                Assert.That(seconds, Is.GreaterThan(0));
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the exchange endpoint does not exist yet")]
        public async Task S7_RevokeAll_OutstandingTokenOfCallingKey_NoLongerRedeemable()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var token = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);

            using var revokeClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            revokeClient.WithApiKey(apiKey);
            using var revokeResponse = await revokeClient.PostAsync(EmbedSessionTestHost.RevokeAllPath, content: null);

            using var enterResponse = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)revokeResponse.StatusCode, Is.LessThan(300));
                Assert.That(enterResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                    "a revoked token must not still establish a session");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the exchange endpoint does not exist yet")]
        public async Task S7_RevokeAll_DoesNotRevokeTokensOfAnotherKey()
        {
            var revokedKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var survivingKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, revokedKey);
            var survivingToken = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, survivingKey);

            using var revokeClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            revokeClient.WithApiKey(revokedKey);
            using var revokeResponse = await revokeClient.PostAsync(EmbedSessionTestHost.RevokeAllPath, content: null);

            using var enterResponse = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, survivingToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)revokeResponse.StatusCode, Is.LessThan(300));
                Assert.That(enterResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                    "revoke-all is scoped to the calling key; another key's outstanding token is untouched");
            }
        }
    }
}
