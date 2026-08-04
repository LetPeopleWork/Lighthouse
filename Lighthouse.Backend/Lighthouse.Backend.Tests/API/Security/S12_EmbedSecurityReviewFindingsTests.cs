using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Security
{
    // Epic 5146 slice 02a (#5641) — the S1-S10 security review of 2026-08-04, findings F1 and F2.
    // Both shipped past 33 acceptance tests and a 90.91% mutation score, because each is invisible to
    // the shape of test the rest of the suite is written in.
    public class S12_EmbedSecurityReviewFindingsTests
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

        // F1. Every other rate-limit test configures the policy it then exercises, so all of them pass
        // whether or not the shipped configuration defines one. An undefined policy does not fail
        // loudly: Program.cs falls through to GetNoLimiter, and the endpoint runs unthrottled forever.
        // This reads the file that ships, which is the only thing that can catch that.
        [Test]
        public void EveryDeclaredRateLimitPolicy_IsDefinedInTheShippedConfiguration()
        {
            var appsettingsPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory, "appsettings.json");

            Assert.That(File.Exists(appsettingsPath), Is.True,
                $"appsettings.json is expected beside the test assembly, at {appsettingsPath}");

            using var document = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
            var policies = document.RootElement
                .GetProperty(RateLimitingConfiguration.SectionName)
                .GetProperty("Policies");

            string[] declaredPolicies =
            [
                RateLimitingConfiguration.AuthLoginPolicy,
                RateLimitingConfiguration.ApiKeysPolicy,
                RateLimitingConfiguration.BootstrapSystemAdminPolicy,
                RateLimitingConfiguration.EmbedSessionPolicy,
            ];

            using (Assert.EnterMultipleScope())
            {
                foreach (var policy in declaredPolicies)
                {
                    Assert.That(policies.TryGetProperty(policy, out _), Is.True,
                        $"policy '{policy}' is referenced by an endpoint but undefined in appsettings.json, "
                        + "so the limiter silently permits everything");
                }
            }
        }

        // F2. The embed cookie's principal carries api_key_id from the same shared claims factory, so a
        // claim-only check accepts it and the session mints its own successor - renewing indefinitely
        // past the 30 minutes ADR-130 set SlidingExpiration=false to bound. The credential has to be
        // presented on the request, not merely reflected in the principal.
        [Test]
        public async Task EmbedSession_CannotMintItsOwnSuccessor()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var embedCookie = await host.EstablishEmbedCookieAsync(apiKey);

            using var client = EmbedSessionTestHost.WithEmbedCookie(
                EmbedSessionTestHost.CreateClient(host.AuthEnabled), embedCookie);

            using var renewal = await client.PostAsync("/api/v1/embed/session-token", null);
            using var withKey = await EmbedSessionTestHost.ExchangeAsync(host.AuthEnabled, apiKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(renewal.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                    "an established embed session must not be able to extend itself");
                Assert.That(withKey.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "differential control: the same instance still mints for a directly presented key");
            }
        }

        // The same reasoning applies to revoke-all: it reads the api_key_id claim through the same
        // helper, so it would accept an embed cookie for exactly the same reason.
        [Test]
        public async Task EmbedSession_CannotRevokeOnBehalfOfTheKey()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var embedCookie = await host.EstablishEmbedCookieAsync(apiKey);

            using var client = EmbedSessionTestHost.WithEmbedCookie(
                EmbedSessionTestHost.CreateClient(host.AuthEnabled), embedCookie);

            using var response = await client.PostAsync("/api/v1/embed/session-token/revoke-all", null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        // A directly presented key still reaches both endpoints - the guard rejects the cookie route,
        // not the header route.
        [Test]
        public async Task DirectlyPresentedKey_StillReachesRevokeAll()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            using var client = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            using var response = await client.PostAsync("/api/v1/embed/session-token/revoke-all", null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }
    }
}
