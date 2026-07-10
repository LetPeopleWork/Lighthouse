using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models
{
    public class DeliveryTest
    {
        private static readonly BlackoutPeriod[] NoBlackoutPeriods = [];

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

        [Test]
        public void CalculateMetrics_MultipleFeaturesTiedOnLikelihood_WhenDistributionReflectsLatestCompletingFeature()
        {
            // Regression for ADO #5435: for large deliveries the forecast dates were taken from
            // an arbitrary feature. Selection ranked features by likelihood-for-target-date, which
            // saturates to 100% for every feature once the target date is comfortably far out, so
            // the tie-break fell back to collection order instead of the latest-completing feature.
            // A delivery is only done when its LATEST feature finishes.
            var deliveryDate = DateTime.UtcNow.AddDays(200); // comfortably far -> every feature is 100% likely

            var lateFeature = new Feature { Id = 1 };
            lateFeature.Forecasts.Add(CreateForecastCompletingInDays(50));
            lateFeature.FeatureWork.Add(new FeatureWork { RemainingWorkItems = 5 });

            var earlyFeature = new Feature { Id = 2 };
            earlyFeature.Forecasts.Add(CreateForecastCompletingInDays(5));
            earlyFeature.FeatureWork.Add(new FeatureWork { RemainingWorkItems = 5 });

            var delivery = new Delivery { Id = 1, Name = "Large Delivery", Date = deliveryDate };
            // Late feature added first: the buggy selection returns the last-ranked (early) feature on a likelihood tie.
            delivery.Features.Add(lateFeature);
            delivery.Features.Add(earlyFeature);

            var metrics = delivery.CalculateMetrics(NoBlackoutPeriods, 70, 85, 95);

            var expectedLatestDate = DateTime.UtcNow.Date.AddDays(50);
            using (Assert.EnterMultipleScope())
            {
                foreach (var percentile in metrics.WhenDistribution)
                {
                    Assert.That(percentile.ExpectedDate, Is.EqualTo(expectedLatestDate),
                        $"Percentile {percentile.Percentile} must reflect the latest-completing feature");
                }
            }
        }

        private static WhenForecast CreateForecastCompletingInDays(int days)
        {
            var simulationResult = new Dictionary<int, int> { { days, 100 } };
            var forecast = new WhenForecast();
            forecast.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast, [simulationResult]);
            return forecast;
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