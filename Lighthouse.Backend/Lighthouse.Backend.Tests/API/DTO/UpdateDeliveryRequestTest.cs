using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class UpdateDeliveryRequestTest
    {
        [Test]
        public void CreateDeliveryRequest_WithValidData_CreatesSuccessfully()
        {
            // Arrange
            var name = "Q1 Release";
            var date = DateTime.UtcNow.AddDays(30);
            var featureIds = new List<int> { 1, 2, 3 };

            // Act
            var request = new UpdateDeliveryRequest
            {
                Name = name,
                Date = date,
                FeatureIds = featureIds
            };

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(request.Name, Is.EqualTo(name));
                Assert.That(request.Date, Is.EqualTo(date));
                Assert.That(request.FeatureIds, Is.EqualTo(featureIds));
            }
        }

        [Test]
        public void CreateDeliveryRequest_Properties_AreInitialized()
        {
            // Arrange & Act
            var request = new UpdateDeliveryRequest();

            // Assert - FeatureIds should be initialized as empty list
            using (Assert.EnterMultipleScope())
            {
                Assert.That(request.FeatureIds, Is.Not.Null);
                Assert.That(request.FeatureIds, Has.Count.EqualTo(0));
            }
        }

        #region SelectionMode Tests

        [Test]
        public void SelectionMode_DefaultsToManual()
        {
            var request = new UpdateDeliveryRequest();
            
            Assert.That(request.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
        }

        [Test]
        public void SelectionMode_CanBeSetToRuleBased()
        {
            var request = new UpdateDeliveryRequest
            {
                SelectionMode = DeliverySelectionMode.RuleBased
            };
            
            Assert.That(request.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased));
        }

        [Test]
        public void Rules_DefaultsToNull()
        {
            var request = new UpdateDeliveryRequest();
            
            Assert.That(request.Rules, Is.Null);
        }

        [Test]
        public void Rules_CanBeSet()
        {
            var request = new UpdateDeliveryRequest
            {
                Rules = 
                [
                    new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }
                ]
            };
            
            Assert.That(request.Rules, Has.Count.EqualTo(1));
        }

        #endregion
    }
}