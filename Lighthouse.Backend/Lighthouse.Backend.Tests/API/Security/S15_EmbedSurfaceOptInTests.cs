using System.Net;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Security
{
    // Epic #5674 — the embed surface is opt-in. An instance that frames nothing must not carry the
    // three hops, because the handshake nonce is not yet bound to whoever asked for it: anyone who
    // gets an authenticated browser to load /embed/start?nonce=N can poll N anonymously and redeem
    // a session carrying that viewer's identity.
    public class S15_EmbedSurfaceOptInTests
    {
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
        public void S15_EmbedConfiguration_Unconfigured_IsOff()
        {
            var unconfigured = new EmbedConfiguration();

            Assert.That(unconfigured.Enabled, Is.False,
                "an instance that never heard of the Jira app must not expose the embed hops");
        }

        [Test]
        public async Task S15_Start_SurfaceNotEnabled_DoesNotExistEvenForASignedInViewer()
        {
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.EmbedDisabled,
                ViewerEmbedTestHost.ExplicitViewerSubject,
                ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.StartAsync(
                host.EmbedDisabled, ViewerEmbedTestHost.NewNonce(), sessionCookie: sessionCookie);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                "hop 1 is what records a grant against the viewer, so it is the hop that must not answer");
        }

        [Test]
        public async Task S15_Start_SurfaceNotEnabled_RecordsNothing()
        {
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.EmbedDisabled,
                ViewerEmbedTestHost.ExplicitViewerSubject,
                ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.StartAsync(
                host.EmbedDisabled, ViewerEmbedTestHost.NewNonce(), sessionCookie: sessionCookie);

            Assert.That(host.ReadEmbedSessionTokens(), Is.Empty,
                "a refused hop 1 must leave no grant behind for a poller to collect");
        }

        [Test]
        public async Task S15_Handshake_SurfaceNotEnabled_DoesNotExist()
        {
            var reading = await ViewerEmbedTestHost.PollHandshakeAsync(
                host.EmbedDisabled, ViewerEmbedTestHost.NewNonce());

            Assert.That(reading.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound),
                "hop 2 is the anonymous read, so it must not answer either");
        }

        [Test]
        public async Task S15_Enter_SurfaceNotEnabled_DoesNotExist()
        {
            using var response = await ViewerEmbedTestHost.EnterAsync(host.EmbedDisabled, "any-token");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task S15_Enter_TokenMintedBeforeTheSurfaceWasTurnedOff_IsNotRedeemable()
        {
            var token = await host.MintTokenAsync(host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            using var response = await ViewerEmbedTestHost.EnterAsync(host.EmbedDisabled, token);
            var embedCookie = ViewerEmbedTestHost.ReadSetCookie(response, ViewerEmbedTestHost.EmbedCookieName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(embedCookie, Is.Null,
                    "turning the surface off must end sessions in flight, not only stop new ones");
            }
        }

        [Test]
        public async Task S15_Start_SurfaceEnabled_StillGrants()
        {
            var (_, grant) = await host.GrantEmbedSessionAsync(
                host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject);

            Assert.That(grant.HasProperty("token"), Is.True,
                "the switch gates the surface; an instance that opts in keeps the whole flow");
        }
    }
}
