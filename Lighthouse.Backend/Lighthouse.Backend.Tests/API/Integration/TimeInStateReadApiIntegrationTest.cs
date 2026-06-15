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
    public class TimeInStateReadApiIntegrationTest
    {
        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime syncDay;
        private DateTime fourteenDaysBeforeSync;
        private DateTime nineDaysBeforeSync;

        [SetUp]
        public void Init()
        {
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 60;
            syncDay = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            fourteenDaysBeforeSync = syncDay.Date.AddDays(-14).AddHours(9).AddMinutes(30);
            nineDaysBeforeSync = syncDay.Date.AddDays(-9).AddHours(13).AddMinutes(15);

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
        public async Task GetWip_JiraTeamWithInProgressItem_ExposesCurrentStateEnteredAtPerItem()
        {
            var teamId = SeedTeamWithInProgressItem(
                WorkTrackingSystems.Jira,
                referenceId: "LGHTHSDMO-12",
                state: "In Progress",
                currentStateEnteredAt: nineDaysBeforeSync);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(WipUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                var item = SingleWorkItem(body);
                Assert.That(TryGetDateTime(item, "currentStateEnteredAt", out _), Is.True,
                    $"Work item JSON must carry currentStateEnteredAt for US-01. Body: {body}");
            }
        }

        [Test]
        public async Task GetWip_TeamSyncedWithTransitionHistory_CurrentStateEnteredAtMatchesLastTransitionWithinOneDay()
        {
            var teamId = SeedTeamWithTransitionHistory(
                WorkTrackingSystems.AzureDevOps,
                referenceId: "12345",
                currentState: "Active",
                lastTransitionInto: fourteenDaysBeforeSync);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(WipUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                var item = SingleWorkItem(body);
                Assert.That(TryGetDateTime(item, "currentStateEnteredAt", out var enteredAt), Is.True, body);
                Assert.That((enteredAt.Date - fourteenDaysBeforeSync.Date).Duration(), Is.LessThanOrEqualTo(TimeSpan.FromDays(1)),
                    $"Derived currentStateEnteredAt must equal the last transition timestamp within 1 day. Body: {body}");
            }
        }

        [Test]
        public async Task GetWip_SameTeamReadTwice_CurrentStateEnteredAtIsStableAcrossReads()
        {
            var teamId = SeedTeamWithTransitionHistory(
                WorkTrackingSystems.AzureDevOps,
                referenceId: "12345",
                currentState: "Active",
                lastTransitionInto: fourteenDaysBeforeSync);

            client.AsTeamAdmin(teamId);

            var firstBody = await (await client.GetAsync(WipUrl(teamId))).Content.ReadAsStringAsync();
            var secondBody = await (await client.GetAsync(WipUrl(teamId))).Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TryGetDateTime(SingleWorkItem(firstBody), "currentStateEnteredAt", out var firstEnteredAt), Is.True, firstBody);
                Assert.That(TryGetDateTime(SingleWorkItem(secondBody), "currentStateEnteredAt", out var secondEnteredAt), Is.True, secondBody);
                Assert.That(secondEnteredAt, Is.EqualTo(firstEnteredAt),
                    "Re-reading the same synced team must surface a stable currentStateEnteredAt (DDD-7 idempotency).");
            }
        }

        [Test]
        public async Task GetWip_ItemFirstObservedThisSync_CurrentStateEnteredAtIsNull()
        {
            var teamId = SeedTeamWithInProgressItem(
                WorkTrackingSystems.Jira,
                referenceId: "LGHTHSDMO-99",
                state: "In Progress",
                currentStateEnteredAt: null);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(WipUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                var item = SingleWorkItem(body);
                Assert.That(item.TryGetProperty("currentStateEnteredAt", out var enteredAtProp), Is.True,
                    $"currentStateEnteredAt must be present on the contract even when null. Body: {body}");
                Assert.That(enteredAtProp.ValueKind, Is.EqualTo(JsonValueKind.Null),
                    $"A first-observed item with no prior transition data must surface currentStateEnteredAt as null. Body: {body}");
            }
        }

        private string WipUrl(int teamId)
        {
            return $"/api/latest/teams/{teamId}/metrics/wip?asOfDate={syncDay:O}";
        }

        private int SeedTeamWithInProgressItem(WorkTrackingSystems system, string referenceId, string state, DateTime? currentStateEnteredAt)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = AddTeam(sp, system);

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = state,
                StateCategory = StateCategories.Doing,
                CreatedDate = fourteenDaysBeforeSync.AddDays(-1),
                StartedDate = fourteenDaysBeforeSync,
                ClosedDate = null,
                Order = referenceId,
                CurrentStateEnteredAt = currentStateEnteredAt,
            };

            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithTransitionHistory(WorkTrackingSystems system, string referenceId, string currentState, DateTime lastTransitionInto)
        {
            return SeedTeamWithInProgressItem(system, referenceId, currentState, currentStateEnteredAt: lastTransitionInto);
        }

        private static Team AddTeam(IServiceProvider sp, WorkTrackingSystems system)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = system,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 0,
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team;
        }

        private static JsonElement SingleWorkItem(string body)
        {
            using var document = JsonDocument.Parse(body);
            var clone = document.RootElement.Clone();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(clone.ValueKind, Is.EqualTo(JsonValueKind.Array), $"Expected a work-item array. Body: {body}");
                Assert.That(clone.GetArrayLength(), Is.EqualTo(1), $"Expected exactly one seeded in-progress item. Body: {body}");
            }
            return clone[0];
        }

        private static bool TryGetDateTime(JsonElement item, string propertyName, out DateTime value)
        {
            value = default;
            if (!item.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            return prop.TryGetDateTime(out value);
        }
    }
}
