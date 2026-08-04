using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Security
{
    // Epic 5146 slice 02a (#5641) — ADR-129 / ADR-131; feature security checklist S3, S4, S8, S10.
    public class S8_EmbedEntryPointTests
    {
        private const int ShortTokenLifetimeSeconds = 1;

        private static readonly string[] OffHostReturnPaths =
        [
            "https://evil.example/steal",
            "//evil.example/steal",
            "/\\evil.example/steal",
            "http://evil.example",
        ];

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
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_ValidToken_RedirectsToACleanUrlThatNoLongerCarriesTheToken()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var token = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);

            using var response = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, token);
            var location = response.Headers.Location?.ToString();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
                Assert.That(location, Is.Not.Null.And.Not.Empty);
                Assert.That(location, Does.Not.Contain(token),
                    "history and access logs must hold the token exactly once, on the request that spent it");
                Assert.That(location, Does.Not.Contain("token="));
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_Response_SuppressesTheReferrer()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var token = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);

            using var response = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, token);

            var referrerPolicy = response.Headers.TryGetValues("Referrer-Policy", out var values)
                ? string.Join(",", values)
                : null;

            Assert.That(referrerPolicy, Is.EqualTo("no-referrer"),
                "the token is in the URL, so the entry point must not let it travel onward in a Referer header");
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_TokenAlreadyRedeemed_RefusedLegibly()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var token = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);

            using var firstResponse = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, token);
            using var replayResponse = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, token);
            var replayBody = await replayResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                    "precondition: the first redemption succeeds");
                Assert.That(replayResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                    "the token is single use");
                Assert.That(replayBody, Is.Not.Empty,
                    "a refusal inside a frame must be readable, never an empty rectangle");
                Assert.That(replayResponse.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
                Assert.That(EmbedSessionTestHost.ReadSetCookie(replayResponse, EmbedSessionTestHost.EmbedCookieName), Is.Null,
                    "a refused redemption issues no session");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_UnknownToken_RefusedLegibly()
        {
            using var response = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, "unknown.token");
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(body, Is.Not.Empty);
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_MalformedToken_RefusedLegibly()
        {
            using var response = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, "no-separator-at-all");
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(body, Is.Not.Empty);
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_MissingToken_RefusedLegibly()
        {
            using var client = EmbedSessionTestHost.CreateClient(host.AuthEnabled);
            using var response = await client.GetAsync(EmbedSessionTestHost.EntryPath);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(body, Is.Not.Empty);
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_ExpiredToken_RefusedLegibly()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var shortLivedHost = host.WithTokenLifetime(ShortTokenLifetimeSeconds);
            var token = await EmbedSessionTestHost.MintTokenAsync(shortLivedHost, apiKey);

            await Task.Delay(TimeSpan.FromSeconds(ShortTokenLifetimeSeconds + 1));

            using var response = await EmbedSessionTestHost.EnterAsync(shortLivedHost, token);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(body, Is.Not.Empty);
                Assert.That(EmbedSessionTestHost.ReadSetCookie(response, EmbedSessionTestHost.EmbedCookieName), Is.Null);
            }
        }

        [Test]
        [TestCaseSource(nameof(OffHostReturnPaths))]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_ReturnPathPointsOffHost_NeverRedirectsThere(string offHostReturnPath)
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var token = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);

            using var response = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, token, offHostReturnPath);
            var location = response.Headers.Location?.ToString() ?? string.Empty;

            Assert.That(location, Does.Not.Contain("evil.example"),
                "the entry point redirects with a freshly authenticated cookie in hand, so an off-host return path is an open redirect");
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_ReturnPathIsALocalPath_RedirectsThere()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var token = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);

            using var response = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, token, "/portfolios/4201");
            var location = response.Headers.Location?.ToString();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
                Assert.That(location, Does.Contain("/portfolios/4201"),
                    "a local return path is how the Forge app deep-links a view");
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_AuthenticationDisabled_Absent_WhileEnabledRedirects()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var enabledToken = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);

            using var enabledResponse = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, enabledToken);
            using var disabledResponse = await EmbedSessionTestHost.EnterAsync(host.AuthDisabled, "any-token");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(enabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                    "differential control: the entry point exists and works when authentication is enabled");
                Assert.That(disabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        [Ignore("pending: epic 5146 slice 02a step 2 — the embed entry point does not exist yet")]
        public async Task S8_Enter_LicenceBlocked_Refused_WhileEnabledRedirects()
        {
            var apiKey = await host.CreateReadScopedKeyAsync(EmbedSessionTestHost.InScopePortfolioId);
            var enabledToken = await EmbedSessionTestHost.MintTokenAsync(host.AuthEnabled, apiKey);

            using var enabledResponse = await EmbedSessionTestHost.EnterAsync(host.AuthEnabled, enabledToken);
            using var blockedResponse = await EmbedSessionTestHost.EnterAsync(host.LicenceBlocked, "any-token");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(enabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                    "differential control: the entry point exists and works on a licensed instance");
                Assert.That(blockedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
                    "D44: BlockedModeFilter refuses first, and widening its allow-list to reach a narrower guard would only change the number");
            }
        }
    }
}
