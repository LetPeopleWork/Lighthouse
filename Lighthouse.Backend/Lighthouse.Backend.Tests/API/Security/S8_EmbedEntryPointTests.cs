using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Security
{
    // Epic 5146 slice 02a (#5641) — ADR-131 / ADR-137; feature security checklist S3, S4, S8, S10.
    public class S8_EmbedEntryPointTests
    {
        private const int ShortTokenLifetimeSeconds = 1;
        private const string ForgedSecret = "forged-secret";

        private static readonly string[] OffHostReturnPaths =
        [
            "https://evil.example/steal",
            "//evil.example/steal",
            "/\\evil.example/steal",
            "http://evil.example",
        ];

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
        public async Task S8_Enter_ValidToken_RedirectsToACleanUrlThatNoLongerCarriesTheToken()
        {
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token);
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
        public async Task S8_Enter_Response_SuppressesTheReferrer()
        {
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token);

            var referrerPolicy = response.Headers.TryGetValues("Referrer-Policy", out var values)
                ? string.Join(",", values)
                : null;

            Assert.That(referrerPolicy, Is.EqualTo("no-referrer"),
                "the token is in the URL, so the entry point must not let it travel onward in a Referer header");
        }

        [Test]
        public async Task S8_Enter_TokenAlreadyRedeemed_RefusedLegibly()
        {
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var firstResponse = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token);
            using var replayResponse = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token);
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
                Assert.That(ViewerEmbedTestHost.ReadSetCookie(replayResponse, ViewerEmbedTestHost.EmbedCookieName), Is.Null,
                    "a refused redemption issues no session");
            }
        }

        [Test]
        public async Task S8_Enter_UnknownToken_RefusedLegibly()
        {
            using var response = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, "unknown.token");
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(body, Is.Not.Empty);
                Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            }
        }

        [Test]
        public async Task S8_Enter_MalformedToken_RefusedLegibly()
        {
            using var response = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, "no-separator-at-all");
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(body, Is.Not.Empty);
            }
        }

        [Test]
        public async Task S8_Enter_MissingToken_RefusedLegibly()
        {
            using var client = ViewerEmbedTestHost.CreateClient(host.AuthEnabled);
            using var response = await client.GetAsync(ViewerEmbedTestHost.EntryPath);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(body, Is.Not.Empty);
            }
        }

        [Test]
        public async Task S8_Enter_ExpiredToken_RefusedLegibly()
        {
            var shortLivedHost = host.WithTokenLifetime(ShortTokenLifetimeSeconds);
            var token = await host.MintTokenAsync(shortLivedHost, ViewerEmbedTestHost.ExplicitViewerSubject);

            await Task.Delay(TimeSpan.FromSeconds(ShortTokenLifetimeSeconds + 1));

            using var response = await ViewerEmbedTestHost.EnterAsync(shortLivedHost, token);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(body, Is.Not.Empty);
                Assert.That(ViewerEmbedTestHost.ReadSetCookie(response, ViewerEmbedTestHost.EmbedCookieName), Is.Null);
            }
        }

        [Test]
        public async Task S8_Enter_GenuineTokenIdWithAWrongSecret_Refused_AndLeavesTheRealTokenSpendable()
        {
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);
            var forgedToken = $"{token.Split('.')[0]}.{ForgedSecret}";

            using var forgedResponse = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, forgedToken);
            using var genuineResponse = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forgedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                    "the token id is only an identifier — all the entropy is in the secret, so half a token must never redeem");
                Assert.That(ViewerEmbedTestHost.ReadSetCookie(forgedResponse, ViewerEmbedTestHost.EmbedCookieName), Is.Null);
                Assert.That(genuineResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                    "and the rejected attempt must not have spent the real token, or a guessed id becomes a denial of service");
            }
        }

        [Test]
        [TestCase(0)]
        [TestCase(-30)]
        public async Task S8_Enter_TokenLifetimeConfiguredNonPositive_FallsBackToTheDefaultWindow(int configuredLifetimeSeconds)
        {
            var misconfiguredHost = host.WithTokenLifetime(configuredLifetimeSeconds);
            var token = await host.MintTokenAsync(misconfiguredHost, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.EnterAsync(misconfiguredHost, token);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                "a non-positive lifetime is a misconfiguration, not an instruction to mint tokens that are dead on arrival");
        }

        [Test]
        [TestCaseSource(nameof(OffHostReturnPaths))]
        public async Task S8_Enter_ReturnPathPointsOffHost_NeverRedirectsThere(string offHostReturnPath)
        {
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token, offHostReturnPath);
            var location = response.Headers.Location?.ToString() ?? string.Empty;

            Assert.That(location, Does.Not.Contain("evil.example"),
                "the entry point redirects with a freshly authenticated cookie in hand, so an off-host return path is an open redirect");
        }

        [Test]
        public async Task S8_Enter_ReturnPathIsALocalPath_RedirectsThere()
        {
            var deepLink = $"/teams/{ViewerEmbedTestHost.ExplicitTeamId}";
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, token, deepLink);
            var location = response.Headers.Location?.ToString();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
                Assert.That(location, Does.Contain(deepLink),
                    "a local return path is how the Forge app deep-links a view");
            }
        }

        [Test]
        public async Task S8_Enter_AuthenticationDisabled_Absent_WhileEnabledRedirects()
        {
            var enabledToken = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var enabledResponse = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, enabledToken);
            using var disabledResponse = await ViewerEmbedTestHost.EnterAsync(host.AuthDisabled, "any-token");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(enabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                    "differential control: the entry point exists and works when authentication is enabled");
                Assert.That(disabledResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task S8_Enter_LicenceBlocked_Refused_WhileEnabledRedirects()
        {
            var enabledToken = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var enabledResponse = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, enabledToken);
            using var blockedResponse = await ViewerEmbedTestHost.EnterAsync(host.LicenceBlocked, "any-token");

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
