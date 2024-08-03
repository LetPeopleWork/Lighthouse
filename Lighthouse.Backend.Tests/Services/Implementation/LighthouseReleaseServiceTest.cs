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

        private IConfiguration SetupConfiguration(string version)
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "LighthouseVersion", version }
            };

            return TestConfiguration.SetupTestConfiguration(inMemorySettings);
        }
    }
}
