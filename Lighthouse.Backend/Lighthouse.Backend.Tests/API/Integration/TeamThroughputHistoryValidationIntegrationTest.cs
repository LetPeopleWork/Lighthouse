using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class TeamThroughputHistoryValidationIntegrationTest
    {
        private const string ThroughputHistoryError = "Throughput history must be at least 1 day.";
        private const int StoredThroughputHistory = 45;
        private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private int seededTeamId;
        private int seededConnectionId;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var licenseServiceMock = new Mock<ILicenseService>();
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

        [TestCase(0)]
        [TestCase(-5)]
        public async Task PostTeam_ThroughputHistoryNotPositiveWithoutFixedDates_Returns400AndCreatesNoTeam(int throughputHistory)
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.ThroughputHistory = throughputHistory;

            client.AsSystemAdmin();
            var response = await client.PostAsJsonAsync("/api/latest/teams", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain(ThroughputHistoryError));
                Assert.That(CountTeams(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task PutTeam_ThroughputHistoryZeroWithoutFixedDates_Returns400AndKeepsStoredValue()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.Id = seededTeamId;
            teamSetting.ThroughputHistory = 0;

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain(ThroughputHistoryError));
                Assert.That(LoadTeamFromDatabase(seededTeamId).ThroughputHistory, Is.EqualTo(StoredThroughputHistory));
            }
        }

        [Test]
        public async Task PutTeam_StoredThroughputHistoryZero_SavesAnotherSettingAndKeepsZero()
        {
            StoreThroughputHistoryDirectly(seededTeamId, 0);

            var teamSetting = BuildTeamSettingDto();
            teamSetting.Id = seededTeamId;
            teamSetting.ThroughputHistory = 0;
            teamSetting.SystemWIPLimit = 7;

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            var storedTeam = LoadTeamFromDatabase(seededTeamId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(storedTeam.SystemWIPLimit, Is.EqualTo(7));
                Assert.That(storedTeam.ThroughputHistory, Is.Zero);
            }
        }

        [Test]
        public async Task PutTeam_StoredThroughputHistoryZero_NegativeWithoutFixedDates_Returns400AndKeepsZero()
        {
            StoreThroughputHistoryDirectly(seededTeamId, 0);

            var teamSetting = BuildTeamSettingDto();
            teamSetting.Id = seededTeamId;
            teamSetting.ThroughputHistory = -3;

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain(ThroughputHistoryError));
                Assert.That(LoadTeamFromDatabase(seededTeamId).ThroughputHistory, Is.Zero);
            }
        }

        [Test]
        public async Task PutTeam_ThroughputHistoryZeroWithFixedDates_IsAccepted()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.Id = seededTeamId;
            teamSetting.ThroughputHistory = 0;
            teamSetting.UseFixedDatesForThroughput = true;
            teamSetting.ThroughputHistoryStartDate = DateTime.UtcNow.Date.AddDays(-60);
            teamSetting.ThroughputHistoryEndDate = DateTime.UtcNow.Date.AddDays(-1);

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(LoadTeamFromDatabase(seededTeamId).UseFixedDatesForThroughput, Is.True);
            }
        }

        [Test]
        public async Task PostTeam_ThroughputHistoryOmitted_Returns400AndCreatesNoTeam()
        {
            var payload = JsonSerializer.SerializeToNode(BuildTeamSettingDto(), WebJsonOptions)!.AsObject();
            payload.Remove("throughputHistory");

            client.AsSystemAdmin();
            using var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/latest/teams", content);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("throughputHistory"));
                Assert.That(CountTeams(), Is.EqualTo(1));
            }
        }

        private void SeedTeam()
        {
            using var scope = factory.Services.CreateScope();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                ThroughputHistory = StoredThroughputHistory,
            };

            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            seededTeamId = team.Id;
            seededConnectionId = connection.Id;
        }

        private void StoreThroughputHistoryDirectly(int teamId, int throughputHistory)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            var team = dbContext.Teams.Single(t => t.Id == teamId);
            team.ThroughputHistory = throughputHistory;
            dbContext.SaveChanges();
        }

        private int CountTeams()
        {
            using var scope = factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetAll().Count();
        }

        private Team LoadTeamFromDatabase(int teamId)
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            return repository.GetById(teamId)
                ?? throw new InvalidOperationException($"Team {teamId} not found");
        }

        private TeamSettingDto BuildTeamSettingDto()
        {
            return new TeamSettingDto
            {
                Name = "Throughput Team",
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
        }
    }
}
