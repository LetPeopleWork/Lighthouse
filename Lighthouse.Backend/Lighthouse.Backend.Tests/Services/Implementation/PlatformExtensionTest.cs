using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class PlatformExtensionTest
    {
        [Test]
        [TestCase(SupportedPlatform.Docker, "docker")]
        [TestCase(SupportedPlatform.Linux, "linux")]
        [TestCase(SupportedPlatform.MacOS, "osx")]
        [TestCase(SupportedPlatform.Windows, "win")]
        public void GetPlatformIdentifier_ReturnsCorrectIdentifier(SupportedPlatform platform,
            string expectedIdentifier)
        {
            var actualIdentifier = platform.GetIdentifier();
            
            Assert.That(actualIdentifier, Is.EqualTo(expectedIdentifier));
        }
    }
}