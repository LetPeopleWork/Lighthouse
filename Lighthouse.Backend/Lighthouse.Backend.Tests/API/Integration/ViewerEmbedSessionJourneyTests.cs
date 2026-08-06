using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// Epic 5146 slice 01 (#5692) — ADR-137. The three hops read in order: a viewer signs in at top
    /// level, the resolver polls the outcome back, the frame is entered as that person.
    /// </summary>
    public class ViewerEmbedSessionJourneyTests
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

        /// <summary>
        /// Control. Everything below reads "signed in" off a forged session cookie; if the forgery is
        /// not a real Lighthouse session, every failure underneath it is a setup failure wearing the
        /// costume of a product failure. This test is green before the feature exists and must stay so.
        /// </summary>
        [Test]
        public async Task Control_AForgedInteractiveCookie_IsARealLighthouseSession()
        {
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject, "Explicitly Permissioned Viewer");

            using var response = await ViewerEmbedTestHost.GetAsViewerAsync(
                host.AuthEnabled, ViewerEmbedTestHost.SessionStatusPath, sessionCookie: sessionCookie);

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(document.RootElement.GetProperty("isAuthenticated").GetBoolean(), Is.True,
                    "the ordinary cookie handler must accept this cookie, or D56's positive branch is untestable");
            }
        }

        [Test]
        public async Task AnInteractiveSignIn_HandsTheFrameTheViewersOwnPermissions()
        {
            var nonce = ViewerEmbedTestHost.NewNonce();
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled, ViewerEmbedTestHost.ExplicitViewerSubject, "Explicitly Permissioned Viewer");

            using var start = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce, sessionCookie: sessionCookie);
            var handshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, nonce);

            Assert.That(handshake.HasProperty("token"), Is.True,
                $"a signed-in viewer with readable scope must be granted; handshake answered {handshake.StatusCode} {handshake.Body}");

            using var entry = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, handshake.ReadString("token"));
            var embedCookie = ViewerEmbedTestHost.ReadCookieValue(entry, ViewerEmbedTestHost.EmbedCookieName);

            Assert.That(embedCookie, Is.Not.Null.And.Not.Empty, "the redirect must carry the embed session cookie");

            var framedTeams = await ReadTeamIdsAsync(host.AuthEnabled, embedCookie: embedCookie);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)start.StatusCode, Is.EqualTo(200),
                    "hop 1 ends on a terminal page in the orphaned tab, not on a redirect or an error (D61)");
                Assert.That(entry.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                    "a redeemed token redirects into the SPA rather than rendering the token URL");
                Assert.That(framedTeams, Does.Contain(ViewerEmbedTestHost.ExplicitTeamId),
                    "the frame carries what this viewer may read");
                Assert.That(framedTeams, Does.Not.Contain(ViewerEmbedTestHost.GroupMappedTeamId),
                    "and nothing else — a frame that shows more than the viewer's own scope is the failure D49 exists to prevent");
            }
        }

        /// <summary>
        /// D59. A viewer with an explicit UserPermission passes with or without the RBAC conjunct, so
        /// only a group-mapped one can fail. Without D59 the frame is silently empty while the same
        /// person works perfectly in an ordinary tab.
        /// </summary>
        [Test]
        public async Task AGroupMappedViewer_SeesTheSameTeamInsideTheFrameAsOutsideIt()
        {
            var nonce = ViewerEmbedTestHost.NewNonce();
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled,
                ViewerEmbedTestHost.GroupMappedViewerSubject,
                "Group Mapped Viewer",
                ViewerEmbedTestHost.ViewerGroupValue);

            var teamsInOrdinaryTab = await ReadTeamIdsAsync(host.AuthEnabled, sessionCookie: sessionCookie);

            using var start = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce, sessionCookie: sessionCookie);
            var handshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, nonce);

            Assert.That(handshake.HasProperty("token"), Is.True,
                $"the group-mapped viewer holds a readable scope and must be granted; handshake answered {handshake.StatusCode} {handshake.Body}");

            using var entry = await ViewerEmbedTestHost.EnterAsync(host.AuthEnabled, handshake.ReadString("token"));
            var embedCookie = ViewerEmbedTestHost.ReadCookieValue(entry, ViewerEmbedTestHost.EmbedCookieName);
            var teamsInsideTheFrame = await ReadTeamIdsAsync(host.AuthEnabled, embedCookie: embedCookie);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teamsInOrdinaryTab, Does.Contain(ViewerEmbedTestHost.GroupMappedTeamId),
                    "differential control: the group mapping resolves for this viewer in an ordinary tab");
                Assert.That(teamsInsideTheFrame, Does.Contain(ViewerEmbedTestHost.GroupMappedTeamId),
                    "the framed session is rebuilt from a stored subject and carries no live group claims, so it "
                    + "resolves nothing unless the snapshot fallback is gated on auth_method rather than api_key_id");
            }
        }

        /// <summary>
        /// Control for the pair below. Without it, "inherits nothing" could pass because the fixture
        /// grants nothing to anybody, and the fail-open guard would be vacuous.
        /// </summary>
        [Test]
        public async Task Control_AGroupMappedViewerInAnOrdinaryTab_AlreadySeesTheirTeam()
        {
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled,
                ViewerEmbedTestHost.GroupMappedViewerSubject,
                "Group Mapped Viewer",
                ViewerEmbedTestHost.ViewerGroupValue);

            var teams = await ReadTeamIdsAsync(host.AuthEnabled, sessionCookie: sessionCookie);

            Assert.That(teams, Does.Contain(ViewerEmbedTestHost.GroupMappedTeamId),
                "a live group claim resolves the mapping; this is the reading the framed session must match");
        }

        /// <summary>
        /// The guard against D59 becoming a fail-open widening: an ordinary cookie principal has no
        /// auth_method claim, so a live token that genuinely returned zero groups must keep resolving
        /// zero — even when a stale snapshot sits on the profile.
        /// </summary>
        [Test]
        public async Task AnOrdinarySession_WithNoLiveGroups_StillInheritsNothingFromTheStoredSnapshot()
        {
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled, ViewerEmbedTestHost.GroupMappedViewerSubject, "Group Mapped Viewer");

            var teams = await ReadTeamIdsAsync(host.AuthEnabled, sessionCookie: sessionCookie);

            Assert.That(teams, Does.Not.Contain(ViewerEmbedTestHost.GroupMappedTeamId),
                "the profile carries a snapshot, but this principal presented a live token with no groups");
        }

        /// <summary>D49: authenticated and provisioned with nothing gets a refusal, not an empty Lighthouse.</summary>
        [Test]
        public async Task AViewerWithNoReadableScope_IsRefusedByLighthouseRatherThanFramedEmpty()
        {
            var nonce = ViewerEmbedTestHost.NewNonce();
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled, ViewerEmbedTestHost.UnprovisionedViewerSubject, "Unprovisioned Viewer");

            using var start = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce, sessionCookie: sessionCookie);
            var handshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, nonce);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)start.StatusCode, Is.EqualTo(200),
                    "the refused viewer is told why on the terminal page, not shown an error");
                Assert.That(handshake.HasProperty("refusalCode"), Is.True,
                    $"the Jira page must be able to stop polling and say something true; got {handshake.StatusCode} {handshake.Body}");
                Assert.That(handshake.HasProperty("token"), Is.False,
                    "a refusal must not carry a redeemable credential (D54)");
            }
        }

        /// <summary>D54, observed at the service boundary. The storage-level guarantee is probed separately.</summary>
        [Test]
        public async Task ARefusedViewer_LeavesNoRedeemableRowBehind()
        {
            var nonce = ViewerEmbedTestHost.NewNonce();
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled, ViewerEmbedTestHost.UnprovisionedViewerSubject, "Unprovisioned Viewer");

            using var start = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce, sessionCookie: sessionCookie);

            var rows = host.ReadEmbedSessionTokens();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Has.Count.EqualTo(1),
                    "exactly one row records the outcome, written at resolution rather than at router.open (D51)");
                Assert.That(rows[0].TokenId, Is.Null.Or.Empty,
                    "a refusal row holds no token id");
                Assert.That(rows[0].SecretHash, Is.Null.Or.Empty,
                    "and no secret a redemption could ever match");
            }
        }

        private static async Task<IReadOnlyList<int>> ReadTeamIdsAsync(
            WebApplicationFactory<Program> factory,
            string? sessionCookie = null,
            string? embedCookie = null)
        {
            using var response = await ViewerEmbedTestHost.GetAsViewerAsync(
                factory, ViewerEmbedTestHost.TeamsPath, sessionCookie, embedCookie);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var teams = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
            return teams is null ? [] : [.. teams.Select(team => team.Id)];
        }
    }
}
