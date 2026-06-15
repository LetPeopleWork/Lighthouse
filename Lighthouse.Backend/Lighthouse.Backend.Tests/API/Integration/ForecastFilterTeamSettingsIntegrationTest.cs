using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// DELIVER wave — drives US-01 (configure rule set), US-07 (premium gate + non-destructive
    /// downgrade), cross-cutting invariant #5 (RBAC: PUT requires TeamWrite).
    /// </summary>
    [TestFixture]
    public class ForecastFilterTeamSettingsIntegrationTest
    {
        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int seededTeamId;
        private int seededConnectionId;
        private int seededAdditionalFieldId;

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
        public async Task PutTeam_PremiumTenantTeamAdminWithValidRuleSet_PersistsRuleSetAndReturns200()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.ForecastFilterRuleSetJson = BuildRuleSetJson(("workitem.type", "equals", "Bug"));

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                var persisted = LoadTeamFromDatabase(seededTeamId).ForecastFilterRuleSetJson;
                Assert.That(persisted, Is.Not.Null.And.Contains("workitem.type"), $"Persisted JSON: {persisted}");
            }
        }

        [Test]
        public async Task GetTeam_AfterRuleSetSaved_ReturnsForecastFilterRuleSetJsonInPayload()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.ForecastFilterRuleSetJson = BuildRuleSetJson(("workitem.state", "notequals", "Removed"));

            client.AsTeamAdmin(seededTeamId);
            var putResponse = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await putResponse.Content.ReadAsStringAsync());

            var settingsResponse = await client.GetAsync($"/api/latest/teams/{seededTeamId}/settings");
            var body = await settingsResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(settingsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                using var document = JsonDocument.Parse(body);
                Assert.That(document.RootElement.TryGetProperty("forecastFilterRuleSetJson", out var jsonProp), Is.True, body);
                Assert.That(jsonProp.GetString(), Does.Contain("workitem.state"));
            }
        }

        [Test]
        public async Task GetForecastFilterSchema_PremiumTenantTeamReader_ReturnsWorkItemFieldSchema()
        {
            client.AsTeamAdmin(seededTeamId);

            var response = await client.GetAsync($"/api/latest/teams/{seededTeamId}/forecast-filter/schema");

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                using var document = JsonDocument.Parse(body);
                var fieldKeys = document.RootElement.GetProperty("fields")
                    .EnumerateArray()
                    .Select(e => e.GetProperty("fieldKey").GetString())
                    .ToList();
                Assert.That(fieldKeys, Does.Contain("workitem.type"));
                Assert.That(fieldKeys, Does.Contain("workitem.state"));
                Assert.That(fieldKeys, Does.Contain("workitem.name"));
                Assert.That(fieldKeys, Does.Contain("workitem.referenceid"));
                Assert.That(fieldKeys, Does.Contain("workitem.parentreferenceid"));
                Assert.That(fieldKeys, Does.Contain("workitem.tags"));
                Assert.That(fieldKeys, Does.Contain($"additionalField.{seededAdditionalFieldId}"));
                Assert.That(document.RootElement.GetProperty("maxRules").GetInt32(), Is.EqualTo(WorkItemRuleSet.MaxRules));
                Assert.That(document.RootElement.GetProperty("maxValueLength").GetInt32(), Is.EqualTo(WorkItemRuleSet.MaxValueLength));
            }
        }

        [Test]
        public async Task PutTeam_PremiumTenantNonTeamAdminWithRuleSet_Returns403()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.ForecastFilterRuleSetJson = BuildRuleSetJson(("workitem.type", "equals", "Bug"));

            client.AsViewer();
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), body);
        }

        [Test]
        public async Task PutTeam_PremiumTenantUnknownFieldKey_Returns400WithErrorMessage()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.ForecastFilterRuleSetJson = BuildRuleSetJson(("workitem.bogus", "equals", "Bug"));

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
                Assert.That(body, Does.Contain("Forecast filter").IgnoreCase, body);
            }
        }

        [Test]
        public async Task PutTeam_PremiumTenantRuleSetExceedingMaxConditions_Returns400()
        {
            var teamSetting = BuildTeamSettingDto();
            var tooMany = Enumerable.Range(0, WorkItemRuleSet.MaxRules + 1)
                .Select(_ => ("workitem.type", "equals", "Bug"))
                .ToArray();
            teamSetting.ForecastFilterRuleSetJson = BuildRuleSetJson(tooMany);

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
        }

        [Test]
        public async Task PutTeam_PremiumTenantRuleValueExceedingMaxLength_Returns400()
        {
            var teamSetting = BuildTeamSettingDto();
            var oversizeValue = new string('x', WorkItemRuleSet.MaxValueLength + 1);
            teamSetting.ForecastFilterRuleSetJson = BuildRuleSetJson(("workitem.name", "contains", oversizeValue));

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
        }

        [Test]
        public async Task PutTeam_PremiumTenantZeroConditions_PersistsAsClearedFilter()
        {
            var teamSetting = BuildTeamSettingDto();
            teamSetting.ForecastFilterRuleSetJson = """{"Version":1,"Conditions":[]}""";

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                var persisted = LoadTeamFromDatabase(seededTeamId).ForecastFilterRuleSetJson;
                Assert.That(persisted, Is.Not.Null);
                var ruleSet = JsonSerializer.Deserialize<WorkItemRuleSet>(persisted!);
                Assert.That(ruleSet, Is.Not.Null);
                Assert.That(ruleSet!.Conditions, Is.Empty);
            }
        }

        [Test]
        public async Task PutTeam_NonPremiumTenantWithRuleSet_PersistsRuleSetForLaterReUpgrade()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

            var teamSetting = BuildTeamSettingDto();
            teamSetting.ForecastFilterRuleSetJson = BuildRuleSetJson(("workitem.tags", "contains", "maintenance"));

            client.AsTeamAdmin(seededTeamId);
            var response = await client.PutAsJsonAsync($"/api/latest/teams/{seededTeamId}", teamSetting);

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                var persisted = LoadTeamFromDatabase(seededTeamId).ForecastFilterRuleSetJson;
                Assert.That(persisted, Is.Not.Null.And.Contains("maintenance"));
            }
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
            var additionalField = new AdditionalFieldDefinition
            {
                Reference = "Priority",
                DisplayName = "Priority",
            };
            connection.AdditionalFieldDefinitions.Add(additionalField);

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
            seededAdditionalFieldId = additionalField.Id;
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

        private static string BuildRuleSetJson(params (string fieldKey, string @operator, string value)[] conditions)
        {
            var ruleSet = new WorkItemRuleSet
            {
                Version = WorkItemRuleSet.SchemaVersion,
                Conditions = conditions
                    .Select(c => new WorkItemRuleCondition
                    {
                        FieldKey = c.fieldKey,
                        Operator = c.@operator,
                        Value = c.value,
                    })
                    .ToList(),
            };
            return JsonSerializer.Serialize(ruleSet);
        }
    }
}
