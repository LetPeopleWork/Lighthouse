using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Repository
{
    public class DeliveryRepositoryTest : IntegrationTestBase
    {
        private WorkTrackingSystemConnection workTrackingSystemConnection;

        [SetUp]
        public void Setup()
        {
            workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
        }

        public DeliveryRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public void GetById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var repository = ServiceProvider.GetService<IDeliveryRepository>();
            var invalidId = 999;

            // Act
            var result = repository.GetById(invalidId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetById_WithValidId_ReturnsDeliveryWithIncludes()
        {
            // Arrange
            var repository = ServiceProvider.GetService<IDeliveryRepository>();
            var portfolioRepository = ServiceProvider.GetService<IRepository<Portfolio>>();
            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();

            var portfolio = GetTestPortfolio();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            var feature = GetTestFeature();
            feature.Portfolios.Add(portfolio); // Add feature to portfolio to prevent orphan cleanup
            featureRepository.Add(feature);
            await featureRepository.Save();
            
            var delivery = GetTestDelivery(portfolio.Id);
            repository.Add(delivery);
            await repository.Save();

            delivery.Features.Add(feature);
            await repository.Save();

            // Act
            var result = repository.GetById(delivery.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(delivery.Name));
            Assert.That(result.Portfolio, Is.Not.Null);
            Assert.That(result.Portfolio.Name, Is.EqualTo(portfolio.Name));
            Assert.That(result.Features, Is.Not.Null);
            Assert.That(result.Features.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetByPortfolioAsync_WithValidPortfolioId_ReturnsDeliveries()
        {
            // Arrange
            var repository = ServiceProvider.GetService<IDeliveryRepository>();
            var portfolioRepository = ServiceProvider.GetService<IRepository<Portfolio>>();

            var portfolio = GetTestPortfolio();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            var delivery1 = GetTestDelivery(portfolio.Id, "First Delivery");
            var delivery2 = GetTestDelivery(portfolio.Id, "Second Delivery");
            
            repository.Add(delivery1);
            repository.Add(delivery2);
            await repository.Save();

            // Act
            var result = repository.GetByPortfolioAsync(portfolio.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Any(d => d.Name == "First Delivery"), Is.True);
            Assert.That(result.Any(d => d.Name == "Second Delivery"), Is.True);
        }

        [Test]
        public async Task Add_ValidDelivery_AddsToDatabase()
        {
            // Arrange
            var repository = ServiceProvider.GetService<IDeliveryRepository>();
            var portfolioRepository = ServiceProvider.GetService<IRepository<Portfolio>>();

            var portfolio = GetTestPortfolio();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            var delivery = GetTestDelivery(portfolio.Id);

            // Act
            repository.Add(delivery);
            await repository.Save();

            // Assert
            var result = repository.GetById(delivery.Id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(delivery.Name));
        }

        private Portfolio GetTestPortfolio()
        {
            return new Portfolio
            {
                Name = "Test Portfolio",
                WorkItemQuery = "test query",
                WorkTrackingSystemConnection = workTrackingSystemConnection
            };
        }

        private Feature GetTestFeature()
        {
            return new Feature
            {
                Name = "Test Feature",
                Order = "1000",
                Type = "Epic",
                State = "New"
            };
        }

        private Delivery GetTestDelivery(int portfolioId, string name = "Test Delivery")
        {
            return new Delivery(name, DateTime.UtcNow.AddDays(30), portfolioId);
        }
    }
}