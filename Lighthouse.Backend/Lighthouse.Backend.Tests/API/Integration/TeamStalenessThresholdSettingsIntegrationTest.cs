using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [NonParallelizable]
    public class TeamStalenessThresholdSettingsIntegrationTest
    {
        private const int DefaultTeamThresholdDays = 7;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int seededTeamId;
        private int seededConnectionId;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });

            client = factory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SeedTeam();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task GetTeamSettings_NewlyCreatedTeam_ExposesDefaultStalenessThresholdOfSevenDays()
        {
            client.AsTeamAdmin(seededTeamId);

            var response = await client.GetAsync($"/api/latest/teams/{seededTeamId}/settings");

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(TryGetInt(body, "stalenessThresholdDays", out var threshold), Is.True,
                    $"Team settings payload must carry stalenessThresholdDays for US-03. Body: {body}");
                Assert.That(threshold, Is.EqualTo(DefaultTeamThresholdDays),
                    $"A newly-created team must default to {DefaultTeamThresholdDays} days. Body: {body}");
            }
        }

        [Test]
        public async Task PutTeam_TeamAdminSetsThresholdToFourteen_SettingsRoundTripsTheNewThreshold()
        {
            client.AsTeamAdmin(seededTeamId);

            var putResponse = await PutTeamWithThreshold(seededTeamId, 14);
            var putBody = await putResponse.Content.ReadAsStringAsync();
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), putBody);

            var settingsResponse = await client.GetAsync($"/api/latest/teams/{seededTeamId}/settings");
            var settingsBody = await settingsResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(settingsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), settingsBody);
                Assert.That(TryGetInt(settingsBody, "stalenessThresholdDays", out var threshold), Is.True, settingsBody);
                Assert.That(threshold, Is.EqualTo(14),
                    $"Saved staleness threshold must round-trip through the settings payload. Body: {settingsBody}");
            }
        }

        [Test]
        public async Task PutTeam_TeamAdminSetsThresholdToZero_PersistsZeroToDisableHighlighting()
        {
            client.AsTeamAdmin(seededTeamId);

            var putResponse = await PutTeamWithThreshold(seededTeamId, 0);
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await putResponse.Content.ReadAsStringAsync());

            var settingsBody = await (await client.GetAsync($"/api/latest/teams/{seededTeamId}/settings")).Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TryGetInt(settingsBody, "stalenessThresholdDays", out var threshold), Is.True, settingsBody);
                Assert.That(threshold, Is.EqualTo(0), settingsBody);
            }
        }

        [Test]
        public async Task PutTeam_ThresholdBelowRange_ReturnsBadRequest()
        {
            client.AsTeamAdmin(seededTeamId);

            var response = await PutTeamWithThreshold(seededTeamId, -1);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
        }

        [Test]
        public async Task PutTeam_ThresholdAboveRange_ReturnsBadRequest()
        {
            client.AsTeamAdmin(seededTeamId);

            var response = await PutTeamWithThreshold(seededTeamId, 366);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
        }

        [Test]
        public async Task PutTeam_NonTeamAdminSetsThreshold_ReturnsForbidden()
        {
            client.AsViewer();

            var response = await PutTeamWithThreshold(seededTeamId, 14);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), body);
        }

        private async Task<HttpResponseMessage> PutTeamWithThreshold(int teamId, int threshold)
        {
            var payload = BuildTeamSettingJson(teamId);
            payload["stalenessThresholdDays"] = threshold;

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/teams/{teamId}", content);
        }

        private JsonObject BuildTeamSettingJson(int teamId)
        {
            var dto = new TeamSettingDto
            {
                Id = teamId,
                Name = $"Team {teamId}",
                DataRetrievalValue = "project = TEST",
                WorkTrackingSystemConnectionId = seededConnectionId,
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Done"],
                ThroughputHistory = 30,
                UseFixedDatesForThroughput = false,
                FeatureWIP = 1,
                AutomaticallyAdjustFeatureWIP = false,
                DoneItemsCutoffDays = 365,
                StateMappings = [],
            };

            var serialized = JsonSerializer.Serialize(dto);
            return JsonNode.Parse(serialized)!.AsObject();
        }

        private void SeedTeam()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            seededTeamId = team.Id;
            seededConnectionId = connection.Id;
        }

        private static bool TryGetInt(string body, string propertyName, out int value)
        {
            value = default;
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Number)
            {
                return false;
            }

            return prop.TryGetInt32(out value);
        }
    }
}
