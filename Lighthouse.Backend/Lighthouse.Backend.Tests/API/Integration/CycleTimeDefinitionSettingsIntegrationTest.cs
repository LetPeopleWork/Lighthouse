using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public class CycleTimeDefinitionSettingsIntegrationTest
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
        public async Task SaveNamedDefinition_SurvivesReload_ReadYourWrites()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.CycleTimeDefinitions = [Definition("Active to Done", "Active", "Done")];

            client.AsTeamAdmin(seededTeamId);
            var putResponse = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await putResponse.Content.ReadAsStringAsync());

            var definitions = await GetCycleTimeDefinitions();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(definitions, Has.Count.EqualTo(1));
                Assert.That(definitions[0].Name, Is.EqualTo("Active to Done"));
                Assert.That(definitions[0].StartState, Is.EqualTo("Active"));
                Assert.That(definitions[0].EndState, Is.EqualTo("Done"));
                Assert.That(definitions[0].IsValid, Is.True);
                Assert.That(definitions[0].Id, Is.GreaterThan(0));
            }
        }

        [Test]
        public async Task SaveNamedDefinition_AppearsInTheScatterplotSelectorSource()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.CycleTimeDefinitions = [Definition("Active to Done", "Active", "Done")];

            client.AsTeamAdmin(seededTeamId);
            var putResponse = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await putResponse.Content.ReadAsStringAsync());

            var savedId = (await GetCycleTimeDefinitions())[0].Id;
            var reloadedId = (await GetCycleTimeDefinitions())[0].Id;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(savedId, Is.GreaterThan(0),
                    "The saved definition exposes a stable id — the source the scatterplot selector binds to.");
                Assert.That(reloadedId, Is.EqualTo(savedId),
                    "The id is stable across reads, so the selector and the cycleTimeData?definitionId read share one SSOT id.");
            }
        }

        [Test]
        public async Task SaveEndStateBeforeStartState_RejectedInline_NothingPersisted()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.CycleTimeDefinitions = [Definition("Backwards", "Done", "Active")];

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            var definitions = await GetCycleTimeDefinitions();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("End state must come after the start state in the workflow"), body);
                Assert.That(definitions, Is.Empty, "A rejected save must persist nothing.");
            }
        }

        [Test]
        public async Task SaveEmptyOrDuplicateName_RejectedInline_NothingPersisted()
        {
            client.AsTeamAdmin(seededTeamId);

            var duplicate = BuildTeamSettingDto();
            duplicate.CycleTimeDefinitions =
            [
                Definition("Same", "Active", "Done"),
                Definition("Same", "New", "Done"),
            ];
            var duplicateResponse = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", duplicate);
            var duplicateBody = await duplicateResponse.Content.ReadAsStringAsync();

            var empty = BuildTeamSettingDto();
            empty.CycleTimeDefinitions = [Definition("   ", "Active", "Done")];
            var emptyResponse = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", empty);

            var definitions = await GetCycleTimeDefinitions();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(duplicateResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), duplicateBody);
                Assert.That(duplicateBody, Does.Contain("Duplicate cycle time name"), duplicateBody);
                Assert.That(emptyResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(definitions, Is.Empty, "Neither rejected save persisted anything.");
            }
        }

        [Test]
        public async Task SaveMappingNameBoundary_ResolvesViaGetRawStatesForCategory()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.StateMappings = [new StateMappingDto { Name = "Validation", States = ["Active"] }];
            teamSetting.CycleTimeDefinitions = [Definition("To Validation", "New", "Validation")];

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

            var definitions = await GetCycleTimeDefinitions();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(definitions, Has.Count.EqualTo(1));
                Assert.That(definitions[0].EndState, Is.EqualTo("Validation"));
                Assert.That(definitions[0].IsValid, Is.True,
                    "A State-Mapping name resolves to its raw states via the EXISTING owner.GetRawStatesForCategory.");
            }
        }

        [Test]
        public async Task SaveDefinition_RequiresTeamAdmin_ViewerIsForbidden()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.CycleTimeDefinitions = [Definition("Active to Done", "Active", "Done")];

            client.AsViewer();
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), body);
        }

        [Test]
        public async Task CycleTimesConfig_IsPremiumGated_NonPremiumWriteRefused()
        {
            client.AsTeamAdmin(seededTeamId);

            var premiumSave = BuildTeamSettingDto();
            premiumSave.CycleTimeDefinitions = [Definition("Active to Done", "Active", "Done")];
            var premiumResponse = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", premiumSave);
            Assert.That(premiumResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await premiumResponse.Content.ReadAsStringAsync());

            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

            var downgradeSave = BuildTeamSettingDto();
            downgradeSave.Name = "Downgraded Team";
            downgradeSave.CycleTimeDefinitions = [];
            var downgradeResponse = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", downgradeSave);

            var definitions = await GetCycleTimeDefinitions();
            var name = await GetSettingsName();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(downgradeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await downgradeResponse.Content.ReadAsStringAsync());
                Assert.That(definitions, Has.Count.EqualTo(1),
                    "A non-premium write must NOT wipe existing definitions (non-destructive downgrade).");
                Assert.That(definitions[0].Name, Is.EqualTo("Active to Done"));
                Assert.That(name, Is.EqualTo("Downgraded Team"), "The rest of the settings round-trip is unaffected.");
            }
        }

        [Test]
        public async Task ConcurrentSettingsEdit_IsRejectedByTheInheritedConcurrencyToken()
        {
            client.AsTeamAdmin(seededTeamId);

            var staleToken = await GetConcurrencyToken();

            var firstSave = BuildTeamSettingDto();
            firstSave.CycleTimeDefinitions = [Definition("Active to Done", "Active", "Done")];
            firstSave.ConcurrencyToken = staleToken;
            var firstResponse = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", firstSave);
            Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await firstResponse.Content.ReadAsStringAsync());

            var secondSave = BuildTeamSettingDto();
            secondSave.CycleTimeDefinitions = [Definition("New to Done", "New", "Done")];
            secondSave.ConcurrencyToken = staleToken;
            var secondResponse = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", secondSave);

            var secondBody = await secondResponse.Content.ReadAsStringAsync();
            var definitions = await GetCycleTimeDefinitions();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), secondBody);
                Assert.That(secondBody, Does.Contain(ConcurrencyConflictCode), secondBody);
                Assert.That(definitions[0].Name, Is.EqualTo("Active to Done"),
                    "The stale second write must not overwrite the first; CycleTimeDefinitions ride the tokened aggregate.");
            }
        }

        private async Task<List<CycleTimeDefinitionDto>> GetCycleTimeDefinitions()
        {
            return (await GetSettings()).CycleTimeDefinitions;
        }

        private async Task<string> GetSettingsName()
        {
            return (await GetSettings()).Name;
        }

        private async Task<Guid> GetConcurrencyToken()
        {
            var token = (await GetSettings()).ConcurrencyToken;
            Assert.That(token, Is.Not.Null, "Settings must expose concurrencyToken so clients can echo it back on save.");
            return token!.Value;
        }

        private async Task<TeamSettingDto> GetSettings()
        {
            var response = await client.GetAsync($"/api/latest/teams/{seededTeamId}/settings");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return JsonSerializer.Deserialize<TeamSettingDto>(body, JsonOptions)!;
        }

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private static CycleTimeDefinitionDto Definition(string name, string startState, string endState)
        {
            return new CycleTimeDefinitionDto
            {
                Name = name,
                StartState = startState,
                EndState = endState,
            };
        }

        private TeamSettingDto BuildTeamSettingDto()
        {
            return new TeamSettingDto
            {
                Id = seededTeamId,
                Name = $"Team {seededTeamId}",
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
