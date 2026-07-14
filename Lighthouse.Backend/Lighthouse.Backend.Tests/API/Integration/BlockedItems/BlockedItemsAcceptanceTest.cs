using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL (Epic 5074 — Blocked Items). Shared production-composition harness for the blocked-items
    /// acceptance suite. This is the single source of truth (Mandate-12) for HOW scenarios reach the
    /// system: through the real ASP.NET host (Pillar 3 — <see cref="WebApplicationFactory{TEntryPoint}"/>),
    /// via the team settings driving port and the team metrics read port, over real EF. Only the license
    /// port (external/non-deterministic) is faked. Per-slice fixtures inherit these step-support methods
    /// and add their own business-language Given/When/Then steps.
    /// </summary>
    public abstract class BlockedItemsAcceptanceTest
    {
        private static int testDateOffset;

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;
        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected DateTime SyncDay;

        [SetUp]
        public void Init()
        {
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 90;
            SyncDay = new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);

            RootFactory = new TestWebApplicationFactory<Program>();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(RootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => LicenseServiceMock.Object);
                    });
                });

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            // Match production startup seeding (and the TimeInState metrics-read precedent): the metrics
            // read ports resolve TeamMetricsService, whose constructor reads the TeamDataRefresh AppSettings.
            // EnsureCreated does not run the runtime seeders, so seed them explicitly here.
            foreach (var seeder in setupScope.ServiceProvider.GetServices<Lighthouse.Backend.Services.Interfaces.Seeding.ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Seeding (preconditions only — never the expected output; see Critical Rule 7 No Fixture Theater) ---

        protected SeededTeam SeedTeam(
            List<string>? blockedStates = null,
            bool withFlaggedAdditionalField = false)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var flaggedFieldId = 0;
            if (withFlaggedAdditionalField)
            {
                var flaggedField = new AdditionalFieldDefinition
                {
                    Reference = "customfield_10001",
                    DisplayName = "Flagged",
                };
                connection.AdditionalFieldDefinitions.Add(flaggedField);
            }

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 0,
                BlockedRuleSetJson = blockedStates is { Count: > 0 } ? BlockedByStatesRuleSetJson(blockedStates) : null,
                // Align work-item-related settings with BuildTeamSettings so a later settings-save PUT is
                // NOT detected as a work-item-related change (which would RemoveWorkItemsForTeam and wipe the
                // seeded item before the WIP read). See TeamExtensions.WorkItemRelatedSettingsChanged.
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story", "Bug"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress", "Blocked", "On Hold"],
                DoneStates = ["Done"],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            if (withFlaggedAdditionalField)
            {
                flaggedFieldId = connection.AdditionalFieldDefinitions[0].Id;
            }

            return new SeededTeam(team.Id, connection.Id, flaggedFieldId);
        }

        private static string BlockedByStatesRuleSetJson(List<string> states)
        {
            var ruleSet = new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [.. states.Select(state => new WorkItemRuleCondition { FieldKey = "workitem.state", Operator = RuleOperators.Equals, Value = state })],
            };

            return JsonSerializer.Serialize(ruleSet);
        }

        protected void SeedWorkItem(
            int teamId,
            string referenceId,
            string state,
            List<string>? tags = null,
            DateTime? currentStateEnteredAt = null,
            Dictionary<int, string?>? additionalFieldValues = null,
            StateCategories stateCategory = StateCategories.Doing)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            var team = teamRepository.GetById(teamId) ?? throw new InvalidOperationException($"Team {teamId} not found");

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = state,
                StateCategory = stateCategory,
                CreatedDate = SyncDay.AddDays(-30),
                StartedDate = SyncDay.AddDays(-20),
                ClosedDate = null,
                Order = referenceId,
                CurrentStateEnteredAt = currentStateEnteredAt,
                Tags = tags ?? [],
                AdditionalFieldValues = additionalFieldValues ?? new(),
            };

            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();
        }

        // --- Driving-port interactions ---

        protected TeamSettingDto BuildTeamSettings(SeededTeam team)
        {
            return new TeamSettingDto
            {
                Id = team.TeamId,
                Name = $"Team {team.TeamId}",
                DataRetrievalValue = "project = TEST",
                WorkTrackingSystemConnectionId = team.ConnectionId,
                WorkItemTypes = ["Story", "Bug"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress", "Blocked", "On Hold"],
                DoneStates = ["Done"],
                ThroughputHistory = 30,
                UseFixedDatesForThroughput = false,
                FeatureWIP = 1,
                AutomaticallyAdjustFeatureWIP = false,
                DoneItemsCutoffDays = 365,
                StateMappings = [],
            };
        }

        protected async Task<HttpResponseMessage> PutTeamSettings(int teamId, TeamSettingDto settings)
            => await Client.PutAsJsonAsync($"/api/latest/teams/{teamId}", settings);

        protected async Task<HttpResponseMessage> PutTeamSettings(int teamId, JsonObject rawPayload)
            => await Client.PutAsync(
                $"/api/latest/teams/{teamId}",
                new StringContent(rawPayload.ToJsonString(), System.Text.Encoding.UTF8, "application/json"));

        protected async Task<(HttpStatusCode Status, string Body)> GetTeamSettings(int teamId)
        {
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/settings");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetTeamWip(int teamId)
        {
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/wip?asOfDate={SyncDay:O}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        // --- JSON assertion helpers ---

        protected static JsonElement WorkItemByReference(string body, string referenceId)
        {
            using var document = JsonDocument.Parse(body);
            var clone = document.RootElement.Clone();
            Assert.That(clone.ValueKind, Is.EqualTo(JsonValueKind.Array), $"Expected a work-item array. Body: {body}");
            foreach (var element in clone.EnumerateArray())
            {
                if (element.TryGetProperty("referenceId", out var refProp) && refProp.GetString() == referenceId)
                {
                    return element;
                }
            }

            throw new AssertionException($"Work item {referenceId} not found in read surface. Body: {body}");
        }

        protected static bool TryGetString(string body, string propertyName, out string? value)
        {
            value = null;
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty(propertyName, out var prop))
            {
                return false;
            }

            value = prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
            return true;
        }

        protected readonly record struct SeededTeam(int TeamId, int ConnectionId, int FlaggedFieldId);
    }
}
