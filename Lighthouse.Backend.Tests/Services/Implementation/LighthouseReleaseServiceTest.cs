using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Moq;
using System;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class LighthouseReleaseServiceTest
    {
        private Mock<IGitHubService> githubServiceMock;
        private Mock<IHostEnvironment> hostEnvironmentMock;
        private Mock<IAssemblyService> assemblyServiceMock;

        [SetUp]
        public void Setup()
        {
            githubServiceMock = new Mock<IGitHubService>();
            hostEnvironmentMock = new Mock<IHostEnvironment>();
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
        public void GetVersion_DevVersion_ReturnsDev()
        {
            hostEnvironmentMock.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);
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
            hostEnvironmentMock.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);

            var subject = CreateSubject();

            var updateAvailable = await subject.UpdateAvailable();

            Assert.That(updateAvailable, Is.True);
        }

        [Test]
        public async Task GetNewReleases_GetsAllReleasesFromGitHubService_ReturnsNewerReleases()
        {
            var currentReleaseVersion = "13.3.7";

            var existingReleases = new List<LighthouseRelease>
            {
                new LighthouseRelease { Name = "Release4", Version = "v18.8.6" },
                new LighthouseRelease { Name = "Release3", Version = "v17.32.33" },
                new LighthouseRelease { Name = "Release2", Version = $"v{currentReleaseVersion}" },
                new LighthouseRelease { Name = "Release1", Version = "v10.2.32" },
            };

            githubServiceMock.Setup(x => x.GetLatestReleaseVersion()).ReturnsAsync("v18.8.6");
            githubServiceMock.Setup(x => x.GetAllReleases()).ReturnsAsync(existingReleases);
            assemblyServiceMock.Setup(x => x.GetAssemblyVersion()).Returns(currentReleaseVersion);

            var subject = CreateSubject();

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
            var currentReleaseVersion = "13.3.7";

            var existingReleases = new List<LighthouseRelease>
            {
                new LighthouseRelease { Name = "Release2", Version = $"v{currentReleaseVersion}" },
                new LighthouseRelease { Name = "Release1", Version = "v10.2.32" },
            };

            githubServiceMock.Setup(x => x.GetLatestReleaseVersion()).ReturnsAsync($"v{currentReleaseVersion}");
            githubServiceMock.Setup(x => x.GetAllReleases()).ReturnsAsync(existingReleases);
            assemblyServiceMock.Setup(x => x.GetAssemblyVersion()).Returns(currentReleaseVersion);

            var subject = CreateSubject();

            var newReleases = (await subject.GetNewReleases()).ToList();

            Assert.That(newReleases, Has.Count.EqualTo(0));
        }

        private LighthouseReleaseService CreateSubject()
        {
            return new LighthouseReleaseService(hostEnvironmentMock.Object, githubServiceMock.Object, assemblyServiceMock.Object);
        }
    }
}
