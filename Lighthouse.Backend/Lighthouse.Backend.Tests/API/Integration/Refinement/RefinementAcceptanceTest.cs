using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// How the Refinement scenarios reach the system: the real ASP.NET host over a real database, through
    /// the Team settings write and read, the Team read, and the Refinement tab's read. Only the licence and
    /// the instance clock are replaced - the licence because it is external, the clock so that every date a
    /// scenario seeds sits on a day it can name.
    ///
    /// Everything a scenario seeds is a precondition: a Team with its mapped states, and the Work Items its
    /// tracker holds. What the Team's refinement states are, and what the tab lists, is only ever put there
    /// by the scenario's own action through the Team settings write.
    /// </summary>
    public abstract class RefinementAcceptanceTest
    {
        protected const string Backlog = "Backlog";

        protected const string Next = "Next";

        protected const string Analysing = "Analysing";

        protected const string Implementation = "Implementation";

        protected const string Done = "Done";

        protected const string Refining = "Refining";

        protected const string Grooming = "Grooming";

        /// <summary>Gravity's Doing states once its admin has taken Analysing out of them.</summary>
        protected static readonly string[] DoingWithoutAnalysing = [Next, Implementation];

        /// <summary>A Wednesday morning in the instance's zone, so every day below is fixed.</summary>
        protected static readonly DateTimeOffset Today = new(2026, 10, 7, 9, 0, 0, TimeSpan.Zero);

        /// <summary>How many days back the Team's Throughput, and anything sampled over the same window, looks.</summary>
        protected const int ThroughputHistoryDays = 30;

        private static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web);

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;

        /// <summary>The instance's clock. Scenarios about which day it is move it; it starts on <see cref="Today"/>.</summary>
        protected FakeLighthouseClock InstanceClock = null!;

        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();

            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
            InstanceClock = new FakeLighthouseClock(Today);
            var clock = InstanceClock;

            Factory = WithAuthentication(RootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseService.Object);
                        services.RemoveAll<ILighthouseClock>();
                        services.AddSingleton<ILighthouseClock>(clock);
                        ConfigureAdditionalServices(services);
                    });
                });

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var context = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Who the instance lets in. By default sign-in is on and roles are enforced; scenarios about an
        /// instance without sign-in, or one that does not enforce roles, choose that instance instead.
        /// </summary>
        protected virtual WebApplicationFactory<Program> WithAuthentication(TestWebApplicationFactory<Program> root)
            => TestWebApplicationFactory<Program>.WithTestAuthentication(root);

        /// <summary>Anything else a fixture replaces in the host, after the licence and the clock.</summary>
        protected virtual void ConfigureAdditionalServices(IServiceCollection services)
        {
            // Most fixtures run the host exactly as shipped apart from the licence and the clock.
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Preconditions ---

        /// <summary>
        /// A Team as the Gravity demo Team maps its tracker: To Do "Backlog"; Doing "Next", "Analysing" and
        /// "Implementation"; Done "Done". No refinement state is chosen.
        /// </summary>
        protected TeamUnderTest ATeamMappedLikeGravity(string name = "Team Gravity")
            => SeedTeam(name, [Backlog], [Next, Analysing, Implementation], [Done], []);

        /// <summary>A Team whose tracker states Analysing and Grooming are both mapped to its Doing state "Refining".</summary>
        protected TeamUnderTest TeamOrbitMappingAnalysingAndGroomingAsRefining()
            => SeedTeam(
                "Team Orbit",
                [Backlog],
                [Refining, Implementation],
                [Done],
                [new StateMapping { Name = Refining, States = [Analysing, Grooming] }]);

        protected TeamUnderTest SeedTeam(
            string name,
            List<string> toDoStates,
            List<string> doingStates,
            List<string> doneStates,
            List<StateMapping> stateMappings,
            int sleProbability = 0,
            int sleRange = 0)
        {
            using var scope = Factory.Services.CreateScope();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = connection,
                DataRetrievalValue = "project = GRAVITY",
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = toDoStates,
                DoingStates = doingStates,
                DoneStates = doneStates,
                StateMappings = stateMappings,
                DoneItemsCutoffDays = 365,
                ThroughputHistory = ThroughputHistoryDays,
                ServiceLevelExpectationProbability = sleProbability,
                ServiceLevelExpectationRange = sleRange,
            };

            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return new TeamUnderTest(team.Id, connection.Id, name, toDoStates, doingStates, doneStates, stateMappings, sleProbability, sleRange);
        }

        /// <summary>
        /// A Work Item the Team's tracker holds. Its rank in the tracker's backlog is <paramref name="rank"/>;
        /// a Doing item started <paramref name="startedDaysAgo"/> days before today. It belongs to no other
        /// Work Item.
        /// </summary>
        protected void SeedWorkItem(
            TeamUnderTest team,
            string referenceId,
            string name,
            string state,
            StateCategories category,
            string rank,
            int startedDaysAgo = 0)
            => SeedWorkItems(team, [new TrackerWorkItem(referenceId, name, state, category, rank, startedDaysAgo)]);

        protected void SeedWorkItems(TeamUnderTest team, List<TrackerWorkItem> workItems)
        {
            using var scope = Factory.Services.CreateScope();

            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var owner = teamRepository.GetById(team.TeamId) ?? throw new InvalidOperationException($"Team {team.TeamId} not found");
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            foreach (var tracked in workItems)
            {
                var started = tracked.Category == StateCategories.Doing
                    ? Today.UtcDateTime.Date.AddDays(-tracked.StartedDaysAgo).AddHours(12)
                    : (DateTime?)null;

                workItemRepository.Add(new WorkItem
                {
                    Team = owner,
                    TeamId = owner.Id,
                    ReferenceId = tracked.ReferenceId,
                    Name = tracked.Name,
                    Type = "User Story",
                    State = tracked.State,
                    StateCategory = tracked.Category,
                    CreatedDate = Today.UtcDateTime.AddDays(-60),
                    StartedDate = started,
                    ClosedDate = null,
                    Order = tracked.Rank,
                    Url = TrackerAddressOf(tracked.ReferenceId),
                    ParentReferenceId = tracked.ParentReferenceId,
                });
            }

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        protected static string TrackerAddressOf(string referenceId) => $"https://tracker.example/browse/{referenceId}";

        protected int WorkItemsStoredFor(TeamUnderTest team)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return context.WorkItems.Count(item => item.TeamId == team.TeamId);
        }

        // --- Who is asking ---

        protected void TheCallerAdministersTheTeam(TeamUnderTest team) => Client.AsTeamAdmin(team.TeamId);

        protected void TheCallerOnlyReadsTheTeam(TeamUnderTest team) => Client.AsTeamViewer(team.TeamId);

        protected void TheCallerHasNoRoleOnTheTeam() => Client.AsViewer();

        protected void TheCallerAdministersTheWholeInstance() => Client.AsSystemAdmin();

        // --- What the admin has already saved ---

        /// <summary>The Team's admin has saved these refinement states, and the save was accepted.</summary>
        protected async Task<TeamUnderTest> TheAdminHasChosen(TeamUnderTest team, params string[] states)
        {
            TheCallerAdministersTheTeam(team);

            using var save = await SaveTheTeamSettingsChoosing(team, states);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin's choice of refinement states was not saved, so nothing that follows can build on it. {await save.Content.ReadAsStringAsync()}");

            return team;
        }

        /// <summary>The Team's admin has saved Gravity's Doing states without Analysing, and the save was accepted.</summary>
        protected async Task<TeamUnderTest> TheAdminHasTakenAnalysingOutOfDoing(TeamUnderTest team)
        {
            TheCallerAdministersTheTeam(team);

            using var save = await SaveTheTeamSettingsWithDoingStates(team, DoingWithoutAnalysing);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin could not take {Analysing} out of Doing, so the scenario describes a Team that still maps it. {await save.Content.ReadAsStringAsync()}");

            return team with { DoingStates = [.. DoingWithoutAnalysing] };
        }

        // --- Driving ports ---

        /// <summary>
        /// The Team settings form saved with the given refinement states chosen. The rest of the form is what
        /// the Team already holds, so nothing but the refinement section changes.
        /// </summary>
        protected async Task<HttpResponseMessage> SaveTheTeamSettingsChoosing(TeamUnderTest team, params string[] refinementStates)
        {
            var payload = TheTeamSettingsFormFor(team);
            payload["refinement"] = new JsonObject
            {
                ["states"] = new JsonArray([.. refinementStates.Select(state => (JsonNode)new JsonObject { ["state"] = state })]),
            };

            return await PutTheTeamSettings(team, payload);
        }

        /// <summary>A new Team created from the given Team's settings form, under another name, with these refinement states chosen.</summary>
        protected async Task<HttpResponseMessage> CreateATeamLike(TeamUnderTest team, string name, params string[] refinementStates)
        {
            var payload = TheTeamSettingsFormFor(team with { Name = name });
            payload["refinement"] = new JsonObject
            {
                ["states"] = new JsonArray([.. refinementStates.Select(state => (JsonNode)new JsonObject { ["state"] = state })]),
            };

            return await Client.PostAsync(
                "/api/latest/teams",
                new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"));
        }

        /// <summary>The Team settings form saved by a client that says nothing about refinement at all.</summary>
        protected async Task<HttpResponseMessage> SaveTheTeamSettingsLeavingRefinementOut(TeamUnderTest team)
            => await PutTheTeamSettings(team, TheTeamSettingsFormFor(team));

        /// <summary>The Team settings form saved with the Team's Doing states replaced, refinement left out.</summary>
        protected async Task<HttpResponseMessage> SaveTheTeamSettingsWithDoingStates(TeamUnderTest team, params string[] doingStates)
        {
            var payload = TheTeamSettingsFormFor(team with { DoingStates = [.. doingStates] });
            return await PutTheTeamSettings(team, payload);
        }

        protected async Task<JsonElement> ReadTheTeamSettings(TeamUnderTest team)
            => await ReadOk($"/api/latest/teams/{team.TeamId}/settings");

        protected async Task<JsonElement> ReadTheTeam(TeamUnderTest team)
            => await ReadOk($"/api/latest/teams/{team.TeamId}");

        protected async Task<HttpResponseMessage> AskForTheRefinementTab(int teamId)
            => await Client.GetAsync($"/api/latest/teams/{teamId}/refinement");

        protected async Task<JsonElement> ReadTheRefinementTab(TeamUnderTest team)
            => await ReadOk($"/api/latest/teams/{team.TeamId}/refinement");

        protected async Task<JsonElement> ReadTheTerminology()
            => await ReadOk("/api/latest/terminology/all");

        protected async Task<HttpResponseMessage> PutTheTeamSettings(TeamUnderTest team, JsonObject payload)
            => await Client.PutAsync(
                $"/api/latest/teams/{team.TeamId}",
                new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"));

        protected static JsonObject TheTeamSettingsFormFor(TeamUnderTest team)
        {
            var form = new TeamSettingDto
            {
                Id = team.TeamId,
                Name = team.Name,
                DataRetrievalValue = "project = GRAVITY",
                WorkTrackingSystemConnectionId = team.ConnectionId,
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = team.ToDoStates,
                DoingStates = team.DoingStates,
                DoneStates = team.DoneStates,
                StateMappings = [.. team.StateMappings.Select(mapping => new StateMappingDto(mapping))],
                ThroughputHistory = ThroughputHistoryDays,
                UseFixedDatesForThroughput = false,
                FeatureWIP = 1,
                AutomaticallyAdjustFeatureWIP = false,
                DoneItemsCutoffDays = 365,
                ServiceLevelExpectationProbability = team.SleProbability,
                ServiceLevelExpectationRange = team.SleRange,
            };

            return JsonNode.Parse(JsonSerializer.Serialize(form, WireOptions))?.AsObject()
                ?? throw new InvalidOperationException("The Team settings form did not serialise to an object.");
        }

        protected async Task<JsonElement> ReadOk(string address)
        {
            using var response = await Client.GetAsync(address);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{address} answered {(int)response.StatusCode}: {body}");

            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }

        // --- Reading the answers ---

        /// <summary>
        /// The refinement states a settings answer lists, in the order it lists them. A Team never configured
        /// may carry no refinement section at all; that reads as no states, and the scenarios that care
        /// whether the Team counts as configured ask the Team read for it rather than relying on this.
        /// </summary>
        protected static List<RefinementStateReading> RefinementStatesIn(JsonElement settings)
        {
            if (!settings.TryGetProperty("refinement", out var refinement) || refinement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            if (!refinement.TryGetProperty("states", out var states) || states.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return [.. states.EnumerateArray().Select(RefinementStateReading.From)];
        }

        /// <summary>
        /// Whether the Team read says the Team has refinement states. Absent reads as a failed assertion
        /// rather than as false, because "not configured" must be said, not inferred from silence.
        /// </summary>
        protected static bool RefinementConfiguredOn(JsonElement team)
        {
            Assert.That(team.TryGetProperty("refinementConfigured", out var configured), Is.True,
                $"The Team read does not say whether refinement states are chosen. Body: {team}");

            return configured.ValueKind == JsonValueKind.True;
        }

        protected static List<RefinementRowReading> RowsIn(JsonElement refinementTab)
        {
            Assert.That(refinementTab.TryGetProperty("workItems", out var rows), Is.True,
                $"The Refinement tab's answer carries no list of Work Items. Body: {refinementTab}");

            return rows.ValueKind == JsonValueKind.Array
                ? [.. rows.EnumerateArray().Select(RefinementRowReading.From)]
                : [];
        }

        /// <summary>Every property name any row of the Refinement tab's answer carries.</summary>
        protected static List<string> RowPropertiesIn(JsonElement refinementTab)
        {
            Assert.That(refinementTab.TryGetProperty("workItems", out var rows), Is.True,
                $"The Refinement tab's answer carries no list of Work Items. Body: {refinementTab}");

            return rows.ValueKind == JsonValueKind.Array
                ? [.. rows.EnumerateArray().SelectMany(row => row.EnumerateObject().Select(property => property.Name))]
                : [];
        }

        protected static string? TextOf(JsonElement element, string property)
            => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        protected sealed record TeamUnderTest(
            int TeamId,
            int ConnectionId,
            string Name,
            List<string> ToDoStates,
            List<string> DoingStates,
            List<string> DoneStates,
            List<StateMapping> StateMappings,
            int SleProbability = 0,
            int SleRange = 0);

        protected sealed record TrackerWorkItem(
            string ReferenceId,
            string Name,
            string State,
            StateCategories Category,
            string Rank,
            int StartedDaysAgo = 0,
            string ParentReferenceId = "");

        protected sealed record RefinementStateReading(string? State)
        {
            public static RefinementStateReading From(JsonElement entry) => new(TextOf(entry, "state"));
        }

        /// <summary>
        /// One row of the Refinement tab. A parent reference that is missing reads as null, so it cannot pass
        /// for the empty reference a row without a parent has to send.
        /// </summary>
        protected sealed record RefinementRowReading(
            string? ReferenceId,
            string? Name,
            string? Url,
            string? State,
            string? ParentReferenceId)
        {
            public static RefinementRowReading From(JsonElement row) => new(
                TextOf(row, "referenceId"),
                TextOf(row, "name"),
                TextOf(row, "url"),
                TextOf(row, "state"),
                TextOf(row, "parentReferenceId"));
        }
    }
}
