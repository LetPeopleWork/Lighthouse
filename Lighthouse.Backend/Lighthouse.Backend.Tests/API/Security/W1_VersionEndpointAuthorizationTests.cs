using System.Net;
using System.Reflection;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Security
{
    [TestFixture]
    public class W1_VersionEndpointAuthorizationTests
    {
        private const string InstallUpdatePath = "/api/latest/version/installUpdate";
        private const string CurrentVersionPath = "/api/latest/version/current";

        private Mock<ILighthouseReleaseService> releaseService;

        [SetUp]
        public void SetUp()
        {
            releaseService = new Mock<ILighthouseReleaseService>();
            releaseService.Setup(service => service.GetCurrentVersion()).Returns("v-test");
            releaseService.Setup(service => service.IsUpdateSupported()).Returns(true);
            releaseService.Setup(service => service.InstallUpdate()).ReturnsAsync(true);
        }

        [Test]
        public async Task InstallUpdate_AuthEnabled_AnonymousCaller_IsRejectedAndNeverInstalls()
        {
            using var factory = CreateFactoryWithAuthentication();
            using var client = factory.CreateClient();
            client.AsAnonymous();

            var response = await client.PostAsync(InstallUpdatePath, content: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    response.StatusCode,
                    Is.EqualTo(HttpStatusCode.Unauthorized).Or.EqualTo(HttpStatusCode.Forbidden));
                releaseService.Verify(service => service.InstallUpdate(), Times.Never);
            }
        }

        [Test]
        public async Task InstallUpdate_AuthEnabled_AuthenticatedViewer_IsForbidden()
        {
            using var factory = CreateFactoryWithAuthentication();
            using var client = factory.CreateClient();
            client.AsViewer();

            var response = await client.PostAsync(InstallUpdatePath, content: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                releaseService.Verify(service => service.InstallUpdate(), Times.Never);
            }
        }

        [Test]
        public async Task InstallUpdate_AuthEnabled_SystemAdmin_Installs()
        {
            using var factory = CreateFactoryWithAuthentication();
            using var client = factory.CreateClient();
            client.AsSystemAdmin();

            var response = await client.PostAsync(InstallUpdatePath, content: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                releaseService.Verify(service => service.InstallUpdate(), Times.Once);
            }
        }

        [Test]
        public async Task InstallUpdate_AuthDisabled_Installs()
        {
            using var factory = CreateFactoryWithAuthenticationDisabled();
            using var client = factory.CreateClient();

            var response = await client.PostAsync(InstallUpdatePath, content: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                releaseService.Verify(service => service.InstallUpdate(), Times.Once);
            }
        }

        // The host these tests build never registers the RequireAuthenticatedUser fallback policy, so an
        // HTTP call cannot show which routes a real deployment challenges. What the fallback covers is
        // decided entirely by where the attribute sits, and that is what is asserted here instead.
        [TestCase(nameof(VersionController.IsUpdateAvailable))]
        [TestCase(nameof(VersionController.GetNewReleases))]
        [TestCase(nameof(VersionController.IsUpdateSupported))]
        [TestCase(nameof(VersionController.GetDistributionInfo))]
        [TestCase(nameof(VersionController.InstallUpdate))]
        public void VersionAction_ExceptCurrentVersion_IsNotAnonymous(string actionName)
        {
            var action = typeof(VersionController).GetMethod(actionName);

            Assert.That(action!.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Null);
        }

        [Test]
        public void VersionController_DoesNotAllowAnonymousForEveryAction()
        {
            var controllerAttribute = typeof(VersionController).GetCustomAttribute<AllowAnonymousAttribute>();

            Assert.That(controllerAttribute, Is.Null);
        }

        [Test]
        public void InstallUpdate_RequiresSystemAdmin()
        {
            var guard = typeof(VersionController)
                .GetMethod(nameof(VersionController.InstallUpdate))!
                .GetCustomAttribute<RbacGuardAttribute>();

            Assert.That(guard?.Requirement, Is.EqualTo(RbacGuardRequirement.SystemAdmin));
        }

        // The Jira Forge app and the platform's served-version probe both call this endpoint without a
        // session, from outside this repository, against instances nobody here upgrades. It has to keep
        // answering anonymously even though every other version route now requires a signed-in caller.
        [Test]
        public async Task CurrentVersion_AuthEnabled_AnonymousCaller_StillAnswers()
        {
            using var factory = CreateFactoryWithAuthentication();
            using var client = factory.CreateClient();
            client.AsAnonymous();

            var response = await client.GetAsync(CurrentVersionPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        private WebApplicationFactory<Program> CreateFactoryWithAuthentication()
        {
            var root = new TestWebApplicationFactory<Program>();
            return TestWebApplicationFactory<Program>
                .WithTestAuthentication(root)
                .WithWebHostBuilder(WithMockedReleaseService);
        }

        private WebApplicationFactory<Program> CreateFactoryWithAuthenticationDisabled()
        {
            var root = new TestWebApplicationFactory<Program>();
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "false",
                    });
                });

                WithMockedReleaseService(builder);
            });
        }

        private void WithMockedReleaseService(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILighthouseReleaseService>();
                services.AddSingleton(releaseService.Object);
            });
        }
    }
}
