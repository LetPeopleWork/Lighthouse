using Lighthouse.Backend.Tests.TestHelpers;
using System.Net;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class TerminologyControllerAuthorizationTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task UpdateTerminology_AsNonPremiumUser_Returns403()
        {
            var terminologyEntry = new TerminologyEntry
            {
                Id = 12,
                Key = "Cheater",
                Description = "Cheater",
                Value = "Tom Brady"
            };
            
            var terminology = new List<TerminologyEntry> { terminologyEntry };
            
            var response = await Client.PutAsJsonAsync("/api/terminology", terminology);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }
    }
}
