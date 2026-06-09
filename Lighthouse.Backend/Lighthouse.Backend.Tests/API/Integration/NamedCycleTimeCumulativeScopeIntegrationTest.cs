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
    // US-04 (D10): cumulativeStateTime scoped to a named window via an additive optional definitionId.
    // The DISTILL "premium-gated cumulative read" scaffold is superseded by the ADR-062 revision (no
    // read-side license gate — the premium gate lives on definition create/update only), mirroring the
    // read-gate removal in 01-01.
    [TestFixture]
    [NonParallelizable]
    public class NamedCycleTimeCumulativeScopeIntegrationTest
    {
        private const string Backlog = "Backlog";
        private const string Implementation = "Implementation";
        private const string Review = "Review";
        private const string Done = "Done";
        private const int ReviewToDoneDefinitionId = 1;
        private const int InvalidDefinitionId = 2;
        private const int BacklogToDoneDefinitionId = 3;

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;
        private DateTime workStart;
        private int seededTeamId;

        [SetUp]
        public void Init()
        {
            // Distinct ~100y window base so the process-wide MetricsCache (reused team ids) never collides
            // with the read/validity fixtures' cumulative reads.
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 400 + 36500;
            windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            windowStart = windowEnd.AddDays(-180);
            workStart = windowStart.AddDays(20);

            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }

            seededTeamId = SeedTeamWithWindowedItem();
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
        public async Task DefinitionIdAbsent_CumulativeStateTime_IsByteIdenticalToTodaysUnscopedResponse()
        {
            var unscoped = await client.GetAsync(CumulativeUrl(null));
            var explicitZero = await client.GetAsync(CumulativeUrl(0));

            var unscopedBody = await unscoped.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unscoped.StatusCode, Is.EqualTo(HttpStatusCode.OK), unscopedBody);
                Assert.That(await explicitZero.Content.ReadAsStringAsync(), Is.EqualTo(unscopedBody),
                    "definitionId=0 behaves exactly like the no-param unscoped response.");
                Assert.That(StateBar(unscopedBody, Implementation), Is.GreaterThan(0),
                    "The unscoped chart bars all Doing-state time, including Implementation before the Review window.");
            }
        }

        [Test]
        public async Task DefinitionIdValid_ScopeSpansTheNamedStates_StatesOutsideTheSpanHaveNoBar()
        {
            var response = await client.GetAsync(CumulativeUrl(ReviewToDoneDefinitionId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(StateBar(body, Review), Is.GreaterThan(0),
                    $"Review is the only state in the [Review .. Done) span and carries its dwell. Body: {body}");
                Assert.That(StateNames(body), Does.Not.Contain(Implementation),
                    $"Implementation is BEFORE the Review span start and is not part of the scope. Body: {body}");
                Assert.That(StateNames(body), Does.Not.Contain(Done),
                    $"D10: the end state Done is excluded from the half-open span. Body: {body}");
            }
        }

        [Test]
        public async Task DefinitionIdValid_ScopeSpansEarlierStates_ABarAppearsThatTheDefaultViewNeverShows()
        {
            var unscoped = await (await client.GetAsync(CumulativeUrl(null))).Content.ReadAsStringAsync();
            var scoped = await (await client.GetAsync(CumulativeUrl(BacklogToDoneDefinitionId))).Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StateNames(unscoped), Does.Not.Contain(Backlog),
                    $"The default per-state view bars only Doing states — Backlog (a ToDo state) never appears. Body: {unscoped}");
                Assert.That(StateNames(scoped), Does.Contain(Backlog),
                    $"Scoping to a Backlog-anchored window adds Backlog to the displayed span. Body: {scoped}");
                Assert.That(StateBar(scoped, Backlog), Is.GreaterThan(0),
                    $"Backlog carries its real dwell inside the [enter Backlog .. enter Done) window. Body: {scoped}");
                Assert.That(StateBar(scoped, Done), Is.Zero,
                    $"D10: the end state Done is excluded from the half-open span. Body: {scoped}");
            }
        }

        [Test]
        public async Task DefinitionIdValid_KeepsTheCompletedVersusOngoingSplit_FromTheWorkflowMapping()
        {
            var scoped = await (await client.GetAsync(CumulativeUrl(BacklogToDoneDefinitionId))).Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OngoingBar(scoped, Implementation), Is.GreaterThan(0),
                    $"An item still sitting in Implementation (a Doing state) accrues ongoing time even when scoped — the " +
                    $"completed/ongoing split follows the To Do/Doing/Done mapping, not whether the named cycle finished. Body: {scoped}");
                Assert.That(CompletedBar(scoped, Backlog), Is.GreaterThan(0),
                    $"Earlier states the in-flight item has already left carry completed time. Body: {scoped}");
            }
        }

        [Test]
        public async Task DefinitionIdInvalid_CumulativeScope_IsRefused_ChartStaysUnscoped()
        {
            var scopedInvalid = await (await client.GetAsync(CumulativeUrl(InvalidDefinitionId))).Content.ReadAsStringAsync();
            var unscoped = await (await client.GetAsync(CumulativeUrl(null))).Content.ReadAsStringAsync();

            Assert.That(scopedInvalid, Is.EqualTo(unscoped),
                "An invalid definitionId is refused: the cumulative chart stays unscoped, never scoped against missing states.");
        }

        private string CumulativeUrl(int? definitionId)
        {
            var url = $"/api/latest/teams/{seededTeamId}/metrics/cumulativeStateTime?startDate={windowStart:O}&endDate={windowEnd:O}";
            return definitionId.HasValue ? $"{url}&definitionId={definitionId.Value}" : url;
        }

        private static double StateBar(string body, string state) => StateMetric(body, state, "totalDays");

        private static double CompletedBar(string body, string state) => StateMetric(body, state, "completedContributionDays");

        private static double OngoingBar(string body, string state) => StateMetric(body, state, "ongoingContributionDays");

        private static double StateMetric(string body, string state, string metric)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var row in document.RootElement.GetProperty("states").EnumerateArray())
            {
                if (string.Equals(row.GetProperty("state").GetString(), state, StringComparison.OrdinalIgnoreCase))
                {
                    return row.GetProperty(metric).GetDouble();
                }
            }

            return 0;
        }

        private static List<string> StateNames(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("states").EnumerateArray()
                .Select(row => row.GetProperty("state").GetString() ?? string.Empty)
                .ToList();
        }

        private int SeedTeamWithWindowedItem()
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
                DoingStates = [Implementation, Review],
                DoneStates = [Done],
                CycleTimeDefinitions =
                [
                    new CycleTimeDefinition { Id = ReviewToDoneDefinitionId, Name = "Review to Done", StartState = Review, EndState = Done },
                    new CycleTimeDefinition { Id = InvalidDefinitionId, Name = "Phantom to Done", StartState = "Phantom", EndState = Done },
                    new CycleTimeDefinition { Id = BacklogToDoneDefinitionId, Name = "Lead Time", StartState = Backlog, EndState = Done },
                ],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = "WIN-1",
                Name = "Story WIN-1",
                Type = "Story",
                State = Done,
                StateCategory = StateCategories.Done,
                Url = "https://example.test/items/WIN-1",
                CreatedDate = workStart.AddDays(-1),
                StartedDate = workStart,
                ClosedDate = workStart.AddDays(10),
                Order = "WIN-1",
            };
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            var inFlight = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = "FLOW-1",
                Name = "Story FLOW-1",
                Type = "Story",
                State = Implementation,
                StateCategory = StateCategories.Doing,
                Url = "https://example.test/items/FLOW-1",
                CreatedDate = workStart.AddDays(-1),
                StartedDate = workStart,
                CurrentStateEnteredAt = workStart.AddDays(3),
                Order = "FLOW-1",
            };
            workItemRepository.Add(inFlight);
            workItemRepository.Save().GetAwaiter().GetResult();

            transitionRepository.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Backlog, ToState = Implementation, TransitionedAt = workStart.AddDays(2) });
            transitionRepository.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Implementation, ToState = Review, TransitionedAt = workStart.AddDays(4) });
            transitionRepository.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Review, ToState = Done, TransitionedAt = workStart.AddDays(10) });
            transitionRepository.Add(new WorkItemStateTransition { WorkItemId = inFlight.Id, FromState = Backlog, ToState = Implementation, TransitionedAt = workStart.AddDays(3) });
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }
    }
}
