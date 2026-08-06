using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Security
{
    /// <summary>
    /// Epic 5146 slice 01 (#5692) — ADR-132. The negatives on the sign-in hop and the handshake
    /// channel: F2 carried forward, D45's no-existence-oracle, D62's observable loss, D31/D44/DQ-7.
    /// </summary>
    public class S13_ViewerEmbedHandshakeTests
    {
        public const string NonceReplayEventName = "EmbedHandshakeNonceReplayed";

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
        public async Task AnAnonymousStart_ChallengesTheIdentityProviderAtTopLevel()
        {
            using var response = await ViewerEmbedTestHost.StartAsync(
                host.AuthEnabled, ViewerEmbedTestHost.NewNonce());

            var location = response.Headers.Location?.ToString() ?? string.Empty;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
                Assert.That(location, Does.StartWith(ViewerEmbedTestHost.StubbedAuthorizationEndpoint),
                    "the challenge must name the OIDC scheme explicitly; ForwardChallenge would send it to the "
                    + "SPA login page instead, which is the blank rectangle at top level");
            }
        }

        /// <summary>
        /// F2's carried negative. An embed cookie satisfies a bare [Authorize], and if that counted as
        /// signed in here a session would mint its own successor forever. It is challenged, not
        /// refused — an embed-cookie-only caller completes an ordinary login and ends up with a real one.
        /// </summary>
        [Test]
        public async Task AnEmbedCookie_CannotStartAHandshake()
        {
            var embedCookie = await EstablishEmbedCookieAsync(ViewerEmbedTestHost.ExplicitViewerSubject);
            var renewalNonce = ViewerEmbedTestHost.NewNonce();

            using var renewal = await ViewerEmbedTestHost.StartAsync(
                host.AuthEnabled, renewalNonce, embedCookie: embedCookie);

            var renewalHandshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, renewalNonce);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(renewal.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                    "an embed-cookie-only caller is challenged");
                Assert.That(renewal.Headers.Location?.ToString() ?? string.Empty,
                    Does.StartWith(ViewerEmbedTestHost.StubbedAuthorizationEndpoint));
                Assert.That(renewalHandshake.HasProperty("token"), Is.False,
                    "and no successor is granted, or the 30-minute bound is unbounded");
            }
        }

        /// <summary>
        /// F4 at the integration layer. Both cookies in one jar is reachable — opening an embed link at
        /// top level does exactly that — and there the person's own login must win.
        /// </summary>
        [Test]
        public async Task AnOrdinarySession_OutranksAnEmbedCookieOnTheSameRequest()
        {
            var embedCookie = await EstablishEmbedCookieAsync(ViewerEmbedTestHost.ExplicitViewerSubject);
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled,
                ViewerEmbedTestHost.GroupMappedViewerSubject,
                "Group Mapped Viewer",
                ViewerEmbedTestHost.ViewerGroupValue);

            var nonce = ViewerEmbedTestHost.NewNonce();
            using var start = await ViewerEmbedTestHost.StartAsync(
                host.AuthEnabled, nonce, sessionCookie: sessionCookie, embedCookie: embedCookie);

            var handshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, nonce);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)start.StatusCode, Is.EqualTo(200),
                    "the session cookie is present, so the caller is signed in and is not challenged");
                Assert.That(handshake.HasProperty("token"), Is.True,
                    "the grant belongs to the person whose ordinary session was presented");
            }
        }

        /// <summary>
        /// D45. Unknown, never-resolved, already-consumed and malformed are one response. Under D51 the
        /// first two are literally the same database state, so this asserts a structural property.
        /// </summary>
        [Test]
        public async Task UnknownPendingConsumedAndMalformedNonces_AreOneIdenticalResponse()
        {
            var consumedNonce = await GrantAndConsumeAsync(ViewerEmbedTestHost.ExplicitViewerSubject);

            var neverIssued = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, ViewerEmbedTestHost.NewNonce());
            var issuedButUnresolved = await PollAfterChallengedStartAsync();
            var alreadyConsumed = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, consumedNonce);
            var malformed = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, "not-a-nonce");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issuedButUnresolved, Is.EqualTo(neverIssued),
                    "a nonce whose sign-in has not finished must be indistinguishable from one that never existed");
                Assert.That(alreadyConsumed, Is.EqualTo(neverIssued),
                    "and so must a spent one, or the channel answers 'a session happened here'");
                Assert.That(malformed, Is.EqualTo(neverIssued),
                    "and so must a malformed one, or the shape of the nonce becomes an oracle");
                Assert.That(neverIssued.HasProperty("token"), Is.False);
                Assert.That(neverIssued.HasProperty("refusalCode"), Is.False);
            }
        }

        /// <summary>D62. Single use already refuses the second reader; it must also say so.</summary>
        [Test]
        public async Task ASecondReadOfAConsumedNonce_IsRecordedAsAnAnomaly()
        {
            var consumedNonce = await GrantAndConsumeAsync(ViewerEmbedTestHost.ExplicitViewerSubject);

            await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, consumedNonce);

            Assert.That(host.LogEvents.EventNames, Does.Contain(NonceReplayEventName),
                "an invisible impersonation is worth converting into a visible anomaly; this costs one log line");
        }

        [Test]
        public async Task AMissingNonce_IsRefusedBeforeAnyIdentityProviderIsInvolved()
        {
            using var response = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce: null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                "a sign-in hop with nowhere to put its answer must fail fast rather than bounce a viewer "
                + "through an identity provider for nothing");
        }

        [Test]
        public async Task StartAndHandshake_AreInvisibleWhenAuthenticationIsDisabled()
        {
            var nonce = ViewerEmbedTestHost.NewNonce();

            using var start = await ViewerEmbedTestHost.StartAsync(host.AuthDisabled, nonce);
            var handshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthDisabled, nonce);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(handshake.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task StartAndHandshake_AreBlockedWhenThePremiumLicenceIsNotValid()
        {
            var nonce = ViewerEmbedTestHost.NewNonce();

            using var start = await ViewerEmbedTestHost.StartAsync(host.LicenceBlocked, nonce);
            var handshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.LicenceBlocked, nonce);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(handshake.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
            }
        }

        /// <summary>
        /// DQ-7. Hop 1 is the only endpoint in this feature that challenges an identity provider, so a
        /// misconfigured registration is reachable here in a way it was not before. Sending a viewer at
        /// a provider the instance cannot talk to is a worse failure than saying nothing.
        /// </summary>
        [Test]
        public async Task Start_DoesNotChallengeAMisconfiguredIdentityProvider()
        {
            using var response = await ViewerEmbedTestHost.StartAsync(
                host.Misconfigured, ViewerEmbedTestHost.NewNonce());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                "the same answer Disabled gives (D31): the embed surface does not exist on an instance "
                + "whose authentication cannot work");
        }

        private async Task<string> EstablishEmbedCookieAsync(string subject)
        {
            var nonce = ViewerEmbedTestHost.NewNonce();
            var sessionCookie = host.ForgeInteractiveSessionCookie(host.AuthEnabled, subject, subject);

            using var start = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce, sessionCookie: sessionCookie);
            var handshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, nonce);

            Assert.That(handshake.HasProperty("token"), Is.True,
                $"precondition: the viewer must be granted an embed session; got {handshake.StatusCode} {handshake.Body}");

            using var entry = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, handshake.ReadString("token"));
            var embedCookie = ViewerEmbedTestHost.ReadCookieValue(entry, ViewerEmbedTestHost.EmbedCookieName);

            Assert.That(embedCookie, Is.Not.Null.And.Not.Empty, "precondition: the entry point must issue an embed cookie");
            return embedCookie!;
        }

        private async Task<string> GrantAndConsumeAsync(string subject)
        {
            var nonce = ViewerEmbedTestHost.NewNonce();
            var sessionCookie = host.ForgeInteractiveSessionCookie(host.AuthEnabled, subject, subject);

            using var start = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce, sessionCookie: sessionCookie);
            var first = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, nonce);

            Assert.That(first.HasProperty("token"), Is.True,
                $"precondition: the first poll must win the nonce; got {first.StatusCode} {first.Body}");

            return nonce;
        }

        private async Task<ViewerEmbedTestHost.HandshakeReading> PollAfterChallengedStartAsync()
        {
            var nonce = ViewerEmbedTestHost.NewNonce();
            using var challenged = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce);

            return await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, nonce);
        }
    }
}
