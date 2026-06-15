using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    // DISTILL scaffold for wait-states-flow-efficiency (Story #5173).
    // Black-box example-based ATs over WebApplicationFactory<Program> — the C#/TS Architecture-of-Reference
    // rows govern (see docs/architecture/atdd-infrastructure-policy.md): no Hypothesis/PBT, no state_delta
    // Universe (Python pilot only). The flowEfficiencyInfo endpoint + WaitStates field land in DELIVER, so
    // every test is [Ignore("pending — DELIVER (wait-states-flow-efficiency)")] (RED-by-skip, not Broken).
    // Assertions target the HTTP/JSON contract only — zero references to not-yet-existing production types,
    // so the project compiles green now and the suite is Skipped until DELIVER un-ignores one slice at a time.
    [TestFixture]
    public class FlowEfficiencyReadApiIntegrationTest
    {
        private const string PendingReason = "pending — DELIVER (wait-states-flow-efficiency)";

        private const string InProgress = "In Progress";
        private const string WaitingForReview = "Waiting for Review";
        private const string ReadyForTest = "Ready for Test";
        private const string BlockedExternal = "Blocked - External";
        private const string Done = "Done";

        private const double PercentTolerance = 0.6;

        // Doing-states for the canonical fixture: one active state + three idle/wait states.
        private static readonly string[] WorkflowDoingStates = [InProgress, WaitingForReview, ReadyForTest, BlockedExternal];

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
        public async Task GetFlowEfficiency_TeamWithKnownWaitTime_ReturnsActiveOverTotalDoingTimeAsPercent()
        {
            // US-01 / D2 / ADR-054: efficiency = activeDoingTime / totalDoingTime over the in-scope set.
            // Fixture: total Doing-time 540d, of which 356d are in wait states ("Waiting for Review" +
            // "Ready for Test") → active 184d → 184/540 = 34%. This is the single most-mis-implementable
            // rule in the feature; pin it against a known fixture.
            var teamId = SeedTeamWithKnownDoingAndWaitTime();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(InfoUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(IsConfigured(body), Is.True,
                    $"D3: with wait states configured, the tile reports IsConfigured=true. Body: {body}");
                Assert.That(HasDataInScope(body), Is.True,
                    $"D4: with Doing-time in scope, the tile reports HasDataInScope=true. Body: {body}");
                Assert.That(EfficiencyPercent(body), Is.EqualTo(34.0).Within(PercentTolerance),
                    $"D2: efficiency = active(184d) / total Doing(540d) = 34%. Body: {body}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_TeamWithRawStateMarkedWait_CountsThatStateAsWaitTime()
        {
            // US-01 / D11: a RAW Doing-state marked directly as a wait state counts its time as wait time.
            // "Waiting for Review" (90d) of 240d total Doing → active 150d → 150/240 = 62.5% ≈ 63%.
            var teamId = SeedTeamWithSingleRawWaitState();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(InfoUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(EfficiencyPercent(body), Is.EqualTo(62.5).Within(PercentTolerance),
                    $"D11: a raw state marked wait removes its 90d from active; 150/240 = 62.5%. Body: {body}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_TeamWithMappingNameMarkedWait_CountsAllUnderlyingRawStatesAsWaitTime()
        {
            // THE critical correctness AT — US-01 / D11 / ADR-056.
            // A State Mapping "Waiting" → ["Waiting for Review", "Blocked - External"] exists; the admin marks
            // the MAPPING NAME "Waiting" as a single wait state (one click, no enumeration). The efficiency
            // must expand it via GetRawStatesForCategory(WaitStates), so time in BOTH underlying raw states
            // counts as wait. Fixture: "Waiting for Review" 120d + "Blocked - External" 100d = 220d wait of
            // 400d total Doing → active 180d → 180/400 = 45%. If the expansion is skipped (literal "Waiting"
            // string match), wait would be 0d and efficiency a wrong 100%.
            var teamId = SeedTeamWithMappingNameMarkedAsWaitState();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(InfoUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(IsConfigured(body), Is.True,
                    $"D11: marking a mapping name as wait counts as configured. Body: {body}");
                Assert.That(EfficiencyPercent(body), Is.EqualTo(45.0).Within(PercentTolerance),
                    $"D11/ADR-056: 'Waiting' expands to both raw states' time (120+100=220d wait); 180/400 = 45%, NOT 100% from a literal match. Body: {body}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_WaitEntryOutsideDoingSet_ContributesNothingToTheDenominator()
        {
            // US-01 edge case / ADR-056 §3: a wait entry that resolves OUTSIDE the Doing set (here the
            // Done-state "Closed") contributes nothing — it is neither in the denominator nor counted as wait.
            // Fixture has the same 240d/90d-wait shape as the raw-wait case PLUS "Closed" in WaitStates;
            // the efficiency must be identical to ignoring "Closed" entirely (62.5%).
            var teamId = SeedTeamWithOutOfDoingWaitEntry();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(InfoUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(EfficiencyPercent(body), Is.EqualTo(62.5).Within(PercentTolerance),
                    $"ADR-056: 'Closed' is outside the Doing set, so it neither adds wait time nor enters the denominator; efficiency stays 62.5%. Body: {body}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_NoWaitStatesConfigured_ReportsNotConfiguredNeverHundredPercent()
        {
            // D3 / ADR-055: no wait states → IsConfigured=false (the "not configured" signal), NEVER a
            // misleading 100%. The two booleans are explicit contract flags, not magic sentinels.
            var teamId = SeedTeamWithDoingTimeButNoWaitStates();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(InfoUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(IsConfigured(body), Is.False,
                    $"D3: an unconfigured team reports IsConfigured=false (renders 'not configured'). Body: {body}");
                Assert.That(EfficiencyPercent(body), Is.Not.EqualTo(100.0).Within(PercentTolerance),
                    $"D3: an unconfigured team must NEVER read 100% efficiency. Body: {body}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_ZeroDoingTimeInScope_ReportsNoDataNeverDividesByZero()
        {
            // D4 / ADR-055: wait states ARE configured but there is zero Doing-time in scope → HasDataInScope
            // =false (the "no data in scope" signal, DISTINCT from D3's "not configured"), and no division error.
            var teamId = SeedTeamWithWaitStatesButNoDoingTimeInScope();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(InfoUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(IsConfigured(body), Is.True,
                    $"D4: wait states ARE configured here — this is the no-data case, not the not-configured case. Body: {body}");
                Assert.That(HasDataInScope(body), Is.False,
                    $"D4: zero Doing-time in scope reports HasDataInScope=false, distinct from D3. Body: {body}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_TileNeverFollowsThePicker_HasNoItemIdsParameter()
        {
            // D5 / D18 / ADR-055: the tile is whole-set ONLY. Passing itemIds must not narrow it — the
            // endpoint has no itemIds parameter, so the value is identical with and without a spurious itemIds.
            var teamId = SeedTeamWithKnownDoingAndWaitTime();

            client.AsTeamAdmin(teamId);
            var wholeSetResponse = await client.GetAsync(InfoUrl(teamId));
            var withSpuriousItemIdsResponse = await client.GetAsync($"{InfoUrl(teamId)}&itemIds=1&itemIds=2");

            var wholeSetBody = await wholeSetResponse.Content.ReadAsStringAsync();
            var withItemIdsBody = await withSpuriousItemIdsResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(wholeSetResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), wholeSetBody);
                Assert.That(withSpuriousItemIdsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), withItemIdsBody);
                Assert.That(EfficiencyPercent(withItemIdsBody), Is.EqualTo(EfficiencyPercent(wholeSetBody)).Within(PercentTolerance),
                    $"D5/D18: the tile never follows the picker; a stray itemIds param does not change the whole-set value. Whole-set: {wholeSetBody} With itemIds: {withItemIdsBody}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_StartDateAfterEndDate_ReturnsBadRequest()
        {
            // ADR-055: validation mirrors the established …Info tile pattern (400 on inverted dates).
            var teamId = SeedTeamWithKnownDoingAndWaitTime();

            client.AsTeamAdmin(teamId);
            var url = $"/api/latest/teams/{teamId}/metrics/flowEfficiencyInfo?startDate={windowEnd:O}&endDate={windowStart:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"ADR-055: startDate after endDate is rejected with 400, mirroring the wipOverviewInfo validation. Body: {body}");
        }

        [Test]
        public async Task GetFlowEfficiency_StartDateEqualsEndDate_IsAccepted()
        {
            var teamId = SeedTeamWithKnownDoingAndWaitTime();

            client.AsTeamAdmin(teamId);
            var url = $"/api/latest/teams/{teamId}/metrics/flowEfficiencyInfo?startDate={windowEnd:O}&endDate={windowEnd:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"ADR-055: an equal start/end date is a valid single-day window and must not be rejected (only startDate strictly after endDate is a 400). Body: {body}");
        }

        [Test]
        public async Task GetFlowEfficiency_TeamViewer_CanReadTheTile()
        {
            // ADR-055: class-level RbacGuard(TeamRead) — a Viewer (read role) can read the tile.
            var teamId = SeedTeamWithKnownDoingAndWaitTime();

            client.AsTeamViewer(teamId);
            var response = await client.GetAsync(InfoUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"A team Viewer must be able to read the Flow Efficiency tile (RbacGuard TeamRead). Body: {body}");
        }

        [Test]
        public async Task GetFlowEfficiency_AnonymousCaller_IsRejected()
        {
            // ADR-055: class-level RbacGuard(TeamRead) — an unauthenticated caller is rejected.
            var teamId = SeedTeamWithKnownDoingAndWaitTime();

            client.AsAnonymous();
            var response = await client.GetAsync(InfoUrl(teamId));

            Assert.That(
                new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound },
                Does.Contain(response.StatusCode),
                $"An unauthenticated caller must not read the team Flow Efficiency tile (RbacGuard TeamRead). Status: {response.StatusCode}");
        }

        [Test]
        public async Task PutTeamSettings_WithWaitStatesIncludingAMappingName_PersistsThemReadYourWrites()
        {
            // US-01 / D8 / ADR-056 §1: WaitStates rides the EXISTING settings endpoint as an additive field
            // (no new write endpoint). Black-box: PUT the settings JSON with a waitStates array containing a
            // raw state AND a mapping name, GET the settings back, assert the array round-trips (read-your-writes).
            var teamId = SeedTeamWithStateMappingWaitingGroup(out var connectionId);

            client.AsTeamAdmin(teamId);

            var putResponse = await PutTeamSettingsWithWaitStates(teamId, connectionId,
                waitStates: [WaitingForReview, "Waiting"]);
            var putBody = await putResponse.Content.ReadAsStringAsync();
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), putBody);

            var settingsResponse = await client.GetAsync($"/api/latest/teams/{teamId}/settings");
            var settingsBody = await settingsResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(settingsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), settingsBody);
                var persisted = StringArray(settingsBody, "waitStates");
                Assert.That(persisted, Does.Contain(WaitingForReview),
                    $"D8: a raw wait state must round-trip through the existing settings payload. Body: {settingsBody}");
                Assert.That(persisted, Does.Contain("Waiting"),
                    $"D11: a mapping-name wait state must round-trip too (entries are raw states OR mapping names). Body: {settingsBody}");
            }
        }

        [Test]
        public async Task GetFlowEfficiency_WithWaitStatesDefined_DoesNotChangeAnyOtherMetric()
        {
            // D9 / review action item #2: wait states are a labelling OVERLAY only — defining them must not
            // shift throughput, cycle-time, or age-in-state for the same team. Capture those metric bodies
            // before any wait state exists, then define wait states through the real settings PUT endpoint,
            // then re-capture: each other-metric body must be byte-identical before and after.
            var teamId = SeedTeamWithDoingTimeButNoWaitStates();

            client.AsTeamAdmin(teamId);

            var throughputBefore = await ReadBody(ThroughputUrl(teamId));
            var cycleTimeBefore = await ReadBody(CycleTimePercentilesUrl(teamId));
            var ageInStateBefore = await ReadBody(AgeInStatePercentilesUrl(teamId));

            var putResponse = await DefineWaitStatesViaSettings(teamId, [WaitingForReview, ReadyForTest]);
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await putResponse.Content.ReadAsStringAsync());

            var throughputAfter = await ReadBody(ThroughputUrl(teamId));
            var cycleTimeAfter = await ReadBody(CycleTimePercentilesUrl(teamId));
            var ageInStateAfter = await ReadBody(AgeInStatePercentilesUrl(teamId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(throughputAfter, Is.EqualTo(throughputBefore),
                    "D9: defining wait states must not change throughput — it is a labelling overlay only.");
                Assert.That(cycleTimeAfter, Is.EqualTo(cycleTimeBefore),
                    "D9: defining wait states must not change cycle-time percentiles.");
                Assert.That(ageInStateAfter, Is.EqualTo(ageInStateBefore),
                    "D9: defining wait states must not change age-in-state percentiles.");
            }
        }

        private async Task<string> ReadBody(string url)
        {
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return body;
        }

        private async Task<HttpResponseMessage> DefineWaitStatesViaSettings(int teamId, string[] waitStates)
        {
            // Read-modify-write the real settings JSON, mutating ONLY waitStates — this guarantees the only
            // configuration delta the team sees is the wait-states overlay, so a byte-identical other-metric
            // body afterwards proves the overlay changed nothing else (D9).
            var settingsBody = await ReadBody($"/api/latest/teams/{teamId}/settings");
            var payload = JsonNode.Parse(settingsBody)!.AsObject();
            payload["waitStates"] = new JsonArray(waitStates.Select(s => (JsonNode)s!).ToArray());

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/teams/{teamId}", content);
        }

        private string ThroughputUrl(int teamId)
            => $"/api/latest/teams/{teamId}/metrics/throughput?startDate={windowStart:O}&endDate={windowEnd:O}";

        private string CycleTimePercentilesUrl(int teamId)
            => $"/api/latest/teams/{teamId}/metrics/cycleTimePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";

        private string AgeInStatePercentilesUrl(int teamId)
            => $"/api/latest/teams/{teamId}/metrics/ageInStatePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";

        private string InfoUrl(int teamId)
            => $"/api/latest/teams/{teamId}/metrics/flowEfficiencyInfo?startDate={windowStart:O}&endDate={windowEnd:O}";

        private async Task<HttpResponseMessage> PutTeamSettingsWithWaitStates(int teamId, int connectionId, string[] waitStates)
        {
            // Built as raw JSON (JsonNode), NOT a typed DTO — the WaitStates property does not exist on
            // SettingsOwnerDtoBase until DELIVER, so referencing it would break compilation. The black-box
            // payload injects waitStates as a plain JSON array, mirroring the staleness-threshold settings test.
            var payload = new JsonObject
            {
                ["id"] = teamId,
                ["name"] = $"Team {teamId}",
                ["dataRetrievalValue"] = "project = TEST",
                ["workTrackingSystemConnectionId"] = connectionId,
                ["workItemTypes"] = new JsonArray("User Story", "Bug"),
                ["toDoStates"] = new JsonArray("To Do"),
                ["doingStates"] = new JsonArray(WorkflowDoingStates.Select(s => (JsonNode)s!).ToArray()),
                ["doneStates"] = new JsonArray(Done),
                ["blockedStates"] = new JsonArray(),
                ["blockedTags"] = new JsonArray(),
                ["serviceLevelExpectationProbability"] = 0,
                ["serviceLevelExpectationRange"] = 0,
                ["systemWIPLimit"] = 0,
                ["throughputHistory"] = 30,
                ["useFixedDatesForThroughput"] = false,
                ["featureWIP"] = 1,
                ["automaticallyAdjustFeatureWIP"] = false,
                ["doneItemsCutoffDays"] = 365,
                ["stalenessThresholdDays"] = 0,
                ["estimationCategoryValues"] = new JsonArray(),
                ["useNonNumericEstimation"] = false,
                ["stateMappings"] = new JsonArray(new JsonObject
                {
                    ["name"] = "Waiting",
                    ["states"] = new JsonArray(WaitingForReview, BlockedExternal),
                }),
                ["waitStates"] = new JsonArray(waitStates.Select(s => (JsonNode)s!).ToArray()),
            };

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/teams/{teamId}", content);
        }

        private int SeedTeamWithKnownDoingAndWaitTime()
        {
            // Total Doing 540d: InProgress 184d (active), Waiting for Review 200d, Ready for Test 156d
            // (wait 356d). Two wait states marked directly → 184/540 = 34%.
            return SeedTeam(
                waitStates: [WaitingForReview, ReadyForTest],
                stateMappings: [],
                visits: new[]
                {
                    (InProgress, 184.0),
                    (WaitingForReview, 200.0),
                    (ReadyForTest, 156.0),
                });
        }

        private int SeedTeamWithSingleRawWaitState()
        {
            // Total Doing 240d: InProgress 150d (active), Waiting for Review 90d (wait) → 150/240 = 62.5%.
            return SeedTeam(
                waitStates: [WaitingForReview],
                stateMappings: [],
                visits: new[]
                {
                    (InProgress, 150.0),
                    (WaitingForReview, 90.0),
                });
        }

        private int SeedTeamWithMappingNameMarkedAsWaitState()
        {
            // Mapping "Waiting" → ["Waiting for Review", "Blocked - External"]. Mark the MAPPING NAME as wait.
            // Total Doing 400d: InProgress 180d (active), Waiting for Review 120d + Blocked - External 100d
            // (wait 220d via expansion) → 180/400 = 45%.
            return SeedTeam(
                waitStates: ["Waiting"],
                stateMappings: [("Waiting", [WaitingForReview, BlockedExternal])],
                visits: new[]
                {
                    (InProgress, 180.0),
                    (WaitingForReview, 120.0),
                    (BlockedExternal, 100.0),
                });
        }

        private int SeedTeamWithOutOfDoingWaitEntry()
        {
            // Same 240d/90d-wait shape as the raw case, plus the Done-state "Closed" added to WaitStates.
            // "Closed" is outside the Doing set, so it neither adds wait nor enters the denominator → 62.5%.
            return SeedTeam(
                waitStates: [WaitingForReview, "Closed"],
                stateMappings: [],
                visits: new[]
                {
                    (InProgress, 150.0),
                    (WaitingForReview, 90.0),
                });
        }

        private int SeedTeamWithDoingTimeButNoWaitStates()
        {
            // Doing-time present, but WaitStates empty → "not configured" (D3).
            return SeedTeam(
                waitStates: [],
                stateMappings: [],
                visits: new[]
                {
                    (InProgress, 100.0),
                    (WaitingForReview, 80.0),
                });
        }

        private int SeedTeamWithWaitStatesButNoDoingTimeInScope()
        {
            // Wait states configured, but no items contribute any Doing-time in the window → "no data" (D4).
            return SeedTeam(
                waitStates: [WaitingForReview],
                stateMappings: [],
                visits: System.Array.Empty<(string, double)>());
        }

        private int SeedTeamWithStateMappingWaitingGroup(out int connectionId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var (team, connection) = AddTeamWithConfig(sp, stateMappings: [("Waiting", [WaitingForReview, BlockedExternal])]);
            connectionId = connection.Id;
            return team.Id;
        }

        private int SeedTeam(
            string[] waitStates,
            (string Name, string[] States)[] stateMappings,
            (string State, double Days)[] visits)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var (team, _) = AddTeamWithConfig(sp, stateMappings);

            // WaitStates is set on the seeded entity directly via reflection-free black-box: the computation
            // under test reads the persisted entity. Until DELIVER adds the WaitStates property, this seed
            // relies on the field existing — so the test is [Ignore]'d (RED-by-skip). Once the field lands,
            // DELIVER replaces this marker with the real assignment.
            ApplyWaitStates(team, waitStates);

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            foreach (var (state, days) in visits)
            {
                AddItemWithSingleDoingVisit(workItemRepository, transitionRepository, team, state, days);
            }

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private static void ApplyWaitStates(Team team, string[] waitStates)
        {
            team.WaitStates = [.. waitStates];
        }

        private void AddItemWithSingleDoingVisit(
            IWorkItemRepository workItemRepository,
            IWorkItemStateTransitionRepository transitionRepository,
            Team team,
            string doingState,
            double days)
        {
            var enter = windowStart.AddDays(10);
            var exit = enter.AddDays(days);

            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = $"ITEM-{Guid.NewGuid():N}",
                Name = "Story",
                Type = "Story",
                State = Done,
                StateCategory = StateCategories.Done,
                Url = "https://example.test/item",
                CreatedDate = enter.AddDays(-1),
                StartedDate = enter,
                ClosedDate = exit,
                CurrentStateEnteredAt = null,
                Order = "1",
            };
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, item, InProgress, doingState, enter);
            AddTransition(transitionRepository, item, doingState, Done, exit);
        }

        private static void AddTransition(IWorkItemStateTransitionRepository repository, WorkItem item, string fromState, string toState, DateTime transitionedAt)
        {
            repository.Add(new WorkItemStateTransition
            {
                WorkItemId = item.Id,
                FromState = fromState,
                ToState = toState,
                TransitionedAt = transitionedAt,
            });
        }

        private static (Team team, WorkTrackingSystemConnection connection) AddTeamWithConfig(IServiceProvider sp, (string Name, string[] States)[] stateMappings)
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
                ToDoStates = ["To Do"],
                DoingStates = [.. WorkflowDoingStates],
                DoneStates = [Done],
                StateMappings = stateMappings
                    .Select(m => new StateMapping { Name = m.Name, States = [.. m.States] })
                    .ToList(),
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return (team, connection);
        }

        private static bool IsConfigured(string body) => Bool(body, "isConfigured");

        private static bool HasDataInScope(string body) => Bool(body, "hasDataInScope");

        private static double EfficiencyPercent(string body) => Double(body, "efficiencyPercent");

        private static bool Bool(string body, string property)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty(property, out var prop)
                && prop.ValueKind == JsonValueKind.True;
        }

        private static double Double(string body, string property)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.Number
                ? prop.GetDouble()
                : double.NaN;
        }

        private static string[] StringArray(string body, string property)
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty(property, out var prop) || prop.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return prop.EnumerateArray()
                .Select(entry => entry.GetString() ?? string.Empty)
                .ToArray();
        }
    }
}
