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
            await SetupPortfolios(1);

            var projectSetting = new PortfolioSettingDto();
            var response = await Client.PostAsJsonAsync("/api/portfolios", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task CreateProject_AsNonPremiumUser_AtLimit_DoesNotReturn403()
        {
            await SetupPortfolios(0);

            var projectSetting = new PortfolioSettingDto();
            var response = await Client.PostAsJsonAsync("/api/portfolios", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateAllProjects_AsNonPremiumUser_Returns403()
        {
            var response = await Client.PostAsync("/api/portfolios/refresh-all", null);
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateProject_AsNonPremiumUser_AboveLimit_Returns403()
        {
            await SetupPortfolios(2);

            var projectSetting = new PortfolioSettingDto();
            var response = await Client.PutAsJsonAsync("/api/portfolios/123", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateProject_AsNonPremiumUser_AtLimit_DoesNotReturn403()
        {
            await SetupPortfolios(1);

            var projectSetting = new PortfolioSettingDto();
            var response = await Client.PutAsJsonAsync("/api/portfolios/123", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateFeaturesForProject_AsNonPremiumUser_AboveLimit_Returns403()
        {
            await SetupPortfolios(2);

            var response = await Client.PostAsync("/api/portfolios/123/refresh", null);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateFeaturesForProject_AsNonPremiumUser_AtLimit_DoesNotReturn403()
        {
            await SetupPortfolios(1);

            var response = await Client.PostAsync("/api/portfolios/123/refresh", null);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task ValidateProjectSettings_AsNonPremiumUser_AboveLimit_Returns403()
        {
            await SetupPortfolios(2);

            var projectSetting = new PortfolioSettingDto();
            var response = await Client.PostAsJsonAsync("/api/portfolios/validate", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task ValidateProjectSettings_AsNonPremiumUser_AtLimit_DoesNotReturn403()
        {
            await SetupPortfolios(1);

            var projectSetting = new PortfolioSettingDto();
            var response = await Client.PostAsJsonAsync("/api/portfolios/validate", projectSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        private async Task SetupPortfolios(int numberOfPortfolios)
        {
            var portfolioRepo = ServiceProvider.GetService<IRepository<Portfolio>>();

            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira
            };

            for (var i = 0; i < numberOfPortfolios; i++)
            {
                var portfolio = new Portfolio
                {
                    Name = $"Project {i + 1}",
                    WorkTrackingSystemConnection = workTrackingSystemConnection
                };
                portfolioRepo.Add(portfolio);
            }

            await portfolioRepo.Save();
        }
    }
}
