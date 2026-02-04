using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Tests.TestHelpers;
using System.Net;
using System.Text;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.Services.Implementation.Licensing;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class ConfigurationControllerIntegrationTests()
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
                WorkTrackingSystems = [],
                Teams = [],
                Projects = [],
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

        [Test]
        public async Task ValidateConfiguration_OldConfigurationFile_CanMigrateToLatestVersion()
        {
            var licenseService = ServiceProvider.GetService<ILicenseService>();
            await licenseService.ImportLicense(TestLicenseData.ValidLicense);
            
            // Read the JSON file from TestData folder
            var jsonFilePath = Path.Combine("API", "Integration", "TestData", "OldConfiguration.json");
            var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
    
            // Create StringContent with the JSON
            var payload = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync("/api/configuration/validate", payload);
                
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}
