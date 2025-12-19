using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Mvc;
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
            const int portfolioId = 1;
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
                .Invoke(forecast, [simulationResult]);
            
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
                .Returns([delivery]);
            
            var controller = CreateSubject();
            
            // Act
            var result = controller.GetByPortfolio(portfolioId);
            
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var deliveries = okResult.Value as IEnumerable<DeliveryWithLikelihoodDto> ?? throw new NullReferenceException("Deliveries is null");
            
            Assert.That(deliveries, Is.Not.Null);
            Assert.That(deliveries.Count(), Is.EqualTo(1));
            
            var deliveryDto = deliveries.First();
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryDto.Id, Is.EqualTo(1));
                Assert.That(deliveryDto.Name, Is.EqualTo("Q1 Release"));
                Assert.That(deliveryDto.LikelihoodPercentage, Is.EqualTo(80.0));
            }
        }

        [Test]
        public void GetAll_ReturnsFromAllPortfolios()
        {
            var delivery = new Delivery
            {
                Id = 1,
                Name = "Q1 Release",
                Date = DateTime.UtcNow.AddDays(30)
            };
            var delivery2 = new Delivery
            {
                Id = 2,
                Name = "Q2 Release",
                Date = DateTime.UtcNow.AddDays(120)
            };
            
            deliveryRepositoryMock.Setup(x => x.GetAll())
                .Returns([delivery, delivery2]);
            
            var controller = CreateSubject();
            
            // Act
            var result = controller.GetAll();
            
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var deliveries = okResult.Value as IEnumerable<DeliveryWithLikelihoodDto> ?? throw new NullReferenceException("Deliveries is null");
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveries, Is.Not.Null);
                Assert.That(deliveries.Count(), Is.EqualTo(2));
            
                var deliveryDto = deliveries.First();
                Assert.That(deliveryDto.Id, Is.EqualTo(1));
                Assert.That(deliveryDto.Name, Is.EqualTo("Q1 Release"));

                var deliveryDto2 = deliveries.Last();
                Assert.That(deliveryDto2.Id, Is.EqualTo(2));
                Assert.That(deliveryDto2.Name, Is.EqualTo("Q2 Release"));
            }
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
            var request = new UpdateDeliveryRequest
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
            const int portfolioId = 1;
            const string name = "Past Release";
            var pastDate = DateTime.UtcNow.AddDays(-1);
            var featureIds = new List<int>();
            
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            
            var controller = CreateSubject();

            // Act
            var request = new UpdateDeliveryRequest
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
            const int portfolioId = 1;
            const string name = "Q2 Release";
            var date = DateTime.UtcNow.AddDays(30);
            var featureIds = new List<int>();
            
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(new List<Delivery> { GetTestDelivery() });
            
            var controller = CreateSubject();

            // Act
            var request = new UpdateDeliveryRequest
            {
                Name = name,
                Date = date,
                FeatureIds = featureIds
            };
            
            var result = await controller.CreateDelivery(portfolioId, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ObjectResult>());
                var objectResult = result as ObjectResult;
                Assert.That(objectResult.StatusCode, Is.EqualTo(403));
                Assert.That(objectResult.Value, Is.EqualTo("Free users can only have 1 delivery per portfolio"));
            }
        }

        [Test]
        public async Task CreateDelivery_NonPremiumFirstDelivery_ReturnsOk()
        {
            // Arrange
            const int portfolioId = 1;
            const string name = "First Release";
            var date = DateTime.UtcNow.AddDays(30);
            var featureIds = new List<int>();
            
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(new List<Delivery>());
            
            var controller = CreateSubject();

            // Act
            var request = new UpdateDeliveryRequest
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
            const int deliveryId = 1;
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

        private static List<Feature> GetTestFeatures(List<int> ids)
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
        public void GetByPortfolio_WithFeaturesAndWork_ReturnsDeliveriesWithProgressAndFeatures()
        {
            // Arrange
            const int portfolioId = 1;
            var deliveryDate = DateTime.UtcNow.AddDays(30);
            
            // Create team and feature work
            var team = new Team { Id = 1, Name = "Test Team" };
            
            // Create feature with forecast and work
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
                .Invoke(forecast, [simulationResult]);
            
            var feature = new Feature
            {
                Id = 1,
                Name = "Test Feature"
            };
            feature.Forecasts.Add(forecast);
            
            var featureWork = new FeatureWork(team, 20, 100, feature); // 80% progress (80/100 completed)
            feature.FeatureWork.Add(featureWork);
            
            var delivery = new Delivery("Q1 Release", deliveryDate, portfolioId)
            {
                Id = 1
            };
            delivery.Features.Add(feature);
            
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns([delivery]);
            
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
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryDto.Id, Is.EqualTo(1));
                Assert.That(deliveryDto.Name, Is.EqualTo("Q1 Release"));
                Assert.That(deliveryDto.PortfolioId, Is.EqualTo(portfolioId));
                
                Assert.That(deliveryDto.LikelihoodPercentage, Is.EqualTo(80.0));
                Assert.That(deliveryDto.Progress, Is.EqualTo(80.0)); // (100-20)/100 * 100 = 80%
                
                Assert.That(deliveryDto.RemainingWork, Is.EqualTo(20));
                Assert.That(deliveryDto.TotalWork, Is.EqualTo(100));
                Assert.That(deliveryDto.Features, Is.Not.Null);
                
                Assert.That(deliveryDto.Features, Has.Count.EqualTo(1));
                Assert.That(deliveryDto.Features[0], Is.EqualTo(1));
            }
        }

        [Test]
        public void GetByPortfolio_ValidPortfolioId_ReturnsDeliveries()
        {
            // Arrange
            const int portfolioId = 1;
            var expectedDeliveries = new List<Delivery>
            {
                new("Q1 Release", DateTime.UtcNow.AddDays(30), portfolioId),
                new("Q2 Release", DateTime.UtcNow.AddDays(90), portfolioId)
            };
            
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(expectedDeliveries);
            
            var controller = CreateSubject();

            // Act
            var result = controller.GetByPortfolio(portfolioId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var deliveryDtos = okResult.Value as IEnumerable<DeliveryWithLikelihoodDto> ?? throw new NullReferenceException("DeliveryDtos is null");
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryDtos, Is.Not.Null);
                Assert.That(deliveryDtos.Count(), Is.EqualTo(2));
                Assert.That(deliveryDtos.First().Name, Is.EqualTo("Q1 Release"));
                Assert.That(deliveryDtos.Last().Name, Is.EqualTo("Q2 Release"));
            }
        }
        
        [Test]
        public async Task UpdateDelivery_WithValidRequest_ReturnsOk()
        {
            // Arrange
            const int deliveryId = 1;
            var existingDelivery = new Delivery("Original Name", DateTime.UtcNow.AddDays(10), 1);
            var feature1 = new Feature { Id = 1, Name = "Feature 1" };
            var feature2 = new Feature { Id = 2, Name = "Feature 2" };
            
            var request = new UpdateDeliveryRequest
            {
                Name = "Updated Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [1, 2]
            };

            deliveryRepositoryMock.Setup(x => x.GetById(deliveryId)).Returns(existingDelivery);
            featureRepositoryMock.Setup(x => x.GetById(1)).Returns(feature1);
            featureRepositoryMock.Setup(x => x.GetById(2)).Returns(feature2);
            deliveryRepositoryMock.Setup(x => x.Save()).Returns(Task.CompletedTask);

            var controller = new DeliveriesController(
                deliveryRepositoryMock.Object,
                featureRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                licenseServiceMock.Object);

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(existingDelivery.Name, Is.EqualTo("Updated Delivery"));
                Assert.That(existingDelivery.Date, Is.EqualTo(request.Date));
                Assert.That(existingDelivery.Features, Has.Count.EqualTo(2));
            }
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task UpdateDelivery_WithPastDate_ReturnsBadRequest()
        {
            // Arrange
            const int deliveryId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "Test Delivery",
                Date = DateTime.UtcNow.AddDays(-1), // Past date
                FeatureIds = [1]
            };

            var controller = new DeliveriesController(
                deliveryRepositoryMock.Object,
                featureRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                licenseServiceMock.Object);

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            var badRequest = result as BadRequestObjectResult;
            Assert.That(badRequest.Value, Is.EqualTo("Delivery date must be in the future"));
        }

        [Test]
        public async Task UpdateDelivery_WithEmptyName_ReturnsBadRequest()
        {
            // Arrange
            const int deliveryId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "",
                Date = DateTime.UtcNow.AddDays(10),
                FeatureIds = [1]
            };

            var controller = new DeliveriesController(
                deliveryRepositoryMock.Object,
                featureRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                licenseServiceMock.Object);

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            var badRequest = result as BadRequestObjectResult;
            Assert.That(badRequest.Value, Is.EqualTo("Name is required"));
        }

        [Test]
        public async Task UpdateDelivery_WithNonExistentDelivery_ReturnsNotFound()
        {
            // Arrange
            const int deliveryId = 999;
            var request = new UpdateDeliveryRequest
            {
                Name = "Test Delivery",
                Date = DateTime.UtcNow.AddDays(10),
                FeatureIds = [1]
            };

            deliveryRepositoryMock.Setup(x => x.GetById(deliveryId)).Returns((Delivery)null);

            var controller = new DeliveriesController(
                deliveryRepositoryMock.Object,
                featureRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                licenseServiceMock.Object);

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
            var notFound = result as NotFoundObjectResult;
            Assert.That(notFound.Value, Is.EqualTo("Delivery with ID 999 not found"));
        }

        [Test]
        public async Task UpdateDelivery_WithNonExistentFeature_ReturnsNotFound()
        {
            // Arrange
            const int deliveryId = 1;
            var existingDelivery = new Delivery("Test", DateTime.UtcNow.AddDays(10), 1);
            var request = new UpdateDeliveryRequest
            {
                Name = "Test Delivery",
                Date = DateTime.UtcNow.AddDays(10),
                FeatureIds = [999]
            };

            deliveryRepositoryMock.Setup(x => x.GetById(deliveryId)).Returns(existingDelivery);
            featureRepositoryMock.Setup(x => x.GetById(999)).Returns((Feature)null);

            var controller = new DeliveriesController(
                deliveryRepositoryMock.Object,
                featureRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                licenseServiceMock.Object);

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
            var notFound = result as NotFoundObjectResult;
            Assert.That(notFound.Value, Is.EqualTo("Feature with ID 999 does not exist"));
        }

        private static Delivery GetTestDelivery()
        {
            return new Delivery("Existing Delivery", DateTime.UtcNow.AddDays(60), 1);
        }
    }
}