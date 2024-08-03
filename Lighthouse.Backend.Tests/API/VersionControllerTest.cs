using Lighthouse.Backend.API;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        public void GetVersion_VersionDefinedInAppSettings_ReturnsVersionLighthouseReleaseService(string version)
        {
            lighthouseReleaseServiceMock.Setup(x => x.GetCurrentVersion()).Returns(version);
            var subject = new VersionController(lighthouseReleaseServiceMock.Object);

            var actual = (ObjectResult)subject.GetVersion();
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

            var actual = (ObjectResult)subject.GetVersion();
            Assert.Multiple(() =>
            {
                Assert.That(actual.StatusCode, Is.EqualTo(404));
                Assert.That(actual.Value, Is.EqualTo("404"));
            });
        }
    }
}
