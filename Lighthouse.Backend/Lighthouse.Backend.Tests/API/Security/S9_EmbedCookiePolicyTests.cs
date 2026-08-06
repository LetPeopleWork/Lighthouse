using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.API.Security
{
    // Epic 5146 slice 02a (#5641) — ADR-130; feature security checklist S5, S6.
    public class S9_EmbedCookiePolicyTests
    {
        // Spelled out rather than taken from the production constants: this fixture is what pins the
        // shipped names, and reading them from the code under test would assert nothing.
        private const string EmbedCookieName = ".Lighthouse.Embed";
        private const string EmbedCookieScheme = "LighthouseEmbedCookie";
        private const string SessionCookieName = ".Lighthouse.Session";

        private static readonly TimeSpan ExpectedEmbedCookieLifetime = TimeSpan.FromMinutes(30);

        private ViewerEmbedTestHost host = null!;

        [SetUp]
        public void SetUp()
        {
            host = new ViewerEmbedTestHost();
            host.SeedRbacFixture();
        }

        [TearDown]
        public void TearDown()
        {
            host.Dispose();
        }

        [Test]
        public async Task S9_EmbedEntryPoint_SetCookieHeader_CarriesSecureSameSiteNoneAndPartitioned()
        {
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token);
            var setCookie = ViewerEmbedTestHost.ReadSetCookie(response, EmbedCookieName);

            // The framework does not validate CookieBuilder.Extensions, so the guard is the literal
            // header rather than the call site (spike/findings.md, 2026-08-04).
            using (Assert.EnterMultipleScope())
            {
                Assert.That(setCookie, Is.Not.Null,
                    "the entry point must issue the embed cookie by name");
                Assert.That(setCookie, Does.Contain("secure").IgnoreCase);
                Assert.That(setCookie, Does.Contain("samesite=none").IgnoreCase);
                Assert.That(setCookie, Does.Contain("Partitioned"),
                    "a cross-site frame without CHIPS partitioning is refused by browsers that require it");
                Assert.That(setCookie, Does.Contain("httponly").IgnoreCase);
            }
        }

        [Test]
        public async Task S9_EmbedEntryPoint_IssuesACookieThatDiesWithTheBrowserSession()
        {
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token);
            var setCookie = ViewerEmbedTestHost.ReadSetCookie(response, EmbedCookieName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(setCookie, Is.Not.Null);
                Assert.That(setCookie, Does.Not.Contain("expires=").IgnoreCase,
                    "D40: a persistent cookie survives the browser closing, which puts the revocation gap far past the 30 minutes S6 settled on");
                Assert.That(setCookie, Does.Not.Contain("max-age=").IgnoreCase);
            }
        }

        [Test]
        public async Task S9_EmbedEntryPoint_DoesNotTouchTheOrdinarySessionCookie()
        {
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token);

            Assert.That(ViewerEmbedTestHost.ReadSetCookie(response, SessionCookieName), Is.Null,
                "two cookie names is what lets an embed session and an ordinary session coexist in one browser");
        }

        [Test]
        public void S9_EmbedCookieScheme_ExpiresInThirtyMinutesAndNeverSlides()
        {
            using var scope = host.AuthEnabled.Services.CreateScope();
            var monitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
            var embedOptions = monitor.Get(EmbedCookieScheme);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(embedOptions.Cookie.Name, Is.EqualTo(EmbedCookieName));
                Assert.That(embedOptions.ExpireTimeSpan, Is.EqualTo(ExpectedEmbedCookieLifetime));
                Assert.That(embedOptions.SlidingExpiration, Is.False,
                    "a sliding embed cookie would make the revocation gap unbounded");
            }
        }

        [Test]
        public void S9_OrdinarySessionCookie_StillSameSiteLaxAndUnpartitioned()
        {
            using var scope = host.AuthEnabled.Services.CreateScope();
            var monitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
            var sessionOptions = monitor.Get(CookieAuthenticationDefaults.AuthenticationScheme);
            var built = sessionOptions.Cookie.Build(new DefaultHttpContext());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sessionOptions.Cookie.Name, Is.EqualTo(SessionCookieName));
                Assert.That(built.SameSite, Is.EqualTo(SameSiteMode.Lax),
                    "the embed relaxation must stay confined to the embed cookie — this is the blast-radius guarantee");
                Assert.That(built.Secure, Is.True);
                Assert.That(built.Extensions, Does.Not.Contain("Partitioned"));
            }
        }
    }
}
