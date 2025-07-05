using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class VersionControllerTest
    {
        private Mock<ILighthouseReleaseService> lighthouseReleaseServiceMock;

        [SetUp]
        public void SetUp()
        {
            lighthouseReleaseServiceMock = new Mock<ILighthouseReleaseService>();
        }

        [Test]
        [TestCase("1.33.7")]
        [TestCase("DEV")]
        public void GetVersion_VersionDefinedInAppSettings_ReturnsVersionFromLighthouseReleaseService(string version)
        {
            lighthouseReleaseServiceMock.Setup(x => x.GetCurrentVersion()).Returns(version);
            var subject = new VersionController(lighthouseReleaseServiceMock.Object);

            var actual = (ObjectResult)subject.GetCurrentVersion();
            Assert.Multiple(() =>
            {
                Assert.That(actual.StatusCode, Is.EqualTo(200));
                Assert.That(actual.Value, Is.EqualTo(version));
            });
        }

        [Test]
        public void GetVersion_VersionNotDefinedInAppSettings_ReturnsNotFound()
        {
            lighthouseReleaseServiceMock.Setup(x => x.GetCurrentVersion()).Returns(string.Empty);
            var subject = new VersionController(lighthouseReleaseServiceMock.Object);

            var actual = (ObjectResult)subject.GetCurrentVersion();
            Assert.Multiple(() =>
            {
                Assert.That(actual.StatusCode, Is.EqualTo(404));
                Assert.That(actual.Value, Is.EqualTo("404"));
            });
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task IsUpdateAvailable_ChecksWithLighthouseReleaseServiceAsync(bool isUpdateAvailable)
        {
            lighthouseReleaseServiceMock.Setup(x => x.UpdateAvailable()).ReturnsAsync(isUpdateAvailable);
            var subject = new VersionController(lighthouseReleaseServiceMock.Object);

            var actual = await subject.IsUpdateAvailable() as ObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(actual.StatusCode, Is.EqualTo(200));
                Assert.That(actual.Value, Is.EqualTo(isUpdateAvailable));
            });
        }

        [Test]
        public async Task GetNewReleases_ReturnsNewerVersion()
        {
            var lighthouseRelease = new LighthouseRelease { Name = "MyRelease", Version = "v13.3.7" };
            lighthouseReleaseServiceMock.Setup(x => x.GetNewReleases()).ReturnsAsync([lighthouseRelease]);

            var subject = new VersionController(lighthouseReleaseServiceMock.Object);

            var response = await subject.GetNewReleases();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;

                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                var newReleases = okResult.Value as IEnumerable<LighthouseRelease>;

                Assert.That(newReleases, Does.Contain(lighthouseRelease));
            });
        }

        [Test]
        public async Task GetNewReleases_NoNewReleaseExists_ReturnsNotFoundAsync()
        {
            lighthouseReleaseServiceMock.Setup(x => x.GetNewReleases()).ReturnsAsync([]);

            var subject = new VersionController(lighthouseReleaseServiceMock.Object);

            var response = await subject.GetNewReleases();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void IsUpdateSupported_ReturnsCorrectValue(bool isSupported)
        {
            lighthouseReleaseServiceMock.Setup(x => x.IsUpdateSupported()).Returns(isSupported);
            var subject = new VersionController(lighthouseReleaseServiceMock.Object);

            var response = subject.IsUpdateSupported();
            var actual = response.Result as ObjectResult;

            Assert.Multiple(() =>
            {
                Assert.That(actual.StatusCode, Is.EqualTo(200));
                Assert.That(actual.Value, Is.EqualTo(isSupported));
            });
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task InstallUpdate_ReturnsCorrectValue(bool installResult)
        {
            lighthouseReleaseServiceMock.Setup(x => x.InstallUpdate()).ReturnsAsync(installResult);
            var subject = new VersionController(lighthouseReleaseServiceMock.Object);

            var response = await subject.InstallUpdate();
            var actual = response.Result as ObjectResult;

            Assert.Multiple(() =>
            {
                Assert.That(actual.StatusCode, Is.EqualTo(200));
                Assert.That(actual.Value, Is.EqualTo(installResult));
            });
        }
    }
}
