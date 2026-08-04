using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.API.Security
{
    // Epic 5146 slice 02a (#5641) — ADR-130, review-gate pre-requisite.
    // CookieAuthenticationOptions is resolved once and shared. Per-request mutation of it would be a
    // data race that passes every single-threaded test, so both cookies are observed together while
    // embed sign-ins are genuinely in flight.
    public class S9b_EmbedCookieConcurrencyTests
    {
        private const int ConcurrentSessions = 8;

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
        public async Task ConcurrentEmbedSignIns_RelaxedAttributesNeverLeakOntoTheOrdinaryCookie()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);

            var tokens = new List<string>();
            for (var index = 0; index < ConcurrentSessions; index++)
            {
                tokens.Add(await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey));
            }

            using var barrier = new Barrier(ConcurrentSessions * 2);

            // Both task sets must be materialised before either is awaited, or the barrier waits on
            // participants a lazy sequence has not started yet.
            var embedHeaders = tokens.Select(token => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                using var response = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, token);
                return EmbedSessionTestHost.ReadSetCookie(response, EmbedSessionTestHost.EmbedCookieName);
            })).ToList();

            var ordinaryCookies = Enumerable.Range(0, ConcurrentSessions).Select(_ => Task.Run(() =>
            {
                barrier.SignalAndWait();
                return BuildOrdinarySessionCookie();
            })).ToList();

            var embedResults = await Task.WhenAll(embedHeaders);
            var ordinaryResults = await Task.WhenAll(ordinaryCookies);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(embedResults, Has.All.Not.Null);
                Assert.That(embedResults, Has.All.Contains("samesite=none"));
                Assert.That(embedResults, Has.All.Contains("Partitioned"));

                Assert.That(ordinaryResults.Select(cookie => cookie.SameSite), Has.All.EqualTo(SameSiteMode.Lax),
                    "a relaxed embed sign-in in flight must not widen the cookie every ordinary session carries");
                Assert.That(ordinaryResults.SelectMany(cookie => cookie.Extensions), Does.Not.Contain("Partitioned"));
                Assert.That(ordinaryResults.Select(cookie => cookie.Secure), Has.All.True);
            }
        }

        private CookieOptions BuildOrdinarySessionCookie()
        {
            using var scope = host.AuthEnabled.Services.CreateScope();
            var monitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();

            return monitor
                .Get(CookieAuthenticationDefaults.AuthenticationScheme)
                .Cookie
                .Build(new DefaultHttpContext());
        }
    }
}
