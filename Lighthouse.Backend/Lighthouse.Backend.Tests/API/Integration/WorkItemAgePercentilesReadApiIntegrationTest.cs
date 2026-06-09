using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
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
    public class WorkItemAgePercentilesReadApiIntegrationTest
    {
        private const string InProgress = "In Progress";

        private static readonly int[] ExpectedPercentileKeys = [50, 70, 85, 95];

        private static readonly HttpStatusCode[] DeniedStatusCodes =
            [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound];

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime today;
        private DateTime windowStart;
        private DateTime windowEnd;

        [SetUp]
        public void Init()
        {
            // WorkItemAge is measured against DateTime.UtcNow (not endDate), so the golden ages must be
            // anchored to today; endDate = today selects the in-progress snapshot that carries those ages.
            today = DateTime.UtcNow.Date;
            windowEnd = today;
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset);
            windowStart = today.AddDays(-180 - offsetDays);

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
                var metricsService = (TeamMetricsService)teardownScope.ServiceProvider.GetRequiredService<ITeamMetricsService>();
                foreach (var seededTeam in dbContext.Teams.ToList())
                {
                    metricsService.InvalidateTeamMetrics(seededTeam);
                }

                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_TeamWithInProgressItemsOfKnownAges_ReturnsExactPercentilesOfThoseAges()
        {
            // Given a team whose current WIP ages are 1,2,2,3,3,4,5,6,7,9; the reused PercentileCalculator
            // (floor(p/100*n)-1 indexing, AS-IS per ADR-065) selects indices 4,6,7,8 => 3,5,6,7.
            var teamId = SeedTeamWithKnownInProgressAges();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var percentiles = PercentilesByKey(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(percentiles[50], Is.EqualTo(3), $"p50 of the current WIP ages. Body: {body}");
                Assert.That(percentiles[70], Is.EqualTo(5), $"p70 of the current WIP ages. Body: {body}");
                Assert.That(percentiles[85], Is.EqualTo(6), $"p85 of the current WIP ages. Body: {body}");
                Assert.That(percentiles[95], Is.EqualTo(7), $"p95 of the current WIP ages. Body: {body}");
            }
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_PopulationIsTheWipSet_ClosedItemsExcluded()
        {
            // Given a team with in-progress items of known ages AND a closed item whose cycle time would skew the result
            var teamId = SeedTeamWithKnownInProgressAgesPlusOneOldClosedItem();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var percentiles = PercentilesByKey(body);
            Assert.That(percentiles[95], Is.EqualTo(7),
                $"p95 must reflect only the in-progress population (WIP ages 1..9 => p95=7); the closed item's 400-day cycle time must NOT leak in. Body: {body}");
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_ResponseShapeIsByteCompatibleWithCycleTimePercentiles()
        {
            // Given a team with in-progress items, both percentile endpoints must return the identical flat shape
            var teamId = SeedTeamWithKnownInProgressAges();

            client.AsTeamAdmin(teamId);
            var wiaResponse = await client.GetAsync(PercentilesUrl(teamId));
            var ctResponse = await client.GetAsync(
                $"/api/latest/teams/{teamId}/metrics/cycleTimePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}");

            var wiaBody = await wiaResponse.Content.ReadAsStringAsync();
            var ctBody = await ctResponse.Content.ReadAsStringAsync();
            var bothOk = wiaResponse.StatusCode == HttpStatusCode.OK && ctResponse.StatusCode == HttpStatusCode.OK;
            Assert.That(bothOk, Is.True,
                $"Both percentile endpoints must return OK before their shapes can be compared. WIA: {wiaResponse.StatusCode} CT: {ctResponse.StatusCode}");
            AssertIsPercentileJsonArray(wiaBody);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PercentileKeysInOrder(wiaBody), Is.EqualTo(PercentileKeysInOrder(ctBody)),
                    $"workItemAgePercentiles must emit the same percentile keys in the same order as cycleTimePercentiles " +
                    $"(flat [{{percentile,value}}], four entries, reusing PercentileValue). WIA: {wiaBody} CT: {ctBody}");
                Assert.That(PropertyNames(wiaBody), Is.EqualTo(PropertyNames(ctBody)),
                    $"Each entry must carry the same JSON property names as cycleTimePercentiles. WIA: {wiaBody} CT: {ctBody}");
            }
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_SameEndDateDifferentStartDate_ReturnsIdenticalPercentiles()
        {
            // Given the same team, two requests with the SAME endDate but DIFFERENT startDate (D4 invariance)
            var teamId = SeedTeamWithKnownInProgressAges();

            client.AsTeamAdmin(teamId);
            var wideStart = windowEnd.AddDays(-365);
            var narrowStart = windowEnd.AddDays(-7);
            var wideResponse = await client.GetAsync(
                $"/api/latest/teams/{teamId}/metrics/workItemAgePercentiles?startDate={wideStart:O}&endDate={windowEnd:O}");
            var narrowResponse = await client.GetAsync(
                $"/api/latest/teams/{teamId}/metrics/workItemAgePercentiles?startDate={narrowStart:O}&endDate={windowEnd:O}");

            var wideBody = await wideResponse.Content.ReadAsStringAsync();
            var narrowBody = await narrowResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(wideResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), wideBody);
                Assert.That(narrowResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), narrowBody);
            }

            Assert.That(PercentilesByKey(wideBody), Is.EqualTo(PercentilesByKey(narrowBody)),
                $"WIA is a current-WIP snapshot keyed on endDate only; startDate must NOT filter the population (D4). " +
                $"wideStart: {wideBody} narrowStart: {narrowBody}");
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_TeamWithNoInProgressItems_ReturnsGracefulZeroValuedSet()
        {
            // Given a team with zero in-progress items (D6 empty-WIP path)
            var teamId = SeedTeamWithNoInProgressItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var percentiles = PercentilesByKey(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(percentiles.Keys, Is.EquivalentTo(ExpectedPercentileKeys),
                    $"Empty WIP yields the four-entry set (BuildPercentiles([]) over an empty list), never a crash. Body: {body}");
                Assert.That(percentiles.Values, Is.All.Zero,
                    $"Empty WIP yields all-zero percentile values (PercentileCalculator returns 0 on an empty list). Body: {body}");
            }
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_TeamWithSingleInProgressItem_ComputesOverThatOneValue()
        {
            // Given a team with exactly one in-progress item aged 5 days (D6 single-item, no low-sample gate)
            var teamId = SeedTeamWithSingleInProgressItem(ageDays: 5);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var percentiles = PercentilesByKey(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(percentiles[50], Is.EqualTo(5), $"Single-item WIP: every percentile is that one value. Body: {body}");
                Assert.That(percentiles[95], Is.EqualTo(5), $"Single-item WIP: every percentile is that one value. Body: {body}");
            }
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_NonPremiumCaller_StillReceivesPercentiles()
        {
            // Given a standard (non-premium) caller with TeamRead — there is NO license gate on this read path (D3)
            var teamId = SeedTeamWithKnownInProgressAges();

            client.AsTeamViewer(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"workItemAgePercentiles is non-premium: a standard caller with read access must receive percentiles, " +
                $"the inverse of a premium-gated endpoint (no ILicenseService on the read path, D3). Body: {body}");

            var percentiles = PercentilesByKey(body);
            Assert.That(percentiles.Keys, Is.EquivalentTo(ExpectedPercentileKeys),
                $"A non-premium caller receives the real percentile payload, not an empty/denied stub. Body: {body}");
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_AnonymousCaller_IsRejected()
        {
            var teamId = SeedTeamWithKnownInProgressAges();

            client.AsAnonymous();
            var response = await client.GetAsync(PercentilesUrl(teamId));

            Assert.That(
                DeniedStatusCodes,
                Does.Contain(response.StatusCode),
                $"An unauthenticated caller must not read team WIP-age percentiles (class-level RbacGuard TeamRead). Status: {response.StatusCode}");
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var teamId = SeedTeamWithKnownInProgressAges();

            client.AsTeamAdmin(teamId);
            var url = $"/api/latest/teams/{teamId}/metrics/workItemAgePercentiles?startDate={windowEnd:O}&endDate={windowStart:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"startDate after endDate must be rejected with 400, mirroring cycleTimePercentiles validation. Body: {body}");
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_StartDateEqualsEndDate_IsAccepted()
        {
            var teamId = SeedTeamWithKnownInProgressAges();

            client.AsTeamAdmin(teamId);
            var url = $"/api/latest/teams/{teamId}/metrics/workItemAgePercentiles?startDate={windowEnd:O}&endDate={windowEnd:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"startDate equal to endDate is a valid single-day window and must be accepted; only strictly-after is rejected. Body: {body}");
        }

        private string PercentilesUrl(int teamId)
        {
            return $"/api/latest/teams/{teamId}/metrics/workItemAgePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";
        }

        private int SeedTeamWithKnownInProgressAges()
        {
            var ages = new[] { 1, 2, 2, 3, 3, 4, 5, 6, 7, 9 };

            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();

            for (var i = 0; i < ages.Length; i++)
            {
                workItemRepository.Add(InProgressItemAged(team, $"WIP-{i}", ages[i]));
            }

            workItemRepository.Save().GetAwaiter().GetResult();
            return team.Id;
        }

        private int SeedTeamWithKnownInProgressAgesPlusOneOldClosedItem()
        {
            var ages = new[] { 1, 2, 2, 3, 3, 4, 5, 6, 7, 9 };

            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();

            for (var i = 0; i < ages.Length; i++)
            {
                workItemRepository.Add(InProgressItemAged(team, $"WIP-{i}", ages[i]));
            }

            var startedLongAgo = today.AddDays(-400);
            workItemRepository.Add(new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = "CLOSED-OLD",
                Name = "Closed long ago, huge cycle time",
                Type = "Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = startedLongAgo.AddDays(-1),
                StartedDate = startedLongAgo,
                ClosedDate = today.AddDays(-2),
                Order = "CLOSED-OLD",
            });

            workItemRepository.Save().GetAwaiter().GetResult();
            return team.Id;
        }

        private int SeedTeamWithNoInProgressItems()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();

            var startedLongAgo = today.AddDays(-30);
            workItemRepository.Add(new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = "DONE-1",
                Name = "Already finished",
                Type = "Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = startedLongAgo.AddDays(-1),
                StartedDate = startedLongAgo,
                ClosedDate = today.AddDays(-5),
                Order = "DONE-1",
            });

            workItemRepository.Save().GetAwaiter().GetResult();
            return team.Id;
        }

        private int SeedTeamWithSingleInProgressItem(int ageDays)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();

            workItemRepository.Add(InProgressItemAged(team, "WIP-ONLY", ageDays));
            workItemRepository.Save().GetAwaiter().GetResult();
            return team.Id;
        }

        private WorkItem InProgressItemAged(Team team, string referenceId, int ageDays)
        {
            // WorkItemAge = (UtcNow.Date - StartedDate.Date).Days + 1 for a Doing item, so StartedDate = today - (ageDays - 1).
            var startedDate = today.AddDays(-(ageDays - 1));
            return new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = InProgress,
                StateCategory = StateCategories.Doing,
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = null,
                Order = referenceId,
            };
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
                DoingStates = [InProgress],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team;
        }

        private static void AssertIsPercentileJsonArray(string body)
        {
            var looksLikeJsonArray = body.TrimStart().StartsWith('[');
            Assert.That(looksLikeJsonArray, Is.True,
                $"Response must be a JSON percentile array; a non-JSON body (e.g. the SPA fallback HTML) means the " +
                $"workItemAgePercentiles endpoint is not wired yet. Body: {body}");
        }

        private static Dictionary<int, int> PercentilesByKey(string body)
        {
            AssertIsPercentileJsonArray(body);
            using var document = JsonDocument.Parse(body);
            var byPercentile = new Dictionary<int, int>();
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                var key = entry.GetProperty("percentile").GetInt32();
                byPercentile[key] = entry.GetProperty("value").GetInt32();
            }
            return byPercentile;
        }

        private static int[] PercentileKeysInOrder(string body)
        {
            AssertIsPercentileJsonArray(body);
            using var document = JsonDocument.Parse(body);
            var keys = new List<int>();
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                keys.Add(entry.GetProperty("percentile").GetInt32());
            }
            return keys.ToArray();
        }

        private static string[] PropertyNames(string body)
        {
            AssertIsPercentileJsonArray(body);
            using var document = JsonDocument.Parse(body);
            var first = document.RootElement.EnumerateArray().FirstOrDefault();
            var names = new List<string>();
            foreach (var property in first.EnumerateObject())
            {
                names.Add(property.Name);
            }
            names.Sort(StringComparer.Ordinal);
            return names.ToArray();
        }
    }
}
