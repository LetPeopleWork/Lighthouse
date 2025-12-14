using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class ProjectsControllerAuthorizationTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task CreateProject_AsNonPremiumUser_AboveLimit_Returns403()
        {
            await SetupProjects(1);

            var projectSetting = new ProjectSettingDto();
            var response = await Client.PostAsJsonAsync("/api/projects", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task CreateProject_AsNonPremiumUser_AtLimit_DoesNotReturn403()
        {
            await SetupProjects(0);

            var projectSetting = new ProjectSettingDto();
            var response = await Client.PostAsJsonAsync("/api/projects", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateAllProjects_AsNonPremiumUser_Returns403()
        {
            var response = await Client.PostAsync("/api/projects/refresh-all", null);
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateProject_AsNonPremiumUser_AboveLimit_Returns403()
        {
            await SetupProjects(2);

            var projectSetting = new ProjectSettingDto();
            var response = await Client.PutAsJsonAsync("/api/projects/123", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateProject_AsNonPremiumUser_AtLimit_DoesNotReturn403()
        {
            await SetupProjects(1);

            var projectSetting = new ProjectSettingDto();
            var response = await Client.PutAsJsonAsync("/api/projects/123", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateFeaturesForProject_AsNonPremiumUser_AboveLimit_Returns403()
        {
            await SetupProjects(2);

            var response = await Client.PostAsync("/api/projects/refresh/123", null);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateFeaturesForProject_AsNonPremiumUser_AtLimit_DoesNotReturn403()
        {
            await SetupProjects(1);

            var response = await Client.PostAsync("/api/projects/refresh/123", null);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task ValidateProjectSettings_AsNonPremiumUser_AboveLimit_Returns403()
        {
            await SetupProjects(2);

            var projectSetting = new ProjectSettingDto();
            var response = await Client.PostAsJsonAsync("/api/projects/validate", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task ValidateProjectSettings_AsNonPremiumUser_AtLimit_DoesNotReturn403()
        {
            await SetupProjects(1);

            var projectSetting = new ProjectSettingDto();
            var response = await Client.PostAsJsonAsync("/api/projects/validate", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        private async Task SetupProjects(int numberOfProjects)
        {
            var projectRepository = ServiceProvider.GetService<IRepository<Portfolio>>();

            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira
            };

            for (int i = 0; i < numberOfProjects; i++)
            {
                var project = new Portfolio
                {
                    Name = $"Project {i + 1}",
                    WorkTrackingSystemConnection = workTrackingSystemConnection
                };
                projectRepository.Add(project);
            }

            await projectRepository.Save();
        }
    }
}
