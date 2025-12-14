using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class TeamsControllerAuthorizationTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task CreateTeam_AsNonPremiumUser_Returns403()
        {
            await SetupTeams(3);

            var teamSetting = new TeamSettingDto();

            var response = await Client.PostAsJsonAsync("/api/teams", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task CreateTeam_AsNonPremiumUser_BelowTeamLimit_DoesNotReturn403()
        {
            await SetupTeams(2);

            var teamSetting = new TeamSettingDto();

            var response = await Client.PostAsJsonAsync("/api/teams", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateAllProjects_AsNonPremiumUser_Returns403()
        {
            var response = await Client.PostAsync("/api/teams/update-all", null);
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateTeam_AsNonPremiumUser_Returns403()
        {
            await SetupTeams(4);
            var teamSetting = new TeamSettingDto();

            var response = await Client.PutAsJsonAsync("/api/teams/123", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateTeam_AsNonPremiumUser_BelowTeamLimit_DoesNotReturn403()
        {
            await SetupTeams(3);
            var teamSetting = new TeamSettingDto();

            var response = await Client.PutAsJsonAsync("/api/teams/123", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateTeamData_AsNonPremiumUser_Returns403()
        {
            await SetupTeams(4);
            var response = await Client.PostAsync("/api/teams/123", null);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task UpdateTeamData_AsNonPremiumUser_BelowTeamLimit_DoesNotReturn403()
        {
            await SetupTeams(3);
            var response = await Client.PostAsync("/api/teams/123", null);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task ValidateTeamSettings_AsNonPremiumUser_Returns403()
        {
            await SetupTeams(4);
            var teamSetting = new TeamSettingDto();

            var response = await Client.PostAsJsonAsync("/api/teams/validate", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        [Test]
        public async Task ValidateTeamSettings_AsNonPremiumUser_BelowTeamLimit_DoesNotReturn403()
        {
            await SetupTeams(3);
            var teamSetting = new TeamSettingDto();

            var response = await Client.PostAsJsonAsync("/api/teams/validate", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden), $"Response body: {body}");
        }

        private async Task SetupTeams(int numberOfTeams)
        {
            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();

            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };

            for (int i = 0; i < numberOfTeams; i++) // Exceeds limit of 3
            {
                var team = new Team { Name = $"Team {i + 1}", WorkTrackingSystemConnection = workTrackingSystemConnection };
                teamRepository.Add(team);
            }

            await teamRepository.Save();
        }
    }
}
