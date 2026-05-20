using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class RbacExceptionEndpointsAuthorizationTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task GetSystemInfo_AsNonPremiumUser_DoesNotReturn403()
        {
            var response = await Client.GetAsync("/api/latest/systeminfo");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task GetUpdateStatus_AsNonPremiumUser_DoesNotReturn403()
        {
            var response = await Client.GetAsync("/api/latest/update/status");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task GetLicenseStatus_AsNonPremiumUser_DoesNotReturn403()
        {
            var response = await Client.GetAsync("/api/latest/license");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task GetAllTerminology_AsNonPremiumUser_DoesNotReturn403()
        {
            var response = await Client.GetAsync("/api/latest/terminology/all");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task GetTeamSuggestions_AsNonPremiumUser_DoesNotReturn403()
        {
            var response = await Client.GetAsync("/api/latest/suggestions/workitemtypes/teams");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task GetFeatureDetailsById_AsNonPremiumUser_DoesNotReturn403()
        {
            var feature = await SeedFeatureAsync();

            var response = await Client.GetAsync($"/api/latest/features/ids?featureIds={feature.Id}");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task ClearLicense_AsNonPremiumUser_DoesNotReturn403()
        {
            var response = await Client.DeleteAsync("/api/latest/license");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task ImportLicense_AsNonPremiumUser_DoesNotReturn403()
        {
            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("{}"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Add(fileContent, "file", "license.json");

            var response = await Client.PostAsync("/api/latest/license/import", content);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task GetBoards_AsNonPremiumUser_DoesNotReturn403()
        {
            var response = await Client.GetAsync("/api/latest/wizards/123/boards");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        private async Task<Feature> SeedFeatureAsync()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira
            };
            var team = new Team
            {
                Name = "Team",
                WorkTrackingSystemConnection = connection
            };
            var feature = new Feature(team, 3)
            {
                Name = "Feature",
                ReferenceId = "FTR-1",
                Order = "1"
            };

            DatabaseContext.Features.Add(feature);
            await DatabaseContext.SaveChangesAsync();

            return feature;
        }
    }
}