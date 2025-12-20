using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class LighthouseReleaseServiceTest
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
        [TestCase("24.8.4")]
        [TestCase("24.8.4.1224")]
        [TestCase("")]
        public void GetVersion_ProductionVersion_ReturnsFromAssemblyService(string version)
        {
            assemblyServiceMock.Setup(x => x.GetAssemblyVersion()).Returns(version);
            var subject = CreateSubject();

            var currentVersion = subject.GetCurrentVersion();

            Assert.That(currentVersion, Is.EqualTo($"v{version}"));
        }

        [Test]
        public void GetVersion_ProductionVersionEndsWithZero_RemovesLastSegment()
        {
            var version = new Version(1, 88, 7, 0);
            assemblyServiceMock.Setup(x => x.GetAssemblyVersion()).Returns(version.ToString());
            var subject = CreateSubject();

            var currentVersion = subject.GetCurrentVersion();

            Assert.That(currentVersion, Is.EqualTo($"v{version.Major}.{version.Minor}.{version.Build}"));
        }

        [Test]
        public void GetVersion_DevVersion_ReturnsDev()
        {
            platformServiceMock.SetupGet(x => x.IsDevEnvironment).Returns(true);
            assemblyServiceMock.Setup(x => x.GetAssemblyVersion()).Returns("1.33.7");

            var subject = CreateSubject();

            var currentVersion = subject.GetCurrentVersion();

            Assert.That(currentVersion, Is.EqualTo("DEV"));
        }

        [Test]
        [TestCase("24.8.3.1040", "v24.8.3.1155", true)]
        [TestCase("24.8.3", "v24.8.3.1040", true)]
        [TestCase("24.8.3", "v23.8.3.1040", false)]
        [TestCase("24.8.3", "something went wrong here", false)]
        [TestCase("24.8.3.1040", "v24.8.3.1040", false)]
        public async Task HasUpdateAvailable_ReturnsTrueIfNewerVersionAvailableAsync(string currentVersion, string latestVersion, bool newerVersionAvailable)
        {
            githubServiceMock.Setup(x => x.GetLatestReleaseVersion()).ReturnsAsync(latestVersion);
            assemblyServiceMock.Setup(x => x.GetAssemblyVersion()).Returns(currentVersion);

            var subject = CreateSubject();

            var updateAvailable = await subject.UpdateAvailable();

            Assert.That(updateAvailable, Is.EqualTo(newerVersionAvailable));
        }

        [Test]
        public async Task HasUpdateAvailable_DevVersion_ReturnsTrue()
        {
            githubServiceMock.Setup(x => x.GetLatestReleaseVersion()).ReturnsAsync("0.0.0.1");
            platformServiceMock.SetupGet(x => x.IsDevEnvironment).Returns(true);
            
            var subject = CreateSubject();

            var updateAvailable = await subject.UpdateAvailable();

            Assert.That(updateAvailable, Is.True);
        }

        [Test]
        public async Task GetNewReleases_GetsAllReleasesFromGitHubService_ReturnsNewerReleases()
        {
            const string currentReleaseVersion = "13.3.7";

            var existingReleases = new List<LighthouseRelease>
            {
                new() { Name = "Release4", Version = "v18.8.6" },
                new() { Name = "Release3", Version = "v17.32.33" },
                new() { Name = "Release2", Version = $"v{currentReleaseVersion}" },
                new() { Name = "Release1", Version = "v10.2.32" },
            };

            githubServiceMock.Setup(x => x.GetLatestReleaseVersion()).ReturnsAsync("v18.8.6");
            githubServiceMock.Setup(x => x.GetAllReleases()).ReturnsAsync(existingReleases);
            assemblyServiceMock.Setup(x => x.GetAssemblyVersion()).Returns(currentReleaseVersion);

            var subject = CreateSubject();

            var newReleases = (await subject.GetNewReleases()).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(newReleases, Has.Count.EqualTo(2));
                Assert.That(newReleases[0].Name, Is.EqualTo("Release4"));
                Assert.That(newReleases[1].Name, Is.EqualTo("Release3"));
            };
            
        }

        [Test]
        public async Task GetNewReleases_CurrentIsLatest_ReturnsEmptyList()
        {
            var currentReleaseVersion = "13.3.7";

            var existingReleases = new List<LighthouseRelease>
            {
                new() { Name = "Release2", Version = $"v{currentReleaseVersion}" },
                new() { Name = "Release1", Version = "v10.2.32" },
            };

            githubServiceMock.Setup(x => x.GetLatestReleaseVersion()).ReturnsAsync($"v{currentReleaseVersion}");
            githubServiceMock.Setup(x => x.GetAllReleases()).ReturnsAsync(existingReleases);
            assemblyServiceMock.Setup(x => x.GetAssemblyVersion()).Returns(currentReleaseVersion);

            var subject = CreateSubject();

            var newReleases = (await subject.GetNewReleases()).ToList();

            Assert.That(newReleases, Has.Count.EqualTo(0));
        }

        [Test]
        public void IsUpdateSupported_ProductionEnvironment_ReturnsTrue()
        {
            platformServiceMock.SetupGet(x => x.IsDevEnvironment).Returns(false);

            var subject = CreateSubject();

            var isSupported = subject.IsUpdateSupported();

            Assert.That(isSupported, Is.True);
        }
        
        [Test]
        [TestCase(SupportedPlatform.Docker, false)]
        [TestCase(SupportedPlatform.MacOS, false)]
        [TestCase(SupportedPlatform.Windows, true)]
        [TestCase(SupportedPlatform.Linux, true)]
        public void IsUpdateSupported_GivenPlatform_ReturnsTrueForWindowsAndLinux(SupportedPlatform platform, bool expectedResult)
        {
            platformServiceMock.SetupGet(x => x.IsDevEnvironment).Returns(false);
            platformServiceMock.SetupGet(x => x.Platform).Returns(platform);

            var subject = CreateSubject();

            var isSupported = subject.IsUpdateSupported();

            Assert.That(isSupported, Is.EqualTo(expectedResult));
        }

        private LighthouseReleaseService CreateSubject()
        {
            return new LighthouseReleaseService(githubServiceMock.Object, assemblyServiceMock.Object, platformServiceMock.Object, Mock.Of<ILogger<LighthouseReleaseService>>());
        }
    }
}
