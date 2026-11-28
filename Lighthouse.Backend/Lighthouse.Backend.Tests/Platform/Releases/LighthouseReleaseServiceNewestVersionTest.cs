using Lighthouse.Backend.Models;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Platform.Releases;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Platform.Releases
{
    public class LighthouseReleaseServiceNewestVersionTest
    {
        private Mock<IGitHubService> githubServiceMock;
        private Mock<IPlatformService> platformServiceMock;
        private Mock<IAssemblyService> assemblyServiceMock;

        [SetUp]
        public void Setup()
        {
            githubServiceMock = new Mock<IGitHubService>();
            platformServiceMock = new Mock<IPlatformService>();
            assemblyServiceMock = new Mock<IAssemblyService>();
        }

        [Test]
        public async Task NewestVersion_TwoNewerReleases_IsTheVersionTheUpdatePopUpShowsFirst()
        {
            GivenInstalledVersionAndReleases("13.3.7", "v18.8.6", "v17.32.33", "v13.3.7", "v10.2.32");
            var subject = CreateSubject();

            var newest = await NewestVersionOf(subject);
            var firstNewRelease = (await subject.GetNewReleases()).First().Version;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(newest, Is.EqualTo("v18.8.6"), "The newest version must be the first release newer than the installed one.");
                Assert.That(newest, Is.EqualTo(firstNewRelease), "The number must equal what the dialog lists first today.");
            }
        }

        [Test]
        public async Task NewestVersion_InstalledVersionIsTheNewest_IsNone()
        {
            GivenInstalledVersionAndReleases("13.3.7", "v13.3.7", "v10.2.32");

            var newest = await NewestVersionOf(CreateSubject());

            Assert.That(newest, Is.Null, "With no newer release there is no newest version to report.");
        }

        [Test]
        public async Task NewestVersion_DevelopmentBuild_IsTheNewestOfAllReleases()
        {
            platformServiceMock.SetupGet(x => x.IsDevEnvironment).Returns(true);
            githubServiceMock.Setup(x => x.GetAllReleases()).ReturnsAsync(
            [
                new LighthouseRelease { Name = "Release2", Version = "v18.8.6" },
                new LighthouseRelease { Name = "Release1", Version = "v17.32.33" },
            ]);

            var newest = await NewestVersionOf(CreateSubject());

            Assert.That(newest, Is.EqualTo("v18.8.6"), "A development build counts every release as newer, so the newest of all is reported.");
        }

        private void GivenInstalledVersionAndReleases(string installed, params string[] releaseVersions)
        {
            assemblyServiceMock.Setup(x => x.GetAssemblyVersion()).Returns(installed);
            githubServiceMock.Setup(x => x.GetAllReleases()).ReturnsAsync(
                releaseVersions.Select(version => new LighthouseRelease { Name = $"Release {version}", Version = version }).ToList());
        }

        private static async Task<string?> NewestVersionOf(LighthouseReleaseService subject)
        {
            return await subject.GetNewestReleaseVersion();
        }

        private LighthouseReleaseService CreateSubject()
        {
            return new LighthouseReleaseService(githubServiceMock.Object, assemblyServiceMock.Object, platformServiceMock.Object, Mock.Of<IProcessService>(), Mock.Of<ILogger<LighthouseReleaseService>>());
        }
    }
}
