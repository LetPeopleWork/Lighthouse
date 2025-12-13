using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
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
        private Mock<ILicenseService> licenseServiceMock;

        [SetUp]
        public void Setup()
        {
            deliveryRepositoryMock = new Mock<IDeliveryRepository>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            licenseServiceMock = new Mock<ILicenseService>();
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
            var result = await controller.CreateDelivery(portfolioId, name, date, featureIds);

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
            var result = await controller.CreateDelivery(portfolioId, name, pastDate, featureIds);

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
            var result = await controller.CreateDelivery(portfolioId, name, date, featureIds);

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
            var result = await controller.CreateDelivery(portfolioId, name, date, featureIds);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            deliveryRepositoryMock.Verify(x => x.Add(It.IsAny<Delivery>()), Times.Once);
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        private DeliveriesController CreateSubject()
        {
            return new DeliveriesController(
                deliveryRepositoryMock.Object,
                featureRepositoryMock.Object,
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

        private Delivery GetTestDelivery()
        {
            return new Delivery("Existing Delivery", DateTime.UtcNow.AddDays(60), 1);
        }
    }
}