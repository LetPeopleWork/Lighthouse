using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
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
    public class CycleTimeDefinitionValidityIntegrationTest
    {
        private const string Backlog = "Backlog";
        private const string Implementation = "Implementation";
        private const string Done = "Done";
        private const int DefinitionId = 1;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private int seededTeamId;
        private int seededConnectionId;
        private DateTime windowStart;
        private DateTime windowEnd;
        private DateTime workStart;

        [SetUp]
        public void Init()
        {
            // Shift this fixture's windows ~200 years before the read fixture's so the process-wide
            // MetricsCache (keyed by reused team id + date window) never collides across test classes.
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 400 + 73000;
            windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            windowStart = windowEnd.AddDays(-180);
            workStart = windowStart.AddDays(20);

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

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }

            SeedTeamWithDefinitionAndItems();
            client.AsTeamAdmin(seededTeamId);
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
        public async Task RemovingABoundaryState_StampsDefinitionDtoIsValidFalse_WithoutReSave()
        {
            Assert.That((await GetDefinitions())[0].IsValid, Is.True, "Baseline: the saved definition is valid.");

            await RemoveImplementationState();

            var definitions = await GetDefinitions();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(definitions, Has.Count.EqualTo(1), "The definition is kept, not dropped.");
                Assert.That(definitions[0].IsValid, Is.False,
                    "IsValid is recomputed from the current AllStates at projection time (not stored).");
            }
        }

        [Test]
        public async Task RemovingABoundaryState_ScatterReadReturnsEmptySeriesPlusInvalidSignal_Not500()
        {
            await RemoveImplementationState();

            var percentilesResponse = await client.GetAsync(PercentilesUrl(DefinitionId));
            var dataResponse = await client.GetAsync(CycleTimeDataUrl());

            var percentilesBody = await percentilesResponse.Content.ReadAsStringAsync();
            var dataBody = await dataResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(percentilesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), percentilesBody);
                Assert.That(Percentiles(percentilesBody).All(value => value == 0), Is.True,
                    $"An invalid definition computes an empty named series, not a 500 and not a wrong series. Body: {percentilesBody}");
                Assert.That(dataResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), dataBody);
                Assert.That(NamedDefinitionIds(dataBody), Does.Not.Contain(DefinitionId),
                    $"No work item carries a named entry for the invalid definition. Body: {dataBody}");
            }
        }

        [Test]
        public async Task RemovingABoundaryState_CumulativeScopeRead_IsRefused_AndStaysUnscoped()
        {
            await RemoveImplementationState();

            var scopedResponse = await client.GetAsync(CumulativeUrl(DefinitionId));
            var unscopedResponse = await client.GetAsync(CumulativeUrl(null));

            var scopedBody = await scopedResponse.Content.ReadAsStringAsync();
            var unscopedBody = await unscopedResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(scopedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), scopedBody);
                Assert.That(scopedBody, Is.EqualTo(unscopedBody),
                    "An invalid definition is refused: the cumulative chart stays unscoped, never scoped against missing states.");
            }
        }

        [Test]
        public async Task ConfigDtoScatterReadAndCumulativeRead_AllReportInvalid_ForTheSameRemovedBoundary()
        {
            await RemoveImplementationState();

            var isValid = (await GetDefinitions())[0].IsValid;
            var percentilesBody = await (await client.GetAsync(PercentilesUrl(DefinitionId))).Content.ReadAsStringAsync();
            var scopedCumulative = await (await client.GetAsync(CumulativeUrl(DefinitionId))).Content.ReadAsStringAsync();
            var unscopedCumulative = await (await client.GetAsync(CumulativeUrl(null))).Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.False, "Config DTO reports invalid.");
                Assert.That(Percentiles(percentilesBody).All(value => value == 0), Is.True, "Scatter read reports an empty series.");
                Assert.That(scopedCumulative, Is.EqualTo(unscopedCumulative), "Cumulative read refuses and stays unscoped.");
            }
        }

        [Test]
        public async Task EditingAnInvalidDefinitionToAPresentBoundary_MakesItValidAgain()
        {
            await RemoveImplementationState();
            Assert.That((await GetDefinitions())[0].IsValid, Is.False, "Precondition: invalid after removal.");

            var settings = BuildSettings(["Coding"]);
            settings.CycleTimeDefinitions = [Definition(DefinitionId, Backlog, Done)];
            var editResponse = await client.PutAsJsonAsync(TeamUrl(), settings);
            Assert.That(editResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await editResponse.Content.ReadAsStringAsync());

            var percentilesBody = await (await client.GetAsync(PercentilesUrl(DefinitionId))).Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That((await GetDefinitions())[0].IsValid, Is.True,
                    "Editing the start to a still-present state flips IsValid back to true.");
                Assert.That(Percentiles(percentilesBody).Any(value => value > 0), Is.True,
                    $"The named read computes again once the definition is valid. Body: {percentilesBody}");
            }
        }

        [Test]
        public async Task DeletingAnInvalidDefinition_RemovesItCleanly()
        {
            await RemoveImplementationState();

            var settings = BuildSettings(["Coding"]);
            settings.CycleTimeDefinitions = [];
            var deleteResponse = await client.PutAsJsonAsync(TeamUrl(), settings);
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await deleteResponse.Content.ReadAsStringAsync());

            Assert.That(await GetDefinitions(), Is.Empty, "The deleted definition disappears from the config list.");
        }

        private async Task RemoveImplementationState()
        {
            var settings = BuildSettings(["Coding"]);
            settings.CycleTimeDefinitions = [Definition(DefinitionId, Implementation, Done)];
            var response = await client.PutAsJsonAsync(TeamUrl(), settings);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

            // The state change re-syncs work items (removes them); re-seed so the reads have data and
            // "empty because invalid" is distinguishable from "empty because no items".
            ReseedClosedItems();
        }

        private void ReseedClosedItems()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = sp.GetRequiredService<IRepository<Team>>().GetById(seededTeamId)!;
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            AddClosedItem(workItemRepository, transitionRepository, team, "VAL-1", workStart, workStart.AddDays(12));
            AddClosedItem(workItemRepository, transitionRepository, team, "VAL-2", workStart.AddDays(2), workStart.AddDays(20));

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();
        }

        private string TeamUrl() => $"/api/latest/teams/{seededTeamId}";

        private string PercentilesUrl(int definitionId) =>
            $"/api/latest/teams/{seededTeamId}/metrics/cycleTimePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}&definitionId={definitionId}";

        private string CycleTimeDataUrl() =>
            $"/api/latest/teams/{seededTeamId}/metrics/cycleTimeData?startDate={windowStart:O}&endDate={windowEnd:O}";

        private string CumulativeUrl(int? definitionId)
        {
            var url = $"/api/latest/teams/{seededTeamId}/metrics/cumulativeStateTime?startDate={windowStart:O}&endDate={windowEnd:O}";
            return definitionId.HasValue ? $"{url}&definitionId={definitionId.Value}" : url;
        }

        private async Task<List<CycleTimeDefinitionDto>> GetDefinitions()
        {
            var response = await client.GetAsync($"{TeamUrl()}/settings");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return JsonSerializer.Deserialize<TeamSettingDto>(body, JsonOptions)!.CycleTimeDefinitions;
        }

        private static int[] Percentiles(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.EnumerateArray()
                .Select(entry => entry.GetProperty("value").GetInt32())
                .ToArray();
        }

        private static int[] NamedDefinitionIds(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.EnumerateArray()
                .SelectMany(item => item.GetProperty("namedCycleTimes").EnumerateArray())
                .Select(named => named.GetProperty("definitionId").GetInt32())
                .ToArray();
        }

        private static CycleTimeDefinitionDto Definition(int id, string startState, string endState) =>
            new() { Id = id, Name = "Implementation to Done", StartState = startState, EndState = endState };

        private TeamSettingDto BuildSettings(string[] doing)
        {
            return new TeamSettingDto
            {
                Id = seededTeamId,
                Name = $"Team {seededTeamId}",
                DataRetrievalValue = "project = TEST",
                WorkTrackingSystemConnectionId = seededConnectionId,
                WorkItemTypes = ["Story"],
                ToDoStates = [Backlog],
                DoingStates = [.. doing],
                DoneStates = [Done],
                ThroughputHistory = 30,
                UseFixedDatesForThroughput = false,
                FeatureWIP = 1,
                AutomaticallyAdjustFeatureWIP = false,
                DoneItemsCutoffDays = 365,
                StateMappings = [],
            };
        }

        private void SeedTeamWithDefinitionAndItems()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 0,
                ToDoStates = [Backlog],
                DoingStates = [Implementation],
                DoneStates = [Done],
                CycleTimeDefinitions = [new CycleTimeDefinition { Id = DefinitionId, Name = "Implementation to Done", StartState = Implementation, EndState = Done }],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            AddClosedItem(workItemRepository, transitionRepository, team, "VAL-1", workStart, workStart.AddDays(12));
            AddClosedItem(workItemRepository, transitionRepository, team, "VAL-2", workStart.AddDays(2), workStart.AddDays(20));

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            seededTeamId = team.Id;
            seededConnectionId = connection.Id;
        }

        private static void AddClosedItem(
            IWorkItemRepository workItemRepository,
            IWorkItemStateTransitionRepository transitionRepository,
            Team team,
            string referenceId,
            DateTime startedDate,
            DateTime closedDate)
        {
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = Done,
                StateCategory = StateCategories.Done,
                Url = $"https://example.test/items/{referenceId}",
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = closedDate,
                Order = referenceId,
            };

            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            transitionRepository.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Backlog, ToState = Implementation, TransitionedAt = startedDate });
            transitionRepository.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Implementation, ToState = Done, TransitionedAt = closedDate });
        }
    }
}
