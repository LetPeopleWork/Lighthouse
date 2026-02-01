using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class DeliveriesControllerUtcTest
    {
        private Mock<IDeliveryRepository> deliveryRepository;
        private Mock<IRepository<Feature>> featureRepository;
        private Mock<IRepository<Portfolio>> portfolioRepository;
        private Mock<ILicenseService> licenseService;
        private DeliveriesController subject;

        [SetUp]
        public void Setup()
        {
            deliveryRepository = new Mock<IDeliveryRepository>();
            featureRepository = new Mock<IRepository<Feature>>();
            portfolioRepository = new Mock<IRepository<Portfolio>>();
            licenseService = new Mock<ILicenseService>();

            subject = new DeliveriesController(
                deliveryRepository.Object,
                featureRepository.Object,
                portfolioRepository.Object,
                licenseService.Object,
                Mock.Of<IDeliveryRuleService>());
        }

        [Test]
        public async Task CreateDelivery_WithUtcDate_StoresUtcDate()
        {
            // Arrange
            var portfolioId = 1;
            var futureDate = DateTime.UtcNow.AddDays(30);
            var request = new UpdateDeliveryRequest
            {
                Name = "Test Delivery",
                Date = futureDate,
                FeatureIds = new List<int>()
            };

            portfolioRepository.Setup(x => x.GetById(portfolioId))
                .Returns(new Portfolio { Id = portfolioId, Name = "Test Portfolio" });
            licenseService.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            Delivery? capturedDelivery = null;
            deliveryRepository.Setup(x => x.Add(It.IsAny<Delivery>()))
                .Callback<Delivery>(d => capturedDelivery = d);

            // Act
            await subject.CreateDelivery(portfolioId, request);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(capturedDelivery, Is.Not.Null);

                Assert.That(capturedDelivery.Date.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(capturedDelivery.Date, Is.EqualTo(futureDate));
            }

        }

        [Test]
        public async Task CreateDelivery_WithUnspecifiedKindDate_ConvertsToUtc()
        {
            // Arrange
            var portfolioId = 1;
            var futureDate = new DateTime(2026, 12, 31, 10, 0, 0, DateTimeKind.Unspecified);
            var request = new UpdateDeliveryRequest
            {
                Name = "Test Delivery",
                Date = futureDate,
                FeatureIds = new List<int>()
            };

            portfolioRepository.Setup(x => x.GetById(portfolioId))
                .Returns(new Portfolio { Id = portfolioId, Name = "Test Portfolio" });
            licenseService.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            Delivery? capturedDelivery = null;
            deliveryRepository.Setup(x => x.Add(It.IsAny<Delivery>()))
                .Callback<Delivery>(d => capturedDelivery = d);

            // Act
            await subject.CreateDelivery(portfolioId, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(capturedDelivery, Is.Not.Null);
                Assert.That(capturedDelivery.Date.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(capturedDelivery.Date.Year, Is.EqualTo(2026));
                Assert.That(capturedDelivery.Date.Month, Is.EqualTo(12));
                Assert.That(capturedDelivery.Date.Day, Is.EqualTo(31));
            }

        }

        [Test]
        public async Task CreateDelivery_WithLocalDate_ConvertsToUtc()
        {
            // Arrange
            var portfolioId = 1;
            var localDate = new DateTime(2026, 12, 31, 10, 0, 0, DateTimeKind.Local);
            var request = new UpdateDeliveryRequest
            {
                Name = "Test Delivery",
                Date = localDate,
                FeatureIds = new List<int>()
            };

            portfolioRepository.Setup(x => x.GetById(portfolioId))
                .Returns(new Portfolio { Id = portfolioId, Name = "Test Portfolio" });
            licenseService.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            Delivery? capturedDelivery = null;
            deliveryRepository.Setup(x => x.Add(It.IsAny<Delivery>()))
                .Callback<Delivery>(d => capturedDelivery = d);

            // Act
            await subject.CreateDelivery(portfolioId, request);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(capturedDelivery, Is.Not.Null);
                Assert.That(capturedDelivery.Date.Kind, Is.EqualTo(DateTimeKind.Utc));
            }
        }

        [Test]
        public async Task UpdateDelivery_WithUtcDate_StoresUtcDate()
        {
            // Arrange
            var deliveryId = 1;
            var futureDate = DateTime.UtcNow.AddDays(30);
            var request = new UpdateDeliveryRequest
            {
                Name = "Updated Delivery",
                Date = futureDate,
                FeatureIds = new List<int>()
            };

            var existingDelivery = new Delivery("Old Name", DateTime.UtcNow.AddDays(60), 1);
            deliveryRepository.Setup(x => x.GetById(deliveryId))
                .Returns(existingDelivery);

            // Act
            await subject.UpdateDelivery(deliveryId, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(existingDelivery.Date.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(existingDelivery.Date, Is.EqualTo(futureDate));
            }

        }

        [Test]
        public async Task UpdateDelivery_WithUnspecifiedKindDate_ConvertsToUtc()
        {
            // Arrange
            var deliveryId = 1;
            var futureDate = new DateTime(2026, 12, 31, 10, 0, 0, DateTimeKind.Unspecified);
            var request = new UpdateDeliveryRequest
            {
                Name = "Updated Delivery",
                Date = futureDate,
                FeatureIds = new List<int>()
            };

            var existingDelivery = new Delivery("Old Name", DateTime.UtcNow.AddDays(60), 1);
            deliveryRepository.Setup(x => x.GetById(deliveryId))
                .Returns(existingDelivery);

            // Act
            await subject.UpdateDelivery(deliveryId, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(existingDelivery.Date.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(existingDelivery.Date.Year, Is.EqualTo(2026));
                Assert.That(existingDelivery.Date.Month, Is.EqualTo(12));
                Assert.That(existingDelivery.Date.Day, Is.EqualTo(31));
            }

        }

        [Test]
        public async Task UpdateDelivery_WithLocalDate_ConvertsToUtc()
        {
            // Arrange
            var deliveryId = 1;
            var localDate = new DateTime(2026, 12, 31, 10, 0, 0, DateTimeKind.Local);
            var request = new UpdateDeliveryRequest
            {
                Name = "Updated Delivery",
                Date = localDate,
                FeatureIds = new List<int>()
            };

            var existingDelivery = new Delivery("Old Name", DateTime.UtcNow.AddDays(60), 1);
            deliveryRepository.Setup(x => x.GetById(deliveryId))
                .Returns(existingDelivery);

            // Act
            await subject.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(existingDelivery.Date.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void GetByPortfolio_ReturnsDeliveriesWithUtcDates()
        {
            // Arrange
            var portfolioId = 1;
            var deliveries = new List<Delivery>
            {
                new Delivery("Delivery 1", DateTime.UtcNow.AddDays(30), portfolioId),
                new Delivery("Delivery 2", DateTime.UtcNow.AddDays(60), portfolioId)
            };

            deliveryRepository.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(deliveries);

            // Act
            var result = subject.GetByPortfolio(portfolioId) as OkObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var dtos = result.Value as IEnumerable<DeliveryWithLikelihoodDto>;
            Assert.That(dtos, Is.Not.Null);

            foreach (var dto in dtos!)
            {
                Assert.That(dto.Date.Kind, Is.EqualTo(DateTimeKind.Utc),
                    $"Delivery '{dto.Name}' date should be in UTC");
            }
        }
    }
}
