using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Tests.TestHelpers;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class CreateDeliveryIntegrationTest : IntegrationTestBase
    {
        public CreateDeliveryIntegrationTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public async Task CreateDelivery_WithMissingName_ReturnsBadRequest()
        {
            // Arrange
            var portfolioId = 1;
            var request = new CreateDeliveryRequest
            {
                // Missing Name
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [1, 2, 3]
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync($"/api/deliveries/portfolio/{portfolioId}", content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task CreateDelivery_WithInvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var portfolioId = 1;
            // This was the original issue - sending fields as individual parameters instead of JSON object
            var invalidJson = "invalid json structure";
            var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync($"/api/deliveries/portfolio/{portfolioId}", content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}