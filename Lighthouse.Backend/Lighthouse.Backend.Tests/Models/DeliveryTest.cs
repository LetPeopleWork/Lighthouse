using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class DeliveryTest
    {
        [Test]
        public void Constructor_WithValidData_CreatesDelivery()
        {
            // Arrange
            const string name = "Q1 Release";
            var date = DateTime.UtcNow.AddDays(30);
            const int portfolioId = 1;

            // Act
            var delivery = new Delivery(name, date, portfolioId);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(delivery.Name, Is.EqualTo(name));
                Assert.That(delivery.Date, Is.EqualTo(date));
                Assert.That(delivery.PortfolioId, Is.EqualTo(portfolioId));
                Assert.That(delivery.Features, Is.Not.Null);
                
                Assert.That(delivery.Features, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void Constructor_WithPastDate_ThrowsArgumentException()
        {
            // Arrange
            const string name = "Past Release";
            var pastDate = DateTime.UtcNow.AddDays(-1);
            const int portfolioId = 1;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Delivery(name, pastDate, portfolioId));
            Assert.That(exception.Message, Is.EqualTo("Delivery date must be in the future"));
        }

        [Test]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            const string emptyName = "";
            var futureDate = DateTime.UtcNow.AddDays(30);
            const int portfolioId = 1;

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
            const int portfolioId = 1;

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
            Assert.That(delivery.Features, Has.Count.EqualTo(1));
            Assert.That(delivery.Features, Does.Contain(feature));
        }

        #region SelectionMode Tests

        [Test]
        public void SelectionMode_DefaultsToManual()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1);
            
            Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
        }

        [Test]
        public void SelectionMode_CanBeSetToRuleBased()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1)
            {
                SelectionMode = DeliverySelectionMode.RuleBased
            };
            
            Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased));
        }

        [Test]
        public void RuleDefinitionJson_DefaultsToNull()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1);
            
            Assert.That(delivery.RuleDefinitionJson, Is.Null);
        }

        [Test]
        public void RuleDefinitionJson_CanBeSet()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1)
            {
                RuleDefinitionJson = "{\"conditions\":[]}"
            };
            
            Assert.That(delivery.RuleDefinitionJson, Is.EqualTo("{\"conditions\":[]}"));
        }

        [Test]
        public void RuleSchemaVersion_DefaultsToNull()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1);
            
            Assert.That(delivery.RuleSchemaVersion, Is.Null);
        }

        [Test]
        public void RuleSchemaVersion_CanBeSet()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1)
            {
                RuleSchemaVersion = 1
            };
            
            Assert.That(delivery.RuleSchemaVersion, Is.EqualTo(1));
        }

        #endregion
    }
}