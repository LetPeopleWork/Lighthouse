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
    public class AgeInStatePercentilesReadApiIntegrationTest
    {
        private const string InProgress = "In Progress";
        private const string Review = "Review";
        private const string Test = "Test";

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;

        [SetUp]
        public void Init()
        {
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 400;
            windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            windowStart = windowEnd.AddDays(-180);

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
        public async Task GetAgeInStatePercentiles_TeamWithCompletedItemsAcrossThreeStates_ReturnsExactCumulativeAgeAtExitPercentilesPerState()
        {
            var teamId = SeedTeamWithKnownStateExitAges();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var inProgress = PercentilesForState(body, InProgress);
                var review = PercentilesForState(body, Review);
                var test = PercentilesForState(body, Test);

                Assert.That(inProgress[50], Is.EqualTo(4), $"In Progress p50 cumulative age-at-exit. Body: {body}");
                Assert.That(inProgress[70], Is.EqualTo(6), $"In Progress p70. Body: {body}");
                Assert.That(inProgress[85], Is.EqualTo(7), $"In Progress p85. Body: {body}");
                Assert.That(inProgress[95], Is.EqualTo(8), $"In Progress p95. Body: {body}");

                Assert.That(review[50], Is.EqualTo(9), $"Review p50. Body: {body}");
                Assert.That(review[70], Is.EqualTo(12), $"Review p70. Body: {body}");
                Assert.That(review[85], Is.EqualTo(14), $"Review p85. Body: {body}");
                Assert.That(review[95], Is.EqualTo(15), $"Review p95. Body: {body}");

                Assert.That(test[50], Is.EqualTo(16), $"Test p50. Body: {body}");
                Assert.That(test[70], Is.EqualTo(20), $"Test p70. Body: {body}");
                Assert.That(test[85], Is.EqualTo(22), $"Test p85. Body: {body}");
                Assert.That(test[95], Is.EqualTo(24), $"Test p95. Body: {body}");
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_TeamWithCompletedItems_BandValuesRiseAcrossStatesInWorkflowOrder()
        {
            var teamId = SeedTeamWithKnownStateExitAges();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var orderedStates = OrderedStateNames(body);
                Assert.That(orderedStates, Is.EqualTo(new[] { InProgress, Review, Test }),
                    $"States must be returned in workflow order matching the chart X axis. Body: {body}");

                foreach (var percentile in new[] { 50, 70, 85, 95 })
                {
                    var inProgress = PercentilesForState(body, InProgress)[percentile];
                    var review = PercentilesForState(body, Review)[percentile];
                    var test = PercentilesForState(body, Test)[percentile];

                    Assert.That(review, Is.GreaterThan(inProgress),
                        $"Review p{percentile} ({review}) must exceed In Progress p{percentile} ({inProgress}) — cumulative age rises left to right. Body: {body}");
                    Assert.That(test, Is.GreaterThan(review),
                        $"Test p{percentile} ({test}) must exceed Review p{percentile} ({review}) — cumulative age rises left to right. Body: {body}");
                }
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_StateWithNoObservations_IsOmittedWhileOtherStatesRemain()
        {
            var teamId = SeedTeamWhereReviewHasNoObservations();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var states = OrderedStateNames(body);
                Assert.That(states, Does.Contain(InProgress),
                    $"In Progress has observations and must be present. Body: {body}");
                Assert.That(states, Does.Contain(Test),
                    $"Test has observations and must be present. Body: {body}");
                Assert.That(states, Does.Not.Contain(Review),
                    $"Review has zero completed-item observations and must be omitted (no empty band). Body: {body}");
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_TeamWithNoCompletedItemsInWindow_ReturnsEmptyArray()
        {
            var teamId = SeedTeamWithNoCompletedItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                using var document = JsonDocument.Parse(body);
                Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array),
                    $"Response must be a (possibly empty) array. Body: {body}");
                Assert.That(document.RootElement.GetArrayLength(), Is.Zero,
                    $"A team with no completed items in the window yields an empty band array. Body: {body}");
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var teamId = SeedTeamWithKnownStateExitAges();

            client.AsTeamAdmin(teamId);
            var url = $"/api/latest/teams/{teamId}/metrics/ageInStatePercentiles?startDate={windowEnd:O}&endDate={windowStart:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"startDate after endDate must be rejected with 400, mirroring cycleTimePercentiles validation. Body: {body}");
        }

        [Test]
        public async Task GetAgeInStatePercentiles_ItemClosedOutsideWindow_ContributesNoObservations()
        {
            var teamId = SeedTeamWhereOnlyItemIsClosedBeforeWindow();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                using var document = JsonDocument.Parse(body);
                Assert.That(document.RootElement.GetArrayLength(), Is.Zero,
                    $"Membership rule is ClosedDate within window; an item closed before the window contributes nothing. Body: {body}");
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_AnonymousCaller_IsRejected()
        {
            var teamId = SeedTeamWithKnownStateExitAges();

            client.AsAnonymous();
            var response = await client.GetAsync(PercentilesUrl(teamId));

            Assert.That(
                new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound },
                Does.Contain(response.StatusCode),
                $"An unauthenticated caller must not read team pace percentiles (RbacGuard TeamRead hides denied reads as 404). Status: {response.StatusCode}");
        }

        private string PercentilesUrl(int teamId)
        {
            return $"/api/latest/teams/{teamId}/metrics/ageInStatePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";
        }

        private int SeedTeamWithKnownStateExitAges()
        {
            // Ten completed items. For each item we record the cumulative total age at the
            // moment it exited each Doing state: ageAtExit = TransitionedAt - StartedDate.
            // The per-state observation sets are chosen so the nearest-rank percentiles
            // 50/70/85/95 (the existing PercentileCalculator) land on exact integers and
            // rise across In Progress < Review < Test.
            var inProgressAges = new[] { 1, 2, 2, 3, 3, 4, 5, 6, 7, 9 };
            var reviewAges = new[] { 4, 5, 6, 8, 8, 9, 11, 13, 14, 18 };
            var testAges = new[] { 9, 11, 12, 15, 15, 16, 19, 21, 23, 28 };

            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp, WorkTrackingSystems.AzureDevOps);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            for (var i = 0; i < 10; i++)
            {
                var startedDate = windowStart.AddDays(10 + i);
                var item = AddCompletedItem(workItemRepository, team, $"S-{i}", startedDate, closedAfterTestAgeDays: testAges[i]);

                AddExitTransition(transitionRepository, item, fromState: InProgress, toState: Review, ageAtExitDays: inProgressAges[i], startedDate);
                AddExitTransition(transitionRepository, item, fromState: Review, toState: Test, ageAtExitDays: reviewAges[i], startedDate);
                AddExitTransition(transitionRepository, item, fromState: Test, toState: "Done", ageAtExitDays: testAges[i], startedDate);
            }

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWhereReviewHasNoObservations()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp, WorkTrackingSystems.AzureDevOps);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            for (var i = 0; i < 4; i++)
            {
                var startedDate = windowStart.AddDays(10 + i);
                var item = AddCompletedItem(workItemRepository, team, $"NR-{i}", startedDate, closedAfterTestAgeDays: 12 + i);

                AddExitTransition(transitionRepository, item, fromState: InProgress, toState: Test, ageAtExitDays: 3 + i, startedDate);
                AddExitTransition(transitionRepository, item, fromState: Test, toState: "Done", ageAtExitDays: 12 + i, startedDate);
            }

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithNoCompletedItems()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp, WorkTrackingSystems.AzureDevOps);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();

            var inFlight = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = "WIP-1",
                Name = "Still in progress",
                Type = "Story",
                State = InProgress,
                StateCategory = StateCategories.Doing,
                CreatedDate = windowStart.AddDays(5),
                StartedDate = windowStart.AddDays(6),
                ClosedDate = null,
                Order = "WIP-1",
            };
            workItemRepository.Add(inFlight);
            workItemRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWhereOnlyItemIsClosedBeforeWindow()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp, WorkTrackingSystems.AzureDevOps);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            var startedBeforeWindow = windowStart.AddDays(-40);
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = "OLD-1",
                Name = "Closed before the window",
                Type = "Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = startedBeforeWindow.AddDays(-1),
                StartedDate = startedBeforeWindow,
                ClosedDate = windowStart.AddDays(-10),
                Order = "OLD-1",
            };
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            AddExitTransition(transitionRepository, item, fromState: InProgress, toState: "Done", ageAtExitDays: 5, startedBeforeWindow);
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private WorkItem AddCompletedItem(IWorkItemRepository repository, Team team, string referenceId, DateTime startedDate, int closedAfterTestAgeDays)
        {
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = startedDate.AddDays(closedAfterTestAgeDays),
                Order = referenceId,
            };
            repository.Add(item);
            repository.Save().GetAwaiter().GetResult();
            return item;
        }

        private static void AddExitTransition(IWorkItemStateTransitionRepository repository, WorkItem item, string fromState, string toState, int ageAtExitDays, DateTime startedDate)
        {
            repository.Add(new WorkItemStateTransition
            {
                WorkItemId = item.Id,
                FromState = fromState,
                ToState = toState,
                TransitionedAt = startedDate.AddDays(ageAtExitDays),
            });
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
                DoingStates = [InProgress, Review, Test],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team;
        }

        private static string[] OrderedStateNames(string body)
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var states = new List<string>();
            foreach (var entry in root.EnumerateArray())
            {
                states.Add(entry.GetProperty("state").GetString() ?? string.Empty);
            }
            return states.ToArray();
        }

        private static Dictionary<int, int> PercentilesForState(string body, string state)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                if (entry.GetProperty("state").GetString() != state)
                {
                    continue;
                }

                var byPercentile = new Dictionary<int, int>();
                foreach (var percentile in entry.GetProperty("percentiles").EnumerateArray())
                {
                    var key = percentile.GetProperty("percentile").GetInt32();
                    var value = (int)Math.Round(percentile.GetProperty("value").GetDouble());
                    byPercentile[key] = value;
                }
                return byPercentile;
            }

            Assert.Fail($"State '{state}' was expected in the response but was absent. Body: {body}");
            return new Dictionary<int, int>();
        }
    }
}
