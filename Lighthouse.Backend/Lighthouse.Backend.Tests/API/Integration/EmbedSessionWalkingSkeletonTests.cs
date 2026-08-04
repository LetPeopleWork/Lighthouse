using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration
{
    // Epic 5146 slice 02a (#5641) — ADR-129.
    public class EmbedSessionWalkingSkeletonTests
    {
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
        public async Task EmbedSession_ApiKeyExchangedForToken_EntryPointSignsIn_FramedAppSeesAuthenticatedSession()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            using var exchangeResponse = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey);
            var exchangeBody = await exchangeResponse.Content.ReadAsStringAsync();
            using var exchangeDocument = JsonDocument.Parse(exchangeBody);
            var token = exchangeDocument.RootElement.GetProperty("token").GetString();

            using var entryResponse = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, token!);
            var embedCookie = EmbedSessionTestHost.ReadCookieValue(entryResponse, EmbedSessionTestHost.EmbedCookieName);

            using var framedClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            EmbedSessionTestHost.WithEmbedCookie(framedClient, embedCookie!);
            using var sessionResponse = await framedClient.GetAsync(EmbedSessionTestHost.SessionStatusPath);
            var sessionBody = await sessionResponse.Content.ReadAsStringAsync();
            using var sessionDocument = JsonDocument.Parse(sessionBody);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exchangeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "the Forge resolver presents its API key and receives a token");
                Assert.That(token, Is.Not.Null.And.Not.Empty);
                Assert.That(exchangeDocument.RootElement.TryGetProperty("expiresAt", out _), Is.True,
                    "the caller must be told when the token dies without having to guess the lifetime");
                Assert.That(exchangeDocument.RootElement.TryGetProperty("embedUrl", out _), Is.True,
                    "the caller frames embedUrl rather than composing the entry-point URL itself");

                Assert.That(entryResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                    "a valid token redirects into the SPA rather than rendering the token URL");
                Assert.That(embedCookie, Is.Not.Null.And.Not.Empty,
                    "the redirect carries the embed session cookie");

                Assert.That(sessionResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(sessionDocument.RootElement.GetProperty("isAuthenticated").GetBoolean(), Is.True,
                    "the framed SPA sees an authenticated session with no interactive login");
            }
        }
    }
}
