using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Tests.TestHelpers;
using System.Net;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class ConfigurationControllerAuthorizationTests()
        : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task ExportConfiguration_AsNonPremiumUser_Returns403()
        {
            var response = await Client.GetAsync("/api/configuration/export");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task ValidateConfiguration_AsNonPremiumUser_Returns403()
        {
            var payload = new ConfigurationExport
            {
                WorkTrackingSystems = new List<WorkTrackingSystemConnectionDto>(),
                Teams = new List<TeamSettingDto>(),
                Projects = new List<ProjectSettingDto>(),
            };

            var response = await Client.PostAsJsonAsync("/api/configuration/validate", payload);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task DeleteConfiguration_AsNonPremiumUser_Returns403()
        {
            var response = await Client.DeleteAsync("/api/configuration/clear");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }
    }
}
