using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Platform.Releases;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.Contracts
{
    /// <summary>
    /// What the update pop-up reads on the wire to learn the newest version without the release notes: the route under
    /// both versions, the plain version number for a signed-in caller, a 404 when no newer release exists, and a refusal
    /// for a caller who is signed out. The release source is replaced at its port, so GitHub is never asked.
    /// </summary>
    public class NewestVersionWireContractTest : WireContractTest
    {
        private const string InstalledVersion = "13.3.7";

        private readonly Mock<IGitHubService> releaseSource = new();

        private readonly Mock<IAssemblyService> installedVersion = new();

        protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
            => TestWebApplicationFactory<Program>.WithTestAuthentication(unchanged).WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IGitHubService>();
                    services.RemoveAll<IAssemblyService>();
                    services.AddSingleton(releaseSource.Object);
                    services.AddSingleton(installedVersion.Object);
                }));

        [SetUp]
        public void TheInstalledVersionIsKnown() => installedVersion.Setup(x => x.GetAssemblyVersion()).Returns(InstalledVersion);

        [TestCase("/api/latest/version/newest")]
        [TestCase("/api/v1/version/newest")]
        public async Task A_signed_in_user_is_told_the_newest_version_when_two_newer_releases_exist(string route)
        {
            ReleasesAre("v18.8.6", "v17.32.33", "v13.3.7", "v10.2.32");

            using var response = await Client.AsViewer().GetAsync(route);
            var body = (await response.Content.ReadAsStringAsync()).Trim('"');

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: GET {route} answered {(int)response.StatusCode}. WHY: the update pop-up asks this route for the newest version instead of loading every release note. HOW: add GET newest on the version routes under both versions.");
                Assert.That(body, Is.EqualTo("v18.8.6"), $"WHAT: the answer was {body}. WHY: the pop-up must show the same number as the first release the dialog lists. HOW: answer the version of the first newer release.");
            }
        }

        [Test]
        public async Task A_signed_in_user_gets_a_404_when_the_installed_version_is_the_newest()
        {
            ReleasesAre("v13.3.7", "v10.2.32");

            using var response = await Client.AsViewer().GetAsync("/api/latest/version/newest");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"WHAT: GET newest answered {(int)response.StatusCode} with no newer release. WHY: there is no newest version to show, and the release list answers 404 the same way. HOW: answer 404 when there is no newer release.");
        }

        [Test]
        public async Task A_signed_out_caller_is_turned_away_from_the_newest_version_like_the_update_check()
        {
            ReleasesAre("v18.8.6", "v13.3.7");

            using var update = await Client.AsAnonymous().GetAsync("/api/latest/version/hasupdate");
            using var newest = await Client.AsAnonymous().GetAsync("/api/latest/version/newest");
            var newestBody = await newest.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(update.IsSuccessStatusCode, Is.False, $"WHAT: the update check answered {(int)update.StatusCode} to a signed-out caller. WHY: this row compares against it. HOW: keep the update check for signed-in users.");
                Assert.That(newest.StatusCode, Is.EqualTo(update.StatusCode), $"WHAT: GET newest answered {(int)newest.StatusCode} to a signed-out caller while the update check answered {(int)update.StatusCode}. WHY: the newest version is for signed-in users, as the update check is. HOW: add the route as signed-in only, like the update check.");
                Assert.That(newestBody, Does.Not.Contain("v18.8.6"), "WHAT: a signed-out caller was told the newest version. WHY: the route is for signed-in users. HOW: refuse before the action runs.");
            }
        }

        private void ReleasesAre(params string[] versions)
            => releaseSource.Setup(x => x.GetAllReleases()).ReturnsAsync(
                versions.Select(version => new LighthouseRelease { Name = $"Release {version}", Version = version }).ToList());
    }
}
