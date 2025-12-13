using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class DeliveryTest
    {
        [Test]
        public void Constructor_WithValidData_CreatesDelivery()
        {
            // Arrange
            var name = "Q1 Release";
            var date = DateTime.UtcNow.AddDays(30);
            var portfolioId = 1;

            // Act
            var delivery = new Delivery(name, date, portfolioId);

            // Assert
            Assert.That(delivery.Name, Is.EqualTo(name));
            Assert.That(delivery.Date, Is.EqualTo(date));
            Assert.That(delivery.PortfolioId, Is.EqualTo(portfolioId));
            Assert.That(delivery.Features, Is.Not.Null);
            Assert.That(delivery.Features.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithPastDate_ThrowsArgumentException()
        {
            // Arrange
            var name = "Past Release";
            var pastDate = DateTime.UtcNow.AddDays(-1);
            var portfolioId = 1;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Delivery(name, pastDate, portfolioId));
            Assert.That(exception.Message, Is.EqualTo("Delivery date must be in the future"));
        }

        [Test]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var emptyName = "";
            var futureDate = DateTime.UtcNow.AddDays(30);
            var portfolioId = 1;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Delivery(emptyName, futureDate, portfolioId));
            Assert.That(exception.Message, Is.EqualTo("Name cannot be null or empty"));
        }

        [Test]
        public void Constructor_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            string nullName = null!;
            var futureDate = DateTime.UtcNow.AddDays(30);
            var portfolioId = 1;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Delivery(nullName, futureDate, portfolioId));
            Assert.That(exception.Message, Is.EqualTo("Name cannot be null or empty"));
        }

        [Test]
        public void AddFeature_ValidFeature_AddsToCollection()
        {
            // Arrange
            var delivery = new Delivery("Test Delivery", DateTime.UtcNow.AddDays(30), 1);
            var feature = new Feature();

            // Act
            delivery.Features.Add(feature);

            // Assert
            Assert.That(delivery.Features.Count, Is.EqualTo(1));
            Assert.That(delivery.Features.Contains(feature), Is.True);
        }
    }
}