using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class DeliveriesControllerTest
    {
        private Mock<IDeliveryRepository> deliveryRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        
        private Mock<ILicenseService> licenseServiceMock;

        [SetUp]
        public void Setup()
        {
            deliveryRepositoryMock = new Mock<IDeliveryRepository>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            licenseServiceMock = new Mock<ILicenseService>();
            
            portfolioRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns(new Portfolio());
        }

        [Test]
        public void GetByPortfolio_WithForecastedFeatures_ReturnsDeliveriesWithLikelihood()
        {
            // Arrange
            var portfolioId = 1;
            var deliveryDate = DateTime.UtcNow.AddDays(30);
            
            // Create feature with 80% likelihood forecast
            var simulationResult = new Dictionary<int, int>
            {
                { 10, 20 },
                { 20, 30 },
                { 30, 30 }, // Total: 80 out of 100 = 80%
                { 40, 20 }
            };
            var forecast = new WhenForecast();
            forecast.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast, new object[] { simulationResult });
            
            var feature = new Feature();
            feature.Forecasts.Add(forecast);
            
            var delivery = new Delivery
            {
                Id = 1,
                Name = "Q1 Release",
                Date = deliveryDate
            };
            delivery.Features.Add(feature);
            
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(new[] { delivery });
            
            var controller = CreateSubject();
            
            // Act
            var result = controller.GetByPortfolio(portfolioId);
            
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var deliveries = okResult.Value as IEnumerable<DeliveryWithLikelihoodDto>;
            
            Assert.That(deliveries, Is.Not.Null);
            Assert.That(deliveries.Count(), Is.EqualTo(1));
            
            var deliveryDto = deliveries.First();
            Assert.That(deliveryDto.Id, Is.EqualTo(1));
            Assert.That(deliveryDto.Name, Is.EqualTo("Q1 Release"));
            Assert.That(deliveryDto.LikelihoodPercentage, Is.EqualTo(80.0));
        }

        [Test]
        public async Task CreateDelivery_ValidData_ReturnsOk()
        {
            // Arrange
            var portfolioId = 1;
            var name = "Q1 Release";
            var date = DateTime.UtcNow.AddDays(30);
            var featureIds = new List<int> { 1, 2 };
            
            var features = GetTestFeatures(featureIds);
            featureRepositoryMock.Setup(x => x.GetById(1)).Returns(features[0]);
            featureRepositoryMock.Setup(x => x.GetById(2)).Returns(features[1]);
            
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            
            var controller = CreateSubject();

            // Act
            var request = new CreateDeliveryRequest
            {
                Name = name,
                Date = date,
                FeatureIds = featureIds
            };
            var result = await controller.CreateDelivery(portfolioId, request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            deliveryRepositoryMock.Verify(x => x.Add(It.IsAny<Delivery>()), Times.Once);
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task CreateDelivery_PastDate_ReturnsBadRequest()
        {
            // Arrange
            var portfolioId = 1;
            var name = "Past Release";
            var pastDate = DateTime.UtcNow.AddDays(-1);
            var featureIds = new List<int>();
            
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            
            var controller = CreateSubject();

            // Act
            var request = new CreateDeliveryRequest
            {
                Name = name,
                Date = pastDate,
                FeatureIds = featureIds
            };
            var result = await controller.CreateDelivery(portfolioId, request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult.Value, Is.EqualTo("Delivery date must be in the future"));
        }

        [Test]
        public async Task CreateDelivery_NonPremiumWithExistingDelivery_ReturnsForbidden()
        {
            // Arrange
            var portfolioId = 1;
            var name = "Q2 Release";
            var date = DateTime.UtcNow.AddDays(30);
            var featureIds = new List<int>();
            
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(new List<Delivery> { GetTestDelivery() });
            
            var controller = CreateSubject();

            // Act
            var request = new CreateDeliveryRequest
            {
                Name = name,
                Date = date,
                FeatureIds = featureIds
            };
            var result = await controller.CreateDelivery(portfolioId, request);

            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult.StatusCode, Is.EqualTo(403));
            Assert.That(objectResult.Value, Is.EqualTo("Free users can only have 1 delivery per portfolio"));
        }

        [Test]
        public async Task CreateDelivery_NonPremiumFirstDelivery_ReturnsOk()
        {
            // Arrange
            var portfolioId = 1;
            var name = "First Release";
            var date = DateTime.UtcNow.AddDays(30);
            var featureIds = new List<int>();
            
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(new List<Delivery>());
            
            var controller = CreateSubject();

            // Act
            var request = new CreateDeliveryRequest
            {
                Name = name,
                Date = date,
                FeatureIds = featureIds
            };
            var result = await controller.CreateDelivery(portfolioId, request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            deliveryRepositoryMock.Verify(x => x.Add(It.IsAny<Delivery>()), Times.Once);
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task DeleteDelivery_ValidId_ReturnsNoContent()
        {
            // Arrange
            var deliveryId = 1;
            var existingDelivery = GetTestDelivery();
            
            deliveryRepositoryMock.Setup(x => x.GetById(deliveryId))
                .Returns(existingDelivery);
            
            var controller = CreateSubject();

            // Act
            var result = await controller.DeleteDelivery(deliveryId);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
            deliveryRepositoryMock.Verify(x => x.Remove(deliveryId), Times.Once);
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        private DeliveriesController CreateSubject()
        {
            return new DeliveriesController(
                deliveryRepositoryMock.Object,
                featureRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                licenseServiceMock.Object);
        }

        private List<Feature> GetTestFeatures(List<int> ids)
        {
            return ids.Select(id => new Feature
            {
                Id = id,
                Name = $"Feature {id}",
                Order = "1000",
                Type = "Epic",
                State = "New"
            }).ToList();
        }

        [Test]
        public void GetByPortfolio_ValidPortfolioId_ReturnsDeliveries()
        {
            // Arrange
            var portfolioId = 1;
            var expectedDeliveries = new List<Delivery>
            {
                new Delivery("Q1 Release", DateTime.UtcNow.AddDays(30), portfolioId),
                new Delivery("Q2 Release", DateTime.UtcNow.AddDays(90), portfolioId)
            };
            
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(expectedDeliveries);
            
            var controller = CreateSubject();

            // Act
            var result = controller.GetByPortfolio(portfolioId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var deliveryDtos = okResult.Value as IEnumerable<DeliveryWithLikelihoodDto>;
            
            Assert.That(deliveryDtos, Is.Not.Null);
            Assert.That(deliveryDtos.Count(), Is.EqualTo(2));
            Assert.That(deliveryDtos.First().Name, Is.EqualTo("Q1 Release"));
            Assert.That(deliveryDtos.Last().Name, Is.EqualTo("Q2 Release"));
        }

        private Delivery GetTestDelivery()
        {
            return new Delivery("Existing Delivery", DateTime.UtcNow.AddDays(60), 1);
        }
    }
}