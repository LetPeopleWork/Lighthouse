using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class LighthouseReleaseServiceTest
    {
        private Mock<IGitHubService> githubServiceMock;

        [SetUp]
        public void Setup()
        {
            githubServiceMock = new Mock<IGitHubService>();
        }

        [Test]
        [TestCase("1.33.7")]
        [TestCase("DEV")]
        [TestCase("")]
        public void GetVersion_VersionDefinedInAppSettings_ReturnsVersionFromAppSettings(string version)
        {
            var config = SetupConfiguration(version);
            var subject = new LighthouseReleaseService(config, githubServiceMock.Object);

            var currentVersion = subject.GetCurrentVersion();

            Assert.That(currentVersion, Is.EqualTo(version));
        }

        [Test]
        [TestCase("v24.8.3.1040", "v24.8.3.1155", true)]
        [TestCase("v24.8.3", "v24.8.3.1040", true)]
        [TestCase("v24.8.3", "v23.8.3.1040", false)]
        [TestCase("v24.8.3", "something went wrong here", false)]
        [TestCase("DEV", "v23.8.3.1040", true)]
        [TestCase("v24.8.3.1040", "v24.8.3.1040", false)]
        public async Task HasUpdateAvailable_ReturnsTrueIfNewerVersionAvailableAsync(string currentVersion, string latestVersion, bool newerVersionAvailable)
        {
            var config = SetupConfiguration(currentVersion);

            githubServiceMock.Setup(x => x.GetLatestReleaseVersion()).ReturnsAsync(latestVersion);

            var subject = new LighthouseReleaseService(config, githubServiceMock.Object);

            var updateAvailable = await subject.UpdateAvailable();

            Assert.That(updateAvailable, Is.EqualTo(newerVersionAvailable));
        }

        [Test]
        public async Task GetNewReleases_GetsAllReleasesFromGitHubService_ReturnsNewerReleases()
        {
            var currentReleaseVersion = "v13.3.7";

            var existingReleases = new List<LighthouseRelease>
            {
                new LighthouseRelease { Name = "Release4", Version = "v18.8.6" },
                new LighthouseRelease { Name = "Release3", Version = "v17.32.33" },
                new LighthouseRelease { Name = "Release2", Version = currentReleaseVersion },
                new LighthouseRelease { Name = "Release1", Version = "v10.2.32" },
            };

            githubServiceMock.Setup(x => x.GetLatestReleaseVersion()).ReturnsAsync("v18.8.6");
            githubServiceMock.Setup(x => x.GetAllReleases()).ReturnsAsync(existingReleases);

            var config = SetupConfiguration(currentReleaseVersion);
            var subject = new LighthouseReleaseService(config, githubServiceMock.Object);

            var newReleases = (await subject.GetNewReleases()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(newReleases, Has.Count.EqualTo(2));
                Assert.That(newReleases[0].Name, Is.EqualTo("Release4"));
                Assert.That(newReleases[1].Name, Is.EqualTo("Release3"));
            });
            
        }

        [Test]
        public async Task GetNewReleases_CurrentIsLatest_ReturnsEmptyList()
        {
            var currentReleaseVersion = "v13.3.7";

            var existingReleases = new List<LighthouseRelease>
            {
                new LighthouseRelease { Name = "Release2", Version = currentReleaseVersion },
                new LighthouseRelease { Name = "Release1", Version = "v10.2.32" },
            };

            githubServiceMock.Setup(x => x.GetLatestReleaseVersion()).ReturnsAsync(currentReleaseVersion);
            githubServiceMock.Setup(x => x.GetAllReleases()).ReturnsAsync(existingReleases);

            var config = SetupConfiguration(currentReleaseVersion);
            var subject = new LighthouseReleaseService(config, githubServiceMock.Object);

            var newReleases = (await subject.GetNewReleases()).ToList();

            Assert.That(newReleases, Has.Count.EqualTo(0));

        }

        private IConfiguration SetupConfiguration(string version = "DEV")
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "LighthouseVersion", version }
            };

            return TestConfiguration.SetupTestConfiguration(inMemorySettings);
        }
    }
}
