using System.Net;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// A person reads a Team in a session embedded in another tool, with everyone signed in able to view. A System Admin
    /// removes them from Users. Their next request in that session reads nothing, and no new profile appears for them.
    /// It needs a host because the embedded session is a cookie the host's own sign-in checks validate on each request.
    /// </summary>
    [TestFixture]
    public class RemovedPersonEmbedSkeletonTest : IntegrationFixture
    {
        private const string Removed = ViewerEmbedTestHost.UnprovisionedViewerSubject;

        private static readonly string TheTeam = $"{ViewerEmbedTestHost.TeamsPath}/{ViewerEmbedTestHost.GroupMappedTeamId}";

        private ViewerEmbedTestHost host = null!;

        [OneTimeSetUp]
        public void StartTheHost() => host = new ViewerEmbedTestHost();

        [SetUp]
        public void GiveTheScenarioItsOwnState()
        {
            host.BeginTest();
            host.SeedRbacFixture();
        }

        [OneTimeTearDown]
        public void StopTheHost() => host.Dispose();

        [Test]
        public async Task A_person_removed_during_an_embedded_session_reads_nothing_on_the_next_request_and_gets_no_new_profile()
        {
            var turnedOn = await TheSystemAdminTurnsEveryoneSignedInOn();
            var embedCookie = await host.EstablishEmbedCookieAsync(Removed);
            using var beforeRemoval = await ViewerEmbedTestHost.GetAsViewerAsync(host.AuthEnabled, TheTeam, embedCookie: embedCookie);

            await host.DeleteViewerAsync(Removed);

            using var afterRemoval = await ViewerEmbedTestHost.GetAsViewerAsync(host.AuthEnabled, TheTeam, embedCookie: embedCookie);
            using var theRequestAfter = await ViewerEmbedTestHost.GetAsViewerAsync(host.AuthEnabled, TheTeam, embedCookie: embedCookie);
            var listedAgain = await ListedInTheUserList(Removed);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(turnedOn, Is.EqualTo("accepted"), $"WHAT: the System Admin could not turn everyone signed in on ({turnedOn}). WHY: the threat is a removed person keeping the reach the switch gives everyone. HOW: let a System Admin save the switch on the token read.");
                Assert.That(beforeRemoval.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: before removal the embedded session read the Team with {(int)beforeRemoval.StatusCode}. WHY: with the switch on, a person with a profile and no role reads every Team, embedded or not. HOW: give an embed principal its profile's reach, switch included.");
                Assert.That(afterRemoval.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"WHAT: after removal the embedded session was answered {(int)afterRemoval.StatusCode}. WHY: a removed person must stop reading on the next request, not when the cookie expires. HOW: reject an embed principal whose profile is gone.");
                Assert.That(theRequestAfter.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"WHAT: the request after that was answered {(int)theRequestAfter.StatusCode}. WHY: a rejection that re-created the profile would let the next request in, reading everything with the switch on. HOW: look the profile up without creating it.");
                Assert.That(listedAgain, Is.False, "WHAT: the removed person is in the user list again. WHY: removal is the only control there is, so it must hold. HOW: never create a profile for an embed principal.");
            }
        }

        private async Task<string> TheSystemAdminTurnsEveryoneSignedInOn()
        {
            using var scope = host.AuthEnabled.Services.CreateScope();
            var port = scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>();
            var admin = SystemWideAccessScenario.SignedIn(ViewerEmbedTestHost.SystemAdminSubject);
            var read = await TheSwitch.Read(port);
            var saved = await TheSwitch.Save(port, admin, true, read.Token);
            return saved.Accepted ? "accepted" : saved.ToString();
        }

        private async Task<bool> ListedInTheUserList(string subject)
        {
            var adminCookie = host.ForgeInteractiveSessionCookie(host.AuthEnabled, ViewerEmbedTestHost.SystemAdminSubject, "System Admin");
            using var response = await ViewerEmbedTestHost.GetAsViewerAsync(host.AuthEnabled, "/api/v1/authorization/users", sessionCookie: adminCookie);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "WHAT: the System Admin could not read the user list. WHY: removal is checked where an administrator sees it. HOW: keep the users route open to a System Admin.");
            return (await response.Content.ReadAsStringAsync()).Contains(subject, StringComparison.Ordinal);
        }
    }
}
