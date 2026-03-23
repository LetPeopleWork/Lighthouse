using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using System.Net;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [Category("Integration")]
    public class LighthouseReleaseServiceIntegrationTest
    {
        // Use static service to work around rate limits
        private static readonly IGitHubService GitHubService = new GitHubService();
        
        private Mock<IPlatformService> platformServiceMock;
        private Mock<IAssemblyService> assemblyServiceMock;
        private Mock<IProcessService> processServiceMock;

        [SetUp]
        public void Setup()
        {
            platformServiceMock = new Mock<IPlatformService>();
            assemblyServiceMock = new Mock<IAssemblyService>();
            processServiceMock = new Mock<IProcessService>();

            // Default: not standalone, not dev, update-capable platform
            platformServiceMock.SetupGet(x => x.IsDevEnvironment).Returns(false);
            platformServiceMock.SetupGet(x => x.IsStandalone).Returns(false);
        }

        [Test]
        [TestCase(SupportedPlatform.Windows, "win")]
        [TestCase(SupportedPlatform.Linux, "linux")]
        public async Task InstallUpdate_SupportedPlatform_ReturnsTrue(SupportedPlatform platform, string operatingSystemIdentifier)
        {
            platformServiceMock.SetupGet(x => x.Platform).Returns(platform);

            var subject = CreateSubject();

            var result = await subject.InstallUpdate();

            Assert.That(result, Is.True,
                $"InstallUpdate should return true for platform '{platform}'. " +
                $"If this fails, check that a release asset containing '{operatingSystemIdentifier}' exists on the latest GitHub release.");
        }

        [Test]
        [TestCase(SupportedPlatform.Windows)]
        [TestCase(SupportedPlatform.Linux)]
        public async Task InstallUpdate_SupportedPlatform_StartsUpdateProcess(SupportedPlatform platform)
        {
            platformServiceMock.SetupGet(x => x.Platform).Returns(platform);

            var subject = CreateSubject();

            await subject.InstallUpdate();

            processServiceMock.Verify(x => x.Start(It.IsAny<ProcessStartInfo>()), Times.Once);
        }

        [Test]
        [TestCase(SupportedPlatform.Windows)]
        [TestCase(SupportedPlatform.Linux)]
        public async Task InstallUpdate_SupportedPlatform_ExitsCurrentProcess(SupportedPlatform platform)
        {
            platformServiceMock.SetupGet(x => x.Platform).Returns(platform);

            var subject = CreateSubject();

            await subject.InstallUpdate();

            processServiceMock.Verify(x => x.Exit(0), Times.Once);
        }

        [Test]
        [TestCase(SupportedPlatform.Windows, ".bat")]
        [TestCase(SupportedPlatform.Linux, ".sh")]
        public async Task InstallUpdate_SupportedPlatform_StartsUpdateScriptWithCorrectExtension(SupportedPlatform platform, string expectedScriptExtension)
        {
            platformServiceMock.SetupGet(x => x.Platform).Returns(platform);

            ProcessStartInfo? capturedStartInfo = null;
            processServiceMock
                .Setup(x => x.Start(It.IsAny<ProcessStartInfo>()))
                .Callback<ProcessStartInfo>(si => capturedStartInfo = si);

            var subject = CreateSubject();

            await subject.InstallUpdate();

            Assert.That(capturedStartInfo, Is.Not.Null);

            var scriptFile = platform == SupportedPlatform.Windows
                ? capturedStartInfo!.FileName
                : capturedStartInfo!.Arguments.Trim('"');

            Assert.That(scriptFile, Does.EndWith(expectedScriptExtension),
                $"Expected update script to have extension '{expectedScriptExtension}' on {platform}");
        }

        private LighthouseReleaseService CreateSubject()
        {
            return new LighthouseReleaseService(
                GitHubService,
                assemblyServiceMock.Object,
                platformServiceMock.Object,
                processServiceMock.Object,
                Mock.Of<ILogger<LighthouseReleaseService>>(),
                TimeSpan.Zero,
                CreateMockHttpClient());
        }
        
        private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode = HttpStatusCode.OK, byte[]? content = null)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new ByteArrayContent(content ?? Array.Empty<byte>())
                });

            return new HttpClient(handlerMock.Object);
        }
    }
}