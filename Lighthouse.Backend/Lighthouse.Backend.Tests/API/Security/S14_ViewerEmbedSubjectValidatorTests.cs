using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Security
{
    /// <summary>
    /// Epic 5146 slice 01 (#5692) — ADR-132 D57/D58. The embed cookie validator re-resolves a subject
    /// on every request, and it must never create one.
    /// </summary>
    public class S14_ViewerEmbedSubjectValidatorTests
    {
        private const string FirstTimeViewerSubject = "viewer-embed-first-timer";

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

        /// <summary>
        /// F3's carried equivalent. The old lever stopped unredeemed tokens; this one ends established
        /// sessions. There is no deactivation in Lighthouse (D58), so the control is deletion.
        /// </summary>
        [Test]
        public async Task DeletingTheViewer_EndsTheirLiveFrame()
        {
            var embedCookie = await EstablishEmbedCookieAsync(ViewerEmbedTestHost.ExplicitViewerSubject);

            using var beforeDeletion = await ViewerEmbedTestHost.GetAsViewerAsync(
                host.AuthEnabled, ViewerEmbedTestHost.TeamsPath, embedCookie: embedCookie);

            await host.DeleteViewerAsync(ViewerEmbedTestHost.ExplicitViewerSubject);

            using var afterDeletion = await ViewerEmbedTestHost.GetAsViewerAsync(
                host.AuthEnabled, ViewerEmbedTestHost.TeamsPath, embedCookie: embedCookie);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(beforeDeletion.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "differential control: the frame works while the viewer exists");
                Assert.That(afterDeletion.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                    "deleting the viewer must end the session that names them, within one request rather "
                    + "than within the cookie's lifetime");
            }
        }

        /// <summary>
        /// The trap D57 exists to close. GetOrCreateFromPrincipalAsync creates; calling it from the
        /// validator re-creates the profile an administrator just deleted, on that person's very next
        /// request, and every other assertion in this file still passes.
        /// </summary>
        [Test]
        public async Task RejectingADeletedViewer_DoesNotBringTheirProfileBack()
        {
            var embedCookie = await EstablishEmbedCookieAsync(ViewerEmbedTestHost.ExplicitViewerSubject);
            await host.DeleteViewerAsync(ViewerEmbedTestHost.ExplicitViewerSubject);

            using var firstAfterDeletion = await ViewerEmbedTestHost.GetAsViewerAsync(
                host.AuthEnabled, ViewerEmbedTestHost.TeamsPath, embedCookie: embedCookie);

            using var secondAfterDeletion = await ViewerEmbedTestHost.GetAsViewerAsync(
                host.AuthEnabled, ViewerEmbedTestHost.TeamsPath, embedCookie: embedCookie);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstAfterDeletion.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                    "the rejected request itself");
                Assert.That(secondAfterDeletion.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
                    "and the one after it: a lookup that creates would have resurrected the profile on "
                    + "the first rejection, so the second request would authenticate and answer 200 or "
                    + "403 instead — the whole control becoming a no-op that looks like it works");
            }
        }

        /// <summary>
        /// A declared effect, not an incident: resolving a viewer creates their profile, so every
        /// curious Jira user appears in the customer's user list. Named here so it is a decision rather
        /// than a discovery.
        /// </summary>
        [Test]
        public async Task AFirstTimeViewersClick_CreatesTheirProfileAndRefusesThem()
        {
            var nonce = ViewerEmbedTestHost.NewNonce();
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled, FirstTimeViewerSubject, "Curious Jira User");

            Assert.That(await ListedInTheUserListAsync(), Is.False, "precondition: nobody by this name yet");

            using var start = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce, sessionCookie: sessionCookie);
            var handshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, nonce);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await ListedInTheUserListAsync(), Is.True,
                    "hop 1 must resolve a profile before it can ask RBAC anything, and resolution creates — "
                    + "observable to an administrator as a new row in the user list, which is the declared "
                    + "effect rather than an incident");
                Assert.That(handshake.HasProperty("refusalCode"), Is.True,
                    "the new row grants nothing, so the viewer is refused rather than framed empty");
            }
        }

        /// <summary>
        /// D57's effect is observable to an administrator, so it is asserted there rather than against
        /// the table — an internal row count would pass a rename of the column it reads.
        /// </summary>
        private async Task<bool> ListedInTheUserListAsync()
        {
            var adminCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled, ViewerEmbedTestHost.SystemAdminSubject, "System Admin");

            using var response = await ViewerEmbedTestHost.GetAsViewerAsync(
                host.AuthEnabled, "/api/v1/authorization/users", sessionCookie: adminCookie);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "precondition: the administrator must be able to read the user list");

            var body = await response.Content.ReadAsStringAsync();
            return body.Contains(FirstTimeViewerSubject, StringComparison.Ordinal);
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
    }
}
