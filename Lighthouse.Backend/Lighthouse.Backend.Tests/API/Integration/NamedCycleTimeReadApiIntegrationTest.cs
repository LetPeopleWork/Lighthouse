using System.Net;
using System.Text.Json;
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
    public class NamedCycleTimeReadApiIntegrationTest
    {
        private const string Planned = "Planned";
        private const string InProgress = "In Progress";
        private const string Done = "Done";
        private const int ConceptToCashDefinitionId = 1;

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
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
        public async Task DefinitionIdAbsent_ReturnsByteIdenticalDefaultCycleTimeSeries()
        {
            var teamId = SeedTeamWithConceptToCashItems();

            client.AsTeamAdmin(teamId);
            var defaultResponse = await client.GetAsync(CycleTimeDataUrl(teamId, definitionId: null));
            var explicitZeroResponse = await client.GetAsync(CycleTimeDataUrl(teamId, definitionId: 0));

            var defaultBody = await defaultResponse.Content.ReadAsStringAsync();
            var explicitZeroBody = await explicitZeroResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaultResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), defaultBody);
                Assert.That(explicitZeroBody, Is.EqualTo(defaultBody),
                    "definitionId=0 must behave exactly like the no-param default series (ADR-062 §1).");

                var phx204DefaultCycleTime = CycleTimeFor(defaultBody, "PHX-204");
                Assert.That(phx204DefaultCycleTime, Is.EqualTo(DefaultPhx204CycleTime()),
                    $"The default series uses the StartedDate->ClosedDate cycle time, not the named boundary. Body: {defaultBody}");
            }
        }

        [Test]
        public async Task NamedDefinition_PlotsEachClosedItemAtItsOrderedBoundaryDuration_Phx204Is47Days()
        {
            var teamId = SeedTeamWithConceptToCashItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CycleTimeDataUrl(teamId, ConceptToCashDefinitionId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CycleTimeFor(body, "PHX-204"), Is.EqualTo(47),
                    $"PHX-204 reaches Planned at conceptStart and Done 46 days later -> inclusive 47-day named duration. Body: {body}");
            }
        }

        [Test]
        public async Task NamedDefinition_RecomputesPercentileLinesOverTheNamedSeries()
        {
            var teamId = SeedTeamWithConceptToCashItems();

            client.AsTeamAdmin(teamId);
            var namedResponse = await client.GetAsync(CycleTimePercentilesUrl(teamId, ConceptToCashDefinitionId));
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
        public async Task NamedDefinition_ReEntryUsesFirstCrossingNotTheReopen_Phx211()
        {
            var teamId = SeedTeamWithConceptToCashItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CycleTimeDataUrl(teamId, ConceptToCashDefinitionId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CycleTimeFor(body, "PHX-211"), Is.EqualTo(30),
                    $"PHX-211 reopens into Planned, but the named duration spans the FIRST Planned crossing to the FIRST Done crossing. Body: {body}");
            }
        }

        [Test]
        public async Task NamedDefinition_ItemsCrossingNeitherOrOnlyOneBoundary_AreExcludedFromTheSeries()
        {
            var teamId = SeedTeamWithConceptToCashItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CycleTimeDataUrl(teamId, ConceptToCashDefinitionId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(ReferenceIds(body), Does.Not.Contain("PHX-NEVERDONE"),
                    $"D9: a closed item that never crossed the Done boundary is excluded from the named series. Body: {body}");
            }
        }

        [Test]
        public async Task NamedDefinition_FewQualifyingItems_StillPlotsThemWithCountAndPercentiles_NoSpecialLowSampleState()
        {
            var teamId = SeedTeamWithConceptToCashItems();

            client.AsTeamAdmin(teamId);
            var dataResponse = await client.GetAsync(CycleTimeDataUrl(teamId, ConceptToCashDefinitionId));
            var percentilesResponse = await client.GetAsync(CycleTimePercentilesUrl(teamId, ConceptToCashDefinitionId));

            var dataBody = await dataResponse.Content.ReadAsStringAsync();
            var percentilesBody = await percentilesResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(dataResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), dataBody);
                Assert.That(percentilesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), percentilesBody);

                Assert.That(ReferenceIds(dataBody), Is.EquivalentTo(new[] { "PHX-204", "PHX-211" }),
                    $"D9: exactly the two boundary-crossing items are plotted, with no low-sample suppression. Body: {dataBody}");
                Assert.That(PercentileCount(percentilesBody), Is.EqualTo(4),
                    $"The 50/70/85/95 percentile lines are still returned for a small named sample. Body: {percentilesBody}");
            }
        }

        [Test]
        public async Task NamedBranch_NonPremiumCaller_PassingDefinitionId_IsRefused()
        {
            var teamId = SeedTeamWithConceptToCashItems();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

            client.AsTeamAdmin(teamId);
            var namedResponse = await client.GetAsync(CycleTimeDataUrl(teamId, ConceptToCashDefinitionId));
            var defaultResponse = await client.GetAsync(CycleTimeDataUrl(teamId, definitionId: null));

            var namedBody = await namedResponse.Content.ReadAsStringAsync();
            var defaultBody = await defaultResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(namedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), namedBody);
                Assert.That(defaultResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), defaultBody);
            }
        }

        [Test]
        public async Task NamedBranch_TeamViewer_CanReadTheNamedSeries()
        {
            var teamId = SeedTeamWithConceptToCashItems();

            client.AsTeamViewer(teamId);
            var viewerResponse = await client.GetAsync(CycleTimeDataUrl(teamId, ConceptToCashDefinitionId));

            client.AsAnonymous();
            var anonymousResponse = await client.GetAsync(CycleTimeDataUrl(teamId, ConceptToCashDefinitionId));

            var viewerBody = await viewerResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), viewerBody);
                Assert.That(
                    new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound },
                    Does.Contain(anonymousResponse.StatusCode),
                    $"An anonymous caller must not read the named series (existing TeamRead guard). Status: {anonymousResponse.StatusCode}");
            }
        }

        private int DefaultPhx204CycleTime()
        {
            return (int)(conceptStart.AddDays(46).Date - conceptStart.Date).TotalDays + 1;
        }

        private string CycleTimeDataUrl(int teamId, int? definitionId)
        {
            var baseUrl = $"/api/latest/teams/{teamId}/metrics/cycleTimeData?startDate={windowStart:O}&endDate={windowEnd:O}";
            return definitionId.HasValue ? $"{baseUrl}&definitionId={definitionId.Value}" : baseUrl;
        }

        private string CycleTimePercentilesUrl(int teamId, int? definitionId)
        {
            var baseUrl = $"/api/latest/teams/{teamId}/metrics/cycleTimePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";
            return definitionId.HasValue ? $"{baseUrl}&definitionId={definitionId.Value}" : baseUrl;
        }

        private int SeedTeamWithConceptToCashItems()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            AddItem(workItemRepository, transitionRepository, team, "PHX-204", conceptStart, conceptStart.AddDays(46),
                Transition(Planned, InProgress, conceptStart.AddDays(10)),
                Transition(InProgress, Done, conceptStart.AddDays(46)));

            AddItem(workItemRepository, transitionRepository, team, "PHX-211", conceptStart, conceptStart.AddDays(29),
                Transition(Planned, InProgress, conceptStart.AddDays(5)),
                Transition(InProgress, Planned, conceptStart.AddDays(12)),
                Transition(Planned, InProgress, conceptStart.AddDays(20)),
                Transition(InProgress, Done, conceptStart.AddDays(29)));

            AddItem(workItemRepository, transitionRepository, team, "PHX-NEVERDONE", conceptStart, conceptStart.AddDays(40),
                Transition(Planned, InProgress, conceptStart.AddDays(6)));

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
                ToDoStates = [Planned],
                DoingStates = [InProgress],
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

        private static string[] ReferenceIds(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.EnumerateArray()
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
