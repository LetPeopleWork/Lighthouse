using System.Net;
using System.Security.Claims;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Security
{
    public class S4_DeliveriesDeleteGuardInversionTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        private const int NonExistentDeliveryId = 9999;
        private const int CallerScopedPortfolioId = 200;
        private const int DeliveryPortfolioId = 100;
        private const int DeliveryIdInScope = 7;
        private const int DeliveryIdOutOfScope = 8;

        private readonly Mock<IDeliveryRepository> deliveryRepository = new();
        private readonly Mock<IRbacAdministrationService> rbacAdministration = new();

        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;

        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            factory = WebApplicationFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDeliveryRepository>();
                    services.AddScoped(_ => deliveryRepository.Object);

                    services.RemoveAll<IRbacAdministrationService>();
                    services.AddScoped(_ => rbacAdministration.Object);
                });
            });
        }

        [SetUp]
        public void ResetTheDoubles()
        {
            WebApplicationFactory.Clock.Reset();
            deliveryRepository.Reset();
            rbacAdministration.Reset();

            client = factory.CreateClient();
        }

        [TearDown]
        public void DropTheClient()
        {
            client.Dispose();
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            factory?.Dispose();
        }

        [Test]
        public async Task S4_DeleteNonExistentDelivery_Returns404AndRemoveNeverCalled()
        {
            deliveryRepository.Setup(r => r.GetPortfolioId(NonExistentDeliveryId)).Returns((int?)null);
            AlwaysAllowRbac();

            var response = await client.DeleteAsync($"/api/latest/deliveries/{NonExistentDeliveryId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            deliveryRepository.Verify(r => r.Remove(It.IsAny<int>()), Times.Never);
            deliveryRepository.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task S4_DeleteDeliveryOutsideWriteScope_Returns403AndRemoveNeverCalled()
        {
            deliveryRepository.Setup(r => r.GetPortfolioId(DeliveryIdOutOfScope)).Returns(DeliveryPortfolioId);

            rbacAdministration
                .Setup(s => s.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.PortfolioWrite,
                    DeliveryPortfolioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            rbacAdministration
                .Setup(s => s.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.PortfolioWrite,
                    CallerScopedPortfolioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var response = await client.DeleteAsync($"/api/latest/deliveries/{DeliveryIdOutOfScope}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            deliveryRepository.Verify(r => r.Remove(It.IsAny<int>()), Times.Never);
            deliveryRepository.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task S4_DeleteDeliveryInWriteScope_Returns204()
        {
            deliveryRepository.Setup(r => r.GetPortfolioId(DeliveryIdInScope)).Returns(DeliveryPortfolioId);

            rbacAdministration
                .Setup(s => s.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.PortfolioWrite,
                    DeliveryPortfolioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var response = await client.DeleteAsync($"/api/latest/deliveries/{DeliveryIdInScope}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            deliveryRepository.Verify(r => r.Remove(DeliveryIdInScope), Times.Once);
            deliveryRepository.Verify(r => r.Save(), Times.Once);
        }

        private void AlwaysAllowRbac()
        {
            rbacAdministration.Setup(s => s.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<RbacGuardRequirement>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }
    }
}
