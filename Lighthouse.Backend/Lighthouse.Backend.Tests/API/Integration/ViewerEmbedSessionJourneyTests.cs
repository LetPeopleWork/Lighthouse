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

        /// <summary>
        /// D49 said a viewer provisioned with nothing gets a refusal rather than an empty Lighthouse.
        /// Reversed by the maintainer 2026-08-06: the refusal protected nothing — RBAC is enforced on
        /// every request, so this viewer sees the same nothing either way — while turning the most
        /// ordinary onboarding shape there is into a dead end after a sign-in that worked.
        /// </summary>
        [Test]
        public async Task AViewerWithNoReadableScope_IsFramedAndLeftToLighthousesOwnEmptyState()
        {
            var nonce = ViewerEmbedTestHost.NewNonce();
            var sessionCookie = host.ForgeInteractiveSessionCookie(
                host.AuthEnabled, ViewerEmbedTestHost.UnprovisionedViewerSubject, "Unprovisioned Viewer");

            using var start = await ViewerEmbedTestHost.StartAsync(host.AuthEnabled, nonce, sessionCookie: sessionCookie);
            var handshake = await ViewerEmbedTestHost.PollHandshakeAsync(host.AuthEnabled, nonce);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)start.StatusCode, Is.EqualTo(200),
                    "the viewer ends on the terminal page, not on an error");
                Assert.That(handshake.HasProperty("token"), Is.True,
                    $"holding no scope is not a reason to withhold the frame; got {handshake.StatusCode} {handshake.Body}");
                Assert.That(handshake.HasProperty("refusalCode"), Is.False,
                    "and nothing is refused, so nothing names a refusal");
            }
        }

        /// <summary>
        /// The frame carries the viewer's own permissions, which for this one are none — so the
        /// emptiness has to be real rather than incidental. Without this, the test above passes
        /// equally well on a frame that quietly shows somebody else's teams.
        /// </summary>
        [Test]
        public async Task AViewerWithNoReadableScope_SeesNoTeamsThroughTheFrameTheyWereGiven()
        {
            var embedCookie = await host.EstablishEmbedCookieAsync(ViewerEmbedTestHost.UnprovisionedViewerSubject);

            using var response = await ViewerEmbedTestHost.GetAsViewerAsync(
                host.AuthEnabled, ViewerEmbedTestHost.TeamsPath, embedCookie: embedCookie);
            var teams = await ReadTeamIdsAsync(response);

            using (Assert.EnterMultipleScope())
            {
                // Read deliberately without the helper's tolerance for a failed request: it maps any
                // non-success onto an empty list, so "empty" here would otherwise also be satisfied by
                // a 401 — a frame that never worked, asserted as a frame that worked and showed nothing.
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "the frame has to actually work before its emptiness means anything");
                Assert.That(teams, Is.Empty,
                    "framed, and still holding nothing — the empty state is Lighthouse's answer, not the app's");
            }
        }

        /// <summary>
        /// The reversal of D49/D60 rests on one claim: RBAC is enforced per request, so framing a
        /// viewer who holds nothing exposes nothing. That claim is only worth what the weakest
        /// unguarded endpoint says, so it is asserted rather than argued — against the instance-wide
        /// surfaces, which is where a scope-less principal would do damage if it were false.
        /// </summary>
        [TestCase("/api/latest/logs")]
        [TestCase("/api/latest/logs/download")]
        // Not /api/latest/systeminfo itself: it carries the version and auth status the app shell
        // reads for every signed-in user, and guarding it blanked the banner. Only the refresh
        // history is instance-wide.
        [TestCase("/api/latest/systeminfo/refreshlog")]
        public async Task AViewerWithNoReadableScope_CannotReachInstanceWideSurfaces(string path)
        {
            var embedCookie = await host.EstablishEmbedCookieAsync(ViewerEmbedTestHost.UnprovisionedViewerSubject);

            using var response = await ViewerEmbedTestHost.GetAsViewerAsync(host.AuthEnabled, path, embedCookie: embedCookie);

            Assert.That((int)response.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden),
                $"{path} returns instance-wide data. Holding an embed cookie and no permission must not "
                + "be enough to read it — being signed in is not the same as being allowed");
        }

        private static async Task<IReadOnlyList<int>> ReadTeamIdsAsync(
            WebApplicationFactory<Program> factory,
            string? sessionCookie = null,
            string? embedCookie = null)
        {
            using var response = await ViewerEmbedTestHost.GetAsViewerAsync(
                factory, ViewerEmbedTestHost.TeamsPath, sessionCookie, embedCookie);

            return await ReadTeamIdsAsync(response);
        }

        // A refused request reads as no teams. That is the right answer for a caller asking
        // "which of these can they see", and the wrong one for a caller asserting emptiness —
        // which is why the empty-expecting test checks the status itself before calling this.
        private static async Task<IReadOnlyList<int>> ReadTeamIdsAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var teams = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
            return teams is null ? [] : [.. teams.Select(team => team.Id)];
        }
    }
}
