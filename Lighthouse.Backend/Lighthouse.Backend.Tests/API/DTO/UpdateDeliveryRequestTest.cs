using Lighthouse.Backend.API.DTO;

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
                Assert.That(request.FeatureIds.Count, Is.EqualTo(0));
            }
        }
    }
}