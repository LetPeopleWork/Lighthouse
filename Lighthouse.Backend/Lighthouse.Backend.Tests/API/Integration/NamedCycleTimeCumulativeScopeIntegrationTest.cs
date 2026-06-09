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
        public async Task DefinitionIdValid_BarsRecomputeOverHalfOpenWindow_AndTheEndStateHasNoBar()
        {
            var response = await client.GetAsync(CumulativeUrl(ReviewToDoneDefinitionId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(StateBar(body, Review), Is.GreaterThan(0),
                    $"Review is in the [enter Review .. enter Done) window and carries its dwell. Body: {body}");
                Assert.That(StateBar(body, Implementation), Is.Zero,
                    $"Implementation is BEFORE the Review window start and is clipped out. Body: {body}");
                Assert.That(StateBar(body, Done), Is.Zero,
                    $"D10: the end state's dwell is excluded — Done contributes no bar. Body: {body}");
            }
        }

        [Test]
        public async Task ScatterNamedDurationWindow_EqualsCumulativeScopedSpan_ForTheSameDefinition()
        {
            var scatterBody = await (await client.GetAsync(CycleTimeDataUrl())).Content.ReadAsStringAsync();
            var cumulativeBody = await (await client.GetAsync(CumulativeUrl(ReviewToDoneDefinitionId))).Content.ReadAsStringAsync();

            var scatterDays = NamedDaysFor(scatterBody, "WIN-1", ReviewToDoneDefinitionId);
            var cumulativeSpan = StateBars(cumulativeBody).Sum();

            Assert.That(Math.Abs(scatterDays - cumulativeSpan), Is.LessThanOrEqualTo(1.5),
                $"The scatter named duration ({scatterDays}d) and the cumulative scoped span ({cumulativeSpan}d) are the " +
                "same window by construction (they resolve through the same NamedCycleTimeDays boundary logic; the " +
                "inclusive-day convention accounts for the <=1d difference).");
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

        private string CycleTimeDataUrl() =>
            $"/api/latest/teams/{seededTeamId}/metrics/cycleTimeData?startDate={windowStart:O}&endDate={windowEnd:O}";

        private static double StateBar(string body, string state)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var row in document.RootElement.GetProperty("states").EnumerateArray())
            {
                if (string.Equals(row.GetProperty("state").GetString(), state, StringComparison.OrdinalIgnoreCase))
                {
                    return row.GetProperty("totalDays").GetDouble();
                }
            }

            return 0;
        }

        private static double[] StateBars(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("states").EnumerateArray()
                .Select(row => row.GetProperty("totalDays").GetDouble())
                .ToArray();
        }

        private static int NamedDaysFor(string body, string referenceId, int definitionId)
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
            }

            Assert.Fail($"Named duration for '{referenceId}' definition {definitionId} not found. Body: {body}");
            return 0;
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

            transitionRepository.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Backlog, ToState = Implementation, TransitionedAt = workStart });
            transitionRepository.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Implementation, ToState = Review, TransitionedAt = workStart.AddDays(4) });
            transitionRepository.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Review, ToState = Done, TransitionedAt = workStart.AddDays(10) });
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }
    }
}
