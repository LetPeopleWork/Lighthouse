using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Security
{
    // Epic 5146 slice 02a (#5641) — ADR-129 claims parity; feature security checklist S9.
    // A dropped api_key_id fails OPEN: the session silently widens to the key owner's full scope.
    // Every test here therefore asserts BOTH what the embed session reaches and what it must not.
    public class S10_EmbedScopeEquivalenceTests
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
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed session does not exist yet")]
        public async Task S10_ReadScopedKey_EmbedSession_ReachesTheSameInScopeResourceAsTheHeader()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var embedCookie = await host.EstablishEmbedCookieAsync(apiKey);

            using var headerClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            headerClient.WithApiKey(apiKey);
            using var headerResponse = await headerClient.GetAsync(InScopePortfolioPath);

            using var embedClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            EmbedSessionTestHost.WithEmbedCookie(embedClient, embedCookie);
            using var embedResponse = await embedClient.GetAsync(InScopePortfolioPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(headerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "control: the key reaches its own scope through the header");
                Assert.That(embedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "drop the subject claim and every scoped check fails closed — the frame would render an empty Lighthouse");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed session does not exist yet")]
        public async Task S10_ReadScopedKey_EmbedSession_IsRefusedTheSameOutOfScopeResourceAsTheHeader()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var embedCookie = await host.EstablishEmbedCookieAsync(apiKey);

            using var headerClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            headerClient.WithApiKey(apiKey);
            using var headerResponse = await headerClient.GetAsync(OutOfScopePortfolioPath);

            using var embedClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            EmbedSessionTestHost.WithEmbedCookie(embedClient, embedCookie);
            using var embedResponse = await embedClient.GetAsync(OutOfScopePortfolioPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(headerResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                    "control: the header-borne principal is refused outside the key's scope");
                Assert.That(embedResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                    "an embed principal missing api_key_id resolves the OWNER's permissions instead of the key's — "
                    + "a privilege escalation that looks like a working session");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed session does not exist yet")]
        public async Task S10_ReadScopedKey_EmbedSession_IsRefusedWritesJustLikeTheHeader()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var embedCookie = await host.EstablishEmbedCookieAsync(apiKey);

            using var headerClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            headerClient.WithApiKey(apiKey);
            using var headerResponse = await headerClient.DeleteAsync(InScopePortfolioPath);

            using var embedClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            EmbedSessionTestHost.WithEmbedCookie(embedClient, embedCookie);
            using var embedResponse = await embedClient.DeleteAsync(InScopePortfolioPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(headerResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
                    "control: a read-scoped key cannot write through the header");
                Assert.That(embedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
                    "a read-scoped key must produce a read-only embed");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed session does not exist yet")]
        public async Task S10_ReadScopedKey_EmbedSession_ReportsTheSameEffectivePermissionsAsTheHeader()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var embedCookie = await host.EstablishEmbedCookieAsync(apiKey);

            using var headerClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            headerClient.WithApiKey(apiKey);
            using var headerResponse = await headerClient.GetAsync(EmbedSessionTestHost.MySummaryPath);
            var headerSummary = await headerResponse.Content.ReadAsStringAsync();

            using var embedClient = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            EmbedSessionTestHost.WithEmbedCookie(embedClient, embedCookie);
            using var embedResponse = await embedClient.GetAsync(EmbedSessionTestHost.MySummaryPath);
            var embedSummary = await embedResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(headerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(embedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(embedSummary, Is.EqualTo(headerSummary),
                    "the SPA gates every admin surface off this summary, so a widened embed summary unhides admin UI");
            }
        }

        private static string InScopePortfolioPath =>
            $"/api/v1/portfolios/{EmbedSessionTestHost.InScopePortfolioId}";

        private static string OutOfScopePortfolioPath =>
            $"/api/v1/portfolios/{EmbedSessionTestHost.OutOfScopePortfolioId}";
    }
}
