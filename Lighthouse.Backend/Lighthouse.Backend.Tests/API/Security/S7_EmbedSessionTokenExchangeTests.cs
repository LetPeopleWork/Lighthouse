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

            using var document = JsonDocument.Parse(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(document.RootElement.GetProperty("reason").GetString(), Is.EqualTo("api_key_owner_unlinked"),
                    "D30: the Forge resolver branches on this code, so it is a wire contract and not prose");
                Assert.That(document.RootElement.GetProperty("message").GetString(), Does.Contain("no linked owner"),
                    "the presenter reads this out mid-demo, so it must name the cause");
                Assert.That(document.RootElement.GetProperty("message").GetString(), Does.Contain("Reassign the key"),
                    "and the fix, or the demo stalls on a refusal nobody can act on");
            }
        }

        [Test]
        public async Task S7_Exchange_SignedInUserWithoutAnApiKey_Refused_WhileTheKeyMints()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            using var keyedResponse = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey);
            using var sessionResponse = await EmbedSessionTestHost.PostAsSignedInUserAsync(host.AuthEnabled, EmbedSessionTestHost.ExchangePath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(keyedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "differential control: the same endpoint mints for a key-borne principal");
                Assert.That(sessionResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                    "S9: without api_key_id the minted session would carry the owner's full scope instead of the key's");
            }
        }

        [Test]
        public async Task S7_RevokeAll_SignedInUserWithoutAnApiKey_Refused()
        {
            using var response = await EmbedSessionTestHost.PostAsSignedInUserAsync(host.AuthEnabled, EmbedSessionTestHost.RevokeAllPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                "revocation is scoped to the calling key, so a caller with no key has nothing to revoke and must not fall through to key 0");
        }

        [Test]
        public async Task S7_RevokeAll_AuthenticationDisabled_Absent_WhileEnabledRevokes()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            using var enabledClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            enabledClient.WithApiKey(apiKey);
            using var enabledResponse = await enabledClient.PostAsync(EmbedSessionTestHost.RevokeAllPath, content: null);

            using var disabledClient = EmbedSessionTestHost.CreateClient(host.AuthDisabled);
            disabledClient.WithApiKey(apiKey);
            using var disabledResponse = await disabledClient.PostAsync(EmbedSessionTestHost.RevokeAllPath, content: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(enabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "differential control: the same request revokes when authentication is enabled");
                Assert.That(disabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                    "the whole embed surface is absent without authentication, revocation included");
            }
        }

        [Test]
        public async Task S7_Exchange_PrunesTokensThatAreAlreadySpent()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var spentToken = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);
            using (var enterResponse = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, spentToken))
            {
                Assert.That(enterResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect), "precondition: the first token is spent");
            }

            var freshToken = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);
            var storedTokenIds = host.ReadEmbedSessionTokens().Select(token => token.TokenId).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(storedTokenIds, Has.Count.EqualTo(1),
                    "minting prunes what is already spent — without it the table only ever grows, one row per page open");
                Assert.That(storedTokenIds, Does.Contain(TokenIdOf(freshToken)));
                Assert.That(storedTokenIds, Does.Not.Contain(TokenIdOf(spentToken)));
            }
        }

        [Test]
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

        private static string TokenIdOf(string token)
        {
            return token.Split('.')[0];
        }
    }
}
