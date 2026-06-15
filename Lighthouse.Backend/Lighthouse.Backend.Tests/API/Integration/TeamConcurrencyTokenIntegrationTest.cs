using System.Net;
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
    public class TeamConcurrencyTokenIntegrationTest
    {
        private const string ConcurrencyConflictCode = "concurrency-conflict";

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
        public async Task TwoStaleWrites_SecondWriterCarriesStaleToken_Returns409_NoWrite_FirstWriterValuePreserved()
        {
            client.AsTeamAdmin(seededTeamId);

            var initialSettings = await GetTeamSettings();
            var staleToken = GetToken(initialSettings);

            var adminAResponse = await PutTeamWithNameAndToken(seededTeamId, "Renamed By Admin A", staleToken);
            Assert.That(adminAResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await adminAResponse.Content.ReadAsStringAsync());

            var adminBResponse = await PutTeamWithNameAndToken(seededTeamId, "Renamed By Admin B", staleToken);
            var adminBBody = await adminBResponse.Content.ReadAsStringAsync();

            var settingsAfterConflict = await GetTeamSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(adminBResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), adminBBody);
                Assert.That(adminBBody, Does.Contain(ConcurrencyConflictCode),
                    $"409 body must carry the distinguishable concurrency code so clients can render a reload affordance. Body: {adminBBody}");
                Assert.That(GetString(settingsAfterConflict, "name"), Is.EqualTo("Renamed By Admin A"),
                    "Admin A's write must be preserved; Admin B's stale write must not overwrite it.");
            }
        }

        [Test]
        public async Task ReadYourWrites_AfterSuccessfulPut_SettingsReturnsNewValueAndAdvancedToken()
        {
            client.AsTeamAdmin(seededTeamId);

            var initialSettings = await GetTeamSettings();
            var originalToken = GetToken(initialSettings);

            var putResponse = await PutTeamWithNameAndToken(seededTeamId, "Read Your Writes Team", originalToken);
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await putResponse.Content.ReadAsStringAsync());

            var reloadedSettings = await GetTeamSettings();
            var advancedToken = GetToken(reloadedSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetString(reloadedSettings, "name"), Is.EqualTo("Read Your Writes Team"));
                Assert.That(advancedToken, Is.Not.EqualTo(originalToken),
                    "A successful save must advance the concurrency token so the next reader sees a fresh token.");
                Assert.That(advancedToken, Is.Not.EqualTo(Guid.Empty));
            }
        }

        [Test]
        public async Task SingleAdminHappyPath_PutWithJustReadToken_Returns200_NoFalseConflict()
        {
            client.AsTeamAdmin(seededTeamId);

            var settings = await GetTeamSettings();
            var token = GetToken(settings);

            var putResponse = await PutTeamWithNameAndToken(seededTeamId, "Lone Editor Team", token);

            var body = await putResponse.Content.ReadAsStringAsync();
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"A lone editor saving with the token they just read must not see a false 409. Body: {body}");
        }

        private async Task<JsonObject> GetTeamSettings()
        {
            var response = await client.GetAsync($"/api/latest/teams/{seededTeamId}/settings");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return JsonNode.Parse(body)!.AsObject();
        }

        private async Task<HttpResponseMessage> PutTeamWithNameAndToken(int teamId, string name, Guid token)
        {
            var payload = BuildTeamSettingJson(teamId);
            payload["name"] = name;
            payload["concurrencyToken"] = token.ToString();

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/teams/{teamId}", content);
        }

        private static Guid GetToken(JsonObject settings)
        {
            var tokenNode = settings["concurrencyToken"];
            Assert.That(tokenNode, Is.Not.Null,
                "Team settings payload must expose concurrencyToken so clients can echo it back on save.");
            return Guid.Parse(tokenNode!.GetValue<string>());
        }

        private static string GetString(JsonObject settings, string propertyName)
        {
            return settings[propertyName]!.GetValue<string>();
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
    }
}
