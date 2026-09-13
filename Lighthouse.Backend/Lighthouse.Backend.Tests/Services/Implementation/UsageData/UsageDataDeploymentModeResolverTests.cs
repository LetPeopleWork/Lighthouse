using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.UsageData
{
    /// <summary>
    /// The one thing about the forwarded facts that no test running in this process can arrange for
    /// itself: a pod. Every container marker the platform service keys on is also set inside one, so
    /// an instance running on Kubernetes reports as Docker unless something asks the narrower
    /// question first - and Kubernetes adoption is close to the first thing anybody would want these
    /// numbers for.
    /// </summary>
    public class UsageDataDeploymentModeResolverTests
    {
        private const string OnlySetInsideAPod = "KUBERNETES_SERVICE_HOST";

        private string? whatThisMachineHadBefore;

        [SetUp]
        public void Setup()
        {
            whatThisMachineHadBefore = Environment.GetEnvironmentVariable(OnlySetInsideAPod);
        }

        [TearDown]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(OnlySetInsideAPod, whatThisMachineHadBefore);
        }

        [Test]
        public void Resolve_InAPodWhereEveryContainerMarkerIsAlsoTrue_SaysKubernetesRatherThanDocker()
        {
            Environment.SetEnvironmentVariable(OnlySetInsideAPod, "10.96.0.1");

            var mode = Resolve(SupportedPlatform.Docker, standalone: false);

            Assert.That(mode, Is.EqualTo(UsageDataDeploymentMode.Kubernetes),
                "the platform service cannot tell these apart, and the release path reads its answer "
                + "to decide whether an in-place update applies - so the distinction has to be drawn "
                + "here or not at all");
        }

        [Test]
        public void Resolve_InAPlainContainer_SaysDocker()
        {
            Environment.SetEnvironmentVariable(OnlySetInsideAPod, null);

            var mode = Resolve(SupportedPlatform.Docker, standalone: false);

            Assert.That(mode, Is.EqualTo(UsageDataDeploymentMode.Docker),
                "without this, the check above would pass for a resolver that answered Kubernetes to "
                + "everything");
        }

        [Test]
        public void Resolve_OnADesktopInstall_SaysStandaloneRatherThanWhichOperatingSystem()
        {
            Environment.SetEnvironmentVariable(OnlySetInsideAPod, null);

            var mode = Resolve(SupportedPlatform.Windows, standalone: true);

            Assert.That(mode, Is.EqualTo(UsageDataDeploymentMode.Standalone),
                "how it was installed is the interesting answer for a desktop build; which operating "
                + "system it landed on is the least interesting one");
        }

        [TestCase(SupportedPlatform.Windows, UsageDataDeploymentMode.Windows)]
        [TestCase(SupportedPlatform.Linux, UsageDataDeploymentMode.Linux)]
        [TestCase(SupportedPlatform.MacOS, UsageDataDeploymentMode.MacOS)]
        public void Resolve_OnAServerInstall_SaysTheOperatingSystemItIsOn(
            SupportedPlatform platform, UsageDataDeploymentMode expected)
        {
            Environment.SetEnvironmentVariable(OnlySetInsideAPod, null);

            var mode = Resolve(platform, standalone: false);

            Assert.That(mode, Is.EqualTo(expected));
        }

        private static UsageDataDeploymentMode Resolve(SupportedPlatform platform, bool standalone)
        {
            var platformMock = new Mock<IPlatformService>();
            platformMock.Setup(p => p.Platform).Returns(platform);
            platformMock.Setup(p => p.IsStandalone).Returns(standalone);

            return new UsageDataDeploymentModeResolver(platformMock.Object).Resolve();
        }
    }
}
