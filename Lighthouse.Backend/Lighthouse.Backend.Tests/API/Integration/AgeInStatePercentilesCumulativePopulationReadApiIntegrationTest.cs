using System.Net;
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
    public class AgeInStatePercentilesCumulativePopulationReadApiIntegrationTest
    {
        private const string InProgress = "In Progress";
        private const string Review = "Review";
        private const string Test = "Test";
        private const string WaitingForFeedback = "Waiting for feedback";
        private const string Done = "Done";

        private const string RedScaffoldReason =
            "RED scaffold — pending #5145 Option A redesign; DELIVER un-ignores one at a time";

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
            windowEnd = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
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
        [TestCase(WorkTrackingSystems.AzureDevOps)]
        [TestCase(WorkTrackingSystems.Jira)]
        [TestCase(WorkTrackingSystems.Linear)]
        public async Task GetAgeInStatePercentiles_OldMetricNonMonotonicOnFasterDownstreamCohort_OptionACorrectsToRiseMonotonicallyAcrossEveryConnector(WorkTrackingSystems system)
        {
            var teamId = SeedFasterDownstreamCohort(system);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                foreach (var percentile in new[] { 50, 70, 85, 95 })
                {
                    var inProgress = PercentilesForState(body, InProgress)[percentile];
                    var review = PercentilesForState(body, Review)[percentile];
                    var test = PercentilesForState(body, Test)[percentile];

                    Assert.That(review, Is.GreaterThanOrEqualTo(inProgress),
                        $"[{system}] Review p{percentile} ({review}) must not fall below In Progress p{percentile} ({inProgress}) on a non-linear flow. Body: {body}");
                    Assert.That(test, Is.GreaterThanOrEqualTo(review),
                        $"[{system}] Test p{percentile} ({test}) must not fall below Review p{percentile} ({review}) even when a fast cohort raced through Test. Body: {body}");
                }
            }
        }

        [Test]
        [TestCase(WorkTrackingSystems.AzureDevOps)]
        [TestCase(WorkTrackingSystems.Jira)]
        [TestCase(WorkTrackingSystems.Linear)]
        public async Task GetAgeInStatePercentiles_AllItemsCloseFromLastMappedState_LastStateMatchesCycleTimePercentilesAcrossEveryConnector(WorkTrackingSystems system)
        {
            var teamId = SeedAllItemsClosingFromLastMappedState(system);

            client.AsTeamAdmin(teamId);
            var paceResponse = await client.GetAsync(PercentilesUrl(teamId));
            var cycleTimeResponse = await client.GetAsync(CycleTimeUrl(teamId));

            var paceBody = await paceResponse.Content.ReadAsStringAsync();
            var cycleTimeBody = await cycleTimeResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(paceResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), paceBody);
                Assert.That(cycleTimeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), cycleTimeBody);

                var lastState = PercentilesForState(paceBody, Test);
                var cycleTime = PercentilesByKey(cycleTimeBody);

                foreach (var percentile in new[] { 50, 70, 85, 95 })
                {
                    Assert.That(lastState[percentile], Is.EqualTo(cycleTime[percentile]),
                        $"[{system}] Last mapped state p{percentile} ({lastState[percentile]}) must equal the cycle-time line p{percentile} ({cycleTime[percentile]}) when every item closes from the last Doing state — the invariant ADR-047 mandates on every connector. Pace: {paceBody} CycleTime: {cycleTimeBody}");
                }
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_ItemsSkipReview_ReviewPopulationIncludesSkippersByImputation()
        {
            var teamId = SeedReviewSkippersAlongsideReviewVisitors();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var states = OrderedStateNames(body);
                Assert.That(states, Does.Contain(Review),
                    $"Review must be present: items that skipped it still reached at-or-after it and are imputed into its population — the skippers must not cause Review to be omitted. Body: {body}");

                var review = PercentilesForState(body, Review);
                Assert.That(review[70], Is.EqualTo(12),
                    $"Review p70 must equal 12 — a value that exists ONLY among the 4 imputed skippers (cumulative ages 12,13,14,15), all of which exceed every visitor's Review age (2,3,4,5). p70 over the 8-observation imputed population lands at 12; over the 4 visitors alone no percentile can reach 12, so this value uniquely proves the skippers were imputed into Review. Body: {body}");
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_ReworkItemReentersReview_ReviewRecordsOnlyTheLastExit()
        {
            var teamId = SeedSingleReworkItemWithTwoReviewExits();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var review = PercentilesForState(body, Review);
                using (Assert.EnterMultipleScope())
                {
                    foreach (var percentile in new[] { 50, 70, 85, 95 })
                    {
                        Assert.That(review[percentile], Is.EqualTo(15),
                            $"A reworked item that exits Review twice contributes exactly ONE observation — its LAST exit cumulative age (day 15), so every percentile equals 15. The current double-count keeps the first exit (day 5) too and drags p50 down to 5. Body: {body}");
                    }
                }
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_MisconfiguredDoingStatesOrder_BandsNeverDropBelowThePreviousState()
        {
            var teamId = SeedMisconfiguredDoingStatesOrder();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var states = OrderedStateNames(body);
                Assert.That(states, Is.EqualTo(new[] { Review, InProgress, Test }),
                    $"States must be returned in the configured (misconfigured) DoingStates order. Body: {body}");

                foreach (var percentile in new[] { 50, 70, 85, 95 })
                {
                    var first = PercentilesForState(body, Review)[percentile];
                    var dipping = PercentilesForState(body, InProgress)[percentile];
                    var last = PercentilesForState(body, Test)[percentile];

                    Assert.That(dipping, Is.GreaterThanOrEqualTo(first),
                        $"Misconfigured-order In Progress p{percentile} ({dipping}) must not fall below the preceding Review p{percentile} ({first}) — the band is clamped up to at least the previous state. Body: {body}");
                    Assert.That(last, Is.GreaterThanOrEqualTo(dipping),
                        $"Test p{percentile} ({last}) must not fall below the clamped In Progress p{percentile} ({dipping}). Body: {body}");
                    Assert.That(dipping, Is.EqualTo(first),
                        $"In Progress p{percentile} ({dipping}) genuinely sits below Review on this seed (exit-from-In-Progress age < exit-from-Review age); the clamp must raise it to EXACTLY the previous Review value ({first}), not leave the unclamped dip. Body: {body}");
                }
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_UnmappedStatusInTransitions_IsExcludedFromPacePath()
        {
            var teamId = SeedTeamWithUnmappedWaitingStatus();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(PercentilesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var states = OrderedStateNames(body);
                Assert.That(states, Does.Not.Contain(WaitingForFeedback),
                    $"An unmapped status must not appear in the pace path; only the team's mapped Doing states participate. Body: {body}");
                Assert.That(states, Does.Contain(Review),
                    $"Mapped Doing states still render. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_ForTeamWithUnmappedStatus_IsUnaffectedByThePacePathFilter()
        {
            var teamId = SeedTeamWithUnmappedWaitingStatus();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CumulativeStateTimeUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var cumulativeStates = CumulativeStateNames(body);
                Assert.That(cumulativeStates, Is.EqualTo(new[] { InProgress, Review, Test }),
                    $"The cumulative-state-time chart orders by the team's mapped Doing states and is untouched by the pace-path mapped-state filter. Body: {body}");
            }
        }

        private string PercentilesUrl(int teamId)
        {
            return $"/api/latest/teams/{teamId}/metrics/ageInStatePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";
        }

        private string CycleTimeUrl(int teamId)
        {
            return $"/api/latest/teams/{teamId}/metrics/cycleTimePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";
        }

        private string CumulativeStateTimeUrl(int teamId)
        {
            return $"/api/latest/teams/{teamId}/metrics/cumulativeStateTime?startDate={windowStart:O}&endDate={windowEnd:O}";
        }

        // old metric: Test p50 over the fast skip-Review cohort only (≈7) < Review p50 over the slow bulk (≈40) → non-monotonic; Option A: both percentiles run over the full reached-at-least population on one monotonic clock → monotonic.
        private int SeedFasterDownstreamCohort(WorkTrackingSystems system)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeamWithDoingStates(sp, system, [InProgress, Review, Test]);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            var lingeredThroughReview = new[] { 39, 40, 41 };
            foreach (var (reviewExit, index) in lingeredThroughReview.Select((value, i) => (value, i)))
            {
                var startedDate = windowStart.AddDays(10 + index);
                var testExit = reviewExit + 2;
                var item = AddCompletedItem(workItemRepository, team, $"SLOW-{index}", startedDate, closedAfterTestAgeDays: testExit);

                AddExitTransition(transitionRepository, item, InProgress, Review, 5 + index, startedDate);
                AddExitTransition(transitionRepository, item, Review, Test, reviewExit, startedDate);
                AddExitTransition(transitionRepository, item, Test, Done, testExit, startedDate);
            }

            var racedThroughTestSkippingReview = new[] { 1, 2, 3, 4, 5, 6, 7 };
            foreach (var (testExitDays, index) in racedThroughTestSkippingReview.Select((value, i) => (value, i)))
            {
                var startedDate = windowStart.AddDays(40 + index);
                var item = AddCompletedItem(workItemRepository, team, $"FAST-{index}", startedDate, closedAfterTestAgeDays: testExitDays + 1);

                AddExitTransition(transitionRepository, item, InProgress, Test, testExitDays, startedDate);
                AddExitTransition(transitionRepository, item, Test, Done, testExitDays + 1, startedDate);
            }

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedAllItemsClosingFromLastMappedState(WorkTrackingSystems system)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeamWithDoingStates(sp, system, [InProgress, Review, Test]);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            var closeAges = new[] { 9, 11, 12, 15, 15, 16, 19, 21, 23, 28 };
            for (var i = 0; i < closeAges.Length; i++)
            {
                var startedDate = windowStart.AddDays(10 + i);
                var closeAge = closeAges[i];
                var item = AddCompletedItem(workItemRepository, team, $"CT-{i}", startedDate, closedAfterTestAgeDays: closeAge);

                AddExitTransition(transitionRepository, item, InProgress, Review, 1 + i, startedDate);
                AddExitTransition(transitionRepository, item, Review, Test, 4 + i, startedDate);
                AddExitTransition(transitionRepository, item, Test, InProgress, 5 + i, startedDate);
                AddExitTransition(transitionRepository, item, InProgress, Test, 6 + i, startedDate);
                AddExitTransition(transitionRepository, item, Test, Done, closeAge, startedDate);
            }

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        // visitor Review ages = {2,3,4,5}; skippers have no Review exit so they impute from their earliest at-or-after exit (the Test exit) = {12,13,14,15}, each above every visitor age; with-imputation Review pop sorted = {2,3,4,5,12,13,14,15} → p70 = item[floor(0.7*8)-1=4] = 12; visitors-only pop = {2,3,4,5} can never yield 12 → p70==12 is the unique signal that imputation ran.
        private int SeedReviewSkippersAlongsideReviewVisitors()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeamWithDoingStates(sp, WorkTrackingSystems.AzureDevOps, [InProgress, Review, Test]);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            for (var i = 0; i < 4; i++)
            {
                var startedDate = windowStart.AddDays(10 + i);
                var reviewExit = 2 + i;
                var testExit = reviewExit + 4;
                var item = AddCompletedItem(workItemRepository, team, $"VISIT-{i}", startedDate, closedAfterTestAgeDays: testExit);

                AddExitTransition(transitionRepository, item, InProgress, Review, reviewExit - 1, startedDate);
                AddExitTransition(transitionRepository, item, Review, Test, reviewExit, startedDate);
                AddExitTransition(transitionRepository, item, Test, Done, testExit, startedDate);
            }

            for (var i = 0; i < 4; i++)
            {
                var startedDate = windowStart.AddDays(30 + i);
                var testExit = 12 + i;
                var item = AddCompletedItem(workItemRepository, team, $"SKIP-{i}", startedDate, closedAfterTestAgeDays: testExit);

                AddExitTransition(transitionRepository, item, InProgress, Test, testExit - 1, startedDate);
                AddExitTransition(transitionRepository, item, Test, Done, testExit, startedDate);
            }

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedSingleReworkItemWithTwoReviewExits()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeamWithDoingStates(sp, WorkTrackingSystems.AzureDevOps, [InProgress, Review, Test]);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            var startedDate = windowStart.AddDays(10);
            var item = AddCompletedItem(workItemRepository, team, "REWORK-1", startedDate, closedAfterTestAgeDays: 17);

            AddExitTransition(transitionRepository, item, InProgress, Review, 2, startedDate);
            AddExitTransition(transitionRepository, item, Review, InProgress, 4, startedDate);
            AddExitTransition(transitionRepository, item, InProgress, Review, 11, startedDate);
            AddExitTransition(transitionRepository, item, Review, Test, 14, startedDate);
            AddExitTransition(transitionRepository, item, Test, Done, 17, startedDate);

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        // misconfigured order [Review, In Progress, Test]: true per-state values are [Review=20, In Progress=10, Test=30] (exit-from-In-Progress age 10 < exit-from-Review age 20) → unclamped output dips at In Progress; DDD-1b clamps In Progress up to EXACTLY the previous Review value (20).
        private int SeedMisconfiguredDoingStatesOrder()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeamWithDoingStates(sp, WorkTrackingSystems.AzureDevOps, [Review, InProgress, Test]);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            for (var i = 0; i < 6; i++)
            {
                var startedDate = windowStart.AddDays(10 + i);
                var item = AddCompletedItem(workItemRepository, team, $"MISCFG-{i}", startedDate, closedAfterTestAgeDays: 30);

                AddExitTransition(transitionRepository, item, InProgress, Review, 10, startedDate);
                AddExitTransition(transitionRepository, item, Review, Test, 20, startedDate);
                AddExitTransition(transitionRepository, item, Test, Done, 30, startedDate);
            }

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithUnmappedWaitingStatus()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeamWithDoingStates(sp, WorkTrackingSystems.AzureDevOps, [InProgress, Review, Test]);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            for (var i = 0; i < 4; i++)
            {
                var startedDate = windowStart.AddDays(10 + i);
                var testExit = 18 + i;
                var item = AddCompletedItem(workItemRepository, team, $"UNMAPPED-{i}", startedDate, closedAfterTestAgeDays: testExit);

                AddExitTransition(transitionRepository, item, InProgress, Review, 3 + i, startedDate);
                AddExitTransition(transitionRepository, item, Review, WaitingForFeedback, 8 + i, startedDate);
                AddExitTransition(transitionRepository, item, WaitingForFeedback, Test, 12 + i, startedDate);
                AddExitTransition(transitionRepository, item, Test, Done, testExit, startedDate);
            }

            workItemRepository.Save().GetAwaiter().GetResult();
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
                State = Done,
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

        private static Team AddTeamWithDoingStates(IServiceProvider sp, WorkTrackingSystems system, List<string> doingStates)
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
                DoingStates = doingStates,
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team;
        }

        private static string[] OrderedStateNames(string body)
        {
            using var document = System.Text.Json.JsonDocument.Parse(body);
            var states = new List<string>();
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                states.Add(entry.GetProperty("state").GetString() ?? string.Empty);
            }
            return states.ToArray();
        }

        private static string[] CumulativeStateNames(string body)
        {
            using var document = System.Text.Json.JsonDocument.Parse(body);
            var states = new List<string>();
            foreach (var entry in document.RootElement.GetProperty("states").EnumerateArray())
            {
                states.Add(entry.GetProperty("state").GetString() ?? string.Empty);
            }
            return states.ToArray();
        }

        private static Dictionary<int, int> PercentilesForState(string body, string state)
        {
            using var document = System.Text.Json.JsonDocument.Parse(body);
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                if (entry.GetProperty("state").GetString() != state)
                {
                    continue;
                }

                return ReadPercentiles(entry.GetProperty("percentiles"));
            }

            Assert.Fail($"State '{state}' was expected in the response but was absent. Body: {body}");
            return new Dictionary<int, int>();
        }

        private static Dictionary<int, int> PercentilesByKey(string body)
        {
            using var document = System.Text.Json.JsonDocument.Parse(body);
            return ReadPercentiles(document.RootElement);
        }

        private static Dictionary<int, int> ReadPercentiles(System.Text.Json.JsonElement percentilesArray)
        {
            var byPercentile = new Dictionary<int, int>();
            foreach (var percentile in percentilesArray.EnumerateArray())
            {
                var key = percentile.GetProperty("percentile").GetInt32();
                var value = (int)Math.Round(percentile.GetProperty("value").GetDouble());
                byPercentile[key] = value;
            }
            return byPercentile;
        }
    }
}
