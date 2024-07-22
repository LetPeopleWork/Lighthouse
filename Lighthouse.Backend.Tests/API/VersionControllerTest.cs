using Lighthouse.Backend.API;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Lighthouse.Backend.Tests.API
{
    public class VersionControllerTest
    {
        [Test]
        [TestCase("1.33.7")]
        [TestCase("DEV")]
        public void GetVersion_VersionDefinedInAppSettings_ReturnsVersionFromAppSettings(string version)
        {
            var config = SetupConfiguration(version);
            var subject = new VersionController(config);

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
            var config = SetupConfiguration(string.Empty);
            var subject = new VersionController(config);

            var actual = (ObjectResult)subject.GetVersion();
            Assert.Multiple(() =>
            {
                Assert.That(actual.StatusCode, Is.EqualTo(404));
                Assert.That(actual.Value, Is.EqualTo("404"));
            });
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
