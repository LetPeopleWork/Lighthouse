using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [NonParallelizable]
    public class NamedCycleTimeReadApiIntegrationTest
    {
        private const string Backlog = "Backlog";
        private const string Implementation = "Implementation";
        private const string Done = "Done";
        private const int ImplementationToDoneDefinitionId = 1;

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;
        private DateTime conceptStart;

        [SetUp]
        public void Init()
        {
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 400;
            windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            windowStart = windowEnd.AddDays(-180);
            conceptStart = windowStart.AddDays(20);

            rootFactory = new TestWebApplicationFactory<Program>();

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);

            client = factory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var seeders = setupScope.ServiceProvider.GetServices<ISeeder>();
            foreach (var seeder in seeders)
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
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
        public async Task CycleTimeData_ReturnsDefaultCycleTimePerClosedItem_Unchanged()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CycleTimeDataUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(ReferenceIds(body), Is.EquivalentTo(new[] { "PHX-204", "PHX-211", "PHX-NEVERDONE" }),
                    $"cycleTimeData returns every closed item; the named durations ride an additive list per item. Body: {body}");
                Assert.That(CycleTimeFor(body, "PHX-204"), Is.EqualTo(DefaultPhx204CycleTime()),
                    $"The default cycleTime field stays the StartedDate->ClosedDate duration. Body: {body}");
            }
        }

        [Test]
        public async Task CycleTimeData_EachClosedItemCarriesItsNamedCycleTimes_Phx204Is47Days()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CycleTimeDataUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(NamedDaysFor(body, "PHX-204", ImplementationToDoneDefinitionId), Is.EqualTo(47),
                    $"PHX-204 reaches Implementation at conceptStart and Done 46 days later -> inclusive 47-day named duration in cycleTimes. Body: {body}");
            }
        }

        [Test]
        public async Task NamedDefinition_RecomputesPercentileLinesOverTheNamedSeries()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var namedResponse = await client.GetAsync(CycleTimePercentilesUrl(teamId, ImplementationToDoneDefinitionId));
            var defaultResponse = await client.GetAsync(CycleTimePercentilesUrl(teamId, definitionId: null));

            var namedBody = await namedResponse.Content.ReadAsStringAsync();
            var defaultBody = await defaultResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(namedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), namedBody);

                Assert.That(Percentile(namedBody, 85), Is.EqualTo(30),
                    $"P85 over the named durations {{30, 47}} is 30 (PercentileCalculator convention). Body: {namedBody}");
                Assert.That(Percentile(namedBody, 85), Is.Not.EqualTo(Percentile(defaultBody, 85)),
                    $"The named-series P85 (over 2 boundary-crossing items) differs from the default-series P85 (over all 3 closed items). Named: {namedBody} Default: {defaultBody}");
            }
        }

        [Test]
        public async Task CycleTimeData_ReEntryUsesFirstCrossingNotTheReopen_Phx211()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CycleTimeDataUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(NamedDaysFor(body, "PHX-211", ImplementationToDoneDefinitionId), Is.EqualTo(30),
                    $"PHX-211 reopens into Backlog, but the named duration spans the FIRST Implementation crossing to the FIRST Done crossing. Body: {body}");
            }
        }

        [Test]
        public async Task CycleTimeData_ItemCrossingNeitherOrOnlyOneBoundary_HasNoNamedEntry()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CycleTimeDataUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(NamedDaysFor(body, "PHX-NEVERDONE", ImplementationToDoneDefinitionId), Is.Null,
                    $"D9: a closed item that never crossed the Done boundary has no entry for that definition in cycleTimes. Body: {body}");
            }
        }

        [Test]
        public async Task NamedDefinition_FewQualifyingItems_StillPlotsThemWithCountAndPercentiles_NoSpecialLowSampleState()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var dataResponse = await client.GetAsync(CycleTimeDataUrl(teamId));
            var percentilesResponse = await client.GetAsync(CycleTimePercentilesUrl(teamId, ImplementationToDoneDefinitionId));

            var dataBody = await dataResponse.Content.ReadAsStringAsync();
            var percentilesBody = await percentilesResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(dataResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), dataBody);
                Assert.That(percentilesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), percentilesBody);

                Assert.That(ReferenceIdsWithNamedEntry(dataBody, ImplementationToDoneDefinitionId), Is.EquivalentTo(new[] { "PHX-204", "PHX-211" }),
                    $"D9: exactly the two boundary-crossing items carry a named entry, with no low-sample suppression. Body: {dataBody}");
                Assert.That(PercentileCount(percentilesBody), Is.EqualTo(4),
                    $"The 50/70/85/95 percentile lines are still returned for a small named sample. Body: {percentilesBody}");
            }
        }

        [Test]
        public async Task TeamViewer_CanReadCycleTimeDataWithNamedCycleTimes()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamViewer(teamId);
            var viewerResponse = await client.GetAsync(CycleTimeDataUrl(teamId));

            client.AsAnonymous();
            var anonymousResponse = await client.GetAsync(CycleTimeDataUrl(teamId));

            var viewerBody = await viewerResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), viewerBody);
                Assert.That(NamedDaysFor(viewerBody, "PHX-204", ImplementationToDoneDefinitionId), Is.EqualTo(47),
                    $"A Team Viewer reads the named cycle times off cycleTimeData (no read-side premium gate). Body: {viewerBody}");
                Assert.That(
                    new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound },
                    Does.Contain(anonymousResponse.StatusCode),
                    $"An anonymous caller must not read cycle time data (existing TeamRead guard). Status: {anonymousResponse.StatusCode}");
            }
        }

        private int DefaultPhx204CycleTime()
        {
            return (int)(conceptStart.AddDays(46).Date - conceptStart.Date).TotalDays + 1;
        }

        private string CycleTimeDataUrl(int teamId)
        {
            return $"/api/latest/teams/{teamId}/metrics/cycleTimeData?startDate={windowStart:O}&endDate={windowEnd:O}";
        }

        private string CycleTimePercentilesUrl(int teamId, int? definitionId)
        {
            var baseUrl = $"/api/latest/teams/{teamId}/metrics/cycleTimePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";
            return definitionId.HasValue ? $"{baseUrl}&definitionId={definitionId.Value}" : baseUrl;
        }

        private int SeedTeamWithClosedItems()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            AddItem(workItemRepository, transitionRepository, team, "PHX-204", conceptStart, conceptStart.AddDays(46),
                Transition(Backlog, Implementation, conceptStart),
                Transition(Implementation, Done, conceptStart.AddDays(46)));

            AddItem(workItemRepository, transitionRepository, team, "PHX-211", conceptStart, conceptStart.AddDays(29),
                Transition(Backlog, Implementation, conceptStart),
                Transition(Implementation, Backlog, conceptStart.AddDays(12)),
                Transition(Backlog, Implementation, conceptStart.AddDays(20)),
                Transition(Implementation, Done, conceptStart.AddDays(29)));

            AddItem(workItemRepository, transitionRepository, team, "PHX-NEVERDONE", conceptStart, conceptStart.AddDays(40),
                Transition(Backlog, Implementation, conceptStart.AddDays(6)));

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private static void AddItem(
            IWorkItemRepository workItemRepository,
            IWorkItemStateTransitionRepository transitionRepository,
            Team team,
            string referenceId,
            DateTime startedDate,
            DateTime closedDate,
            params WorkItemStateTransition[] transitions)
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

            foreach (var transition in transitions)
            {
                transitionRepository.Add(new WorkItemStateTransition
                {
                    WorkItemId = item.Id,
                    FromState = transition.FromState,
                    ToState = transition.ToState,
                    TransitionedAt = transition.TransitionedAt,
                });
            }
        }

        private static WorkItemStateTransition Transition(string fromState, string toState, DateTime transitionedAt)
        {
            return new WorkItemStateTransition { FromState = fromState, ToState = toState, TransitionedAt = transitionedAt };
        }

        private static Team AddTeam(IServiceProvider sp)
        {
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
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team;
        }

        private static int CycleTimeFor(string body, string referenceId)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.GetProperty("referenceId").GetString() == referenceId)
                {
                    return item.GetProperty("cycleTime").GetInt32();
                }
            }

            Assert.Fail($"Item '{referenceId}' was expected in the series but was absent. Body: {body}");
            return 0;
        }

        private static int? NamedDaysFor(string body, string referenceId, int definitionId)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.GetProperty("referenceId").GetString() != referenceId)
                {
                    continue;
                }

                foreach (var named in item.GetProperty("namedCycleTimes").EnumerateArray())
                {
                    if (named.GetProperty("definitionId").GetInt32() == definitionId)
                    {
                        return named.GetProperty("days").GetInt32();
                    }
                }

                return null;
            }

            Assert.Fail($"Item '{referenceId}' was expected in the series but was absent. Body: {body}");
            return null;
        }

        private static string[] ReferenceIds(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.EnumerateArray()
                .Select(item => item.GetProperty("referenceId").GetString() ?? string.Empty)
                .ToArray();
        }

        private static string[] ReferenceIdsWithNamedEntry(string body, int definitionId)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.EnumerateArray()
                .Where(item => item.GetProperty("namedCycleTimes").EnumerateArray()
                    .Any(named => named.GetProperty("definitionId").GetInt32() == definitionId))
                .Select(item => item.GetProperty("referenceId").GetString() ?? string.Empty)
                .ToArray();
        }

        private static int Percentile(string body, int percentile)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                if (entry.GetProperty("percentile").GetInt32() == percentile)
                {
                    return entry.GetProperty("value").GetInt32();
                }
            }

            Assert.Fail($"Percentile {percentile} was expected in the response but was absent. Body: {body}");
            return 0;
        }

        private static int PercentileCount(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetArrayLength();
        }
    }
}
