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

        [Test]
        public async Task S4_DeleteNonExistentDelivery_Returns404AndRemoveNeverCalled()
        {
            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetPortfolioId(NonExistentDeliveryId)).Returns((int?)null);
            var rbacAdministration = AlwaysAllowRbac();

            using var factory = WithMockedServices(deliveryRepository.Object, rbacAdministration.Object);
            using var client = factory.CreateClient();

            var response = await client.DeleteAsync($"/api/latest/deliveries/{NonExistentDeliveryId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            deliveryRepository.Verify(r => r.Remove(It.IsAny<int>()), Times.Never);
            deliveryRepository.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task S4_DeleteDeliveryOutsideWriteScope_Returns403AndRemoveNeverCalled()
        {
            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetPortfolioId(DeliveryIdOutOfScope)).Returns(DeliveryPortfolioId);

            var rbacAdministration = new Mock<IRbacAdministrationService>();
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

            using var factory = WithMockedServices(deliveryRepository.Object, rbacAdministration.Object);
            using var client = factory.CreateClient();

            var response = await client.DeleteAsync($"/api/latest/deliveries/{DeliveryIdOutOfScope}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            deliveryRepository.Verify(r => r.Remove(It.IsAny<int>()), Times.Never);
            deliveryRepository.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task S4_DeleteDeliveryInWriteScope_Returns204()
        {
            var deliveryRepository = new Mock<IDeliveryRepository>();
            deliveryRepository.Setup(r => r.GetPortfolioId(DeliveryIdInScope)).Returns(DeliveryPortfolioId);

            var rbacAdministration = new Mock<IRbacAdministrationService>();
            rbacAdministration
                .Setup(s => s.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.PortfolioWrite,
                    DeliveryPortfolioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            using var factory = WithMockedServices(deliveryRepository.Object, rbacAdministration.Object);
            using var client = factory.CreateClient();

            var response = await client.DeleteAsync($"/api/latest/deliveries/{DeliveryIdInScope}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            deliveryRepository.Verify(r => r.Remove(DeliveryIdInScope), Times.Once);
            deliveryRepository.Verify(r => r.Save(), Times.Once);
        }

        private WebApplicationFactory<Program> WithMockedServices(
            IDeliveryRepository deliveryRepository,
            IRbacAdministrationService rbacAdministrationService)
        {
            return new TestWebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDeliveryRepository>();
                    services.AddScoped(_ => deliveryRepository);

                    services.RemoveAll<IRbacAdministrationService>();
                    services.AddScoped(_ => rbacAdministrationService);
                });
            });
        }

        private static Mock<IRbacAdministrationService> AlwaysAllowRbac()
        {
            var rbac = new Mock<IRbacAdministrationService>();
            rbac.Setup(s => s.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<RbacGuardRequirement>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            return rbac;
        }
    }
}
