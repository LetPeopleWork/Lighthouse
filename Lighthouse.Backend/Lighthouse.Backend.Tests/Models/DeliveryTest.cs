using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Models
{
    public class DeliveryTest
    {
        private static readonly BlackoutPeriod[] NoBlackoutPeriods = [];

        /// <summary>
        /// Bug #5567: a fixed instant on a UTC instance. The expectations below are unchanged from
        /// before the anchor moved - the point is that they no longer RE-DERIVE the production
        /// expression, which is root cause D.
        /// </summary>
        private static readonly FakeLighthouseClock Clock =
            new(new DateTimeOffset(2026, 7, 27, 10, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);

        [Test]
        public void Constructor_WithValidData_CreatesDelivery()
        {
            // Arrange
            const string name = "Q1 Release";
            var date = DateTime.UtcNow.AddDays(30);
            const int portfolioId = 1;

            // Act
            var delivery = new Delivery(name, date, portfolioId, TestToday.Ambient);

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

        /// <summary>
        /// A Delivery that follows a Release elsewhere takes whatever date the Release carries, and a
        /// Release that shipped last month is a perfectly ordinary thing to point one at. Refusing a
        /// past date is a rule about what somebody may type in, not about what a Delivery can be, so
        /// it is asked at the API and no longer here.
        /// </summary>
        [Test]
        public void Constructor_WithPastDate_CreatesDelivery()
        {
            var pastDate = DateTime.UtcNow.AddDays(-1);

            var delivery = new Delivery("Past Release", pastDate, 1, TestToday.Ambient);

            Assert.That(delivery.Date, Is.EqualTo(pastDate));
        }

        [Test]
        public void Constructor_WithTheInstanceNextDay_CreatesDelivery()
        {
            var tomorrow = Clock.TodayAsUtcMidnight.AddDays(1);

            var delivery = new Delivery("Q1 Release", tomorrow, 1, Clock.Today);

            Assert.That(delivery.Date, Is.EqualTo(tomorrow));
        }

        [Test]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            const string emptyName = "";
            var futureDate = DateTime.UtcNow.AddDays(30);
            const int portfolioId = 1;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new Delivery(emptyName, futureDate, portfolioId, TestToday.Ambient));
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
            var exception = Assert.Throws<ArgumentException>(() => new Delivery(nullName, futureDate, portfolioId, TestToday.Ambient));
            Assert.That(exception.Message, Is.EqualTo("Name cannot be null or empty"));
        }

        [Test]
        public void ReplaceFeatures_ValidFeature_AddsToCollection()
        {
            // Arrange
            var delivery = new Delivery("Test Delivery", DateTime.UtcNow.AddDays(30), 1, TestToday.Ambient);
            var feature = new Feature();

            // Act
            delivery.ReplaceFeatures([feature]);

            // Assert
            Assert.That(delivery.Features, Has.Count.EqualTo(1));
            Assert.That(delivery.Features, Does.Contain(feature));
        }

        #region SelectionMode Tests

        [Test]
        public void SelectionMode_DefaultsToManual()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1, TestToday.Ambient);
            
            Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
        }

        [Test]
        public void SelectionMode_CanBeSetToRuleBased()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1, TestToday.Ambient)
            {
                SelectionMode = DeliverySelectionMode.RuleBased
            };
            
            Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased));
        }

        [Test]
        public void RuleDefinitionJson_DefaultsToNull()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1, TestToday.Ambient);
            
            Assert.That(delivery.RuleDefinitionJson, Is.Null);
        }

        [Test]
        public void RuleDefinitionJson_CanBeSet()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1, TestToday.Ambient)
            {
                RuleDefinitionJson = "{\"conditions\":[]}"
            };
            
            Assert.That(delivery.RuleDefinitionJson, Is.EqualTo("{\"conditions\":[]}"));
        }

        [Test]
        public void RuleSchemaVersion_DefaultsToNull()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1, TestToday.Ambient);
            
            Assert.That(delivery.RuleSchemaVersion, Is.Null);
        }

        [Test]
        public void RuleSchemaVersion_CanBeSet()
        {
            var delivery = new Delivery("Test", DateTime.UtcNow.AddDays(30), 1, TestToday.Ambient)
            {
                RuleSchemaVersion = 1
            };
            
            Assert.That(delivery.RuleSchemaVersion, Is.EqualTo(1));
        }

        #endregion
    }
}