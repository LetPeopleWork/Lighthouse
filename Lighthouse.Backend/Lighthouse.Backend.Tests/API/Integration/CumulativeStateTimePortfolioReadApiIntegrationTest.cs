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
    public class CumulativeStateTimePortfolioReadApiIntegrationTest
    {
        private const string Analyzing = "Analyzing";
        private const string Building = "Building";
        private const string Validating = "Validating";
        private const string Done = "Done";

        private const double DaysTolerance = 0.1;
        private const int PortfolioWindowDisjointShiftDays = 10000;

        private static readonly string[] WorkflowDoingStates = [Analyzing, Building, Validating];
        private static readonly double[] BuildingDrillDownDaysDescending = [40.0, 30.0, 20.0, 10.0];

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;

        [SetUp]
        public void Init()
        {
            var offsetDays = (System.Threading.Interlocked.Increment(ref testDateOffset) * 400) + PortfolioWindowDisjointShiftDays;
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
        public async Task GetCumulativeStateTime_PortfolioWithKnownVisits_ReturnsSameShapeAsTeamScope()
        {
            var portfolioId = SeedPortfolioWithKnownVisitsAndInFlightFeatures();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(BarUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var orderedStates = OrderedStateNames(body);
                Assert.That(orderedStates, Is.EqualTo(WorkflowDoingStates),
                    $"D19/US-02: portfolio bars must come back one-per-Doing-state in workflow order, no Done-category bar, identical shape to the team scope. Body: {body}");

                var building = StateRow(body, Building);
                Assert.That(building.CompletedContributionDays, Is.EqualTo(60.0).Within(DaysTolerance),
                    $"US-02: completed-segment arithmetic mirrors the team scope on a comparable fixture. Body: {body}");
                Assert.That(building.OngoingContributionDays, Is.EqualTo(40.0).Within(DaysTolerance),
                    $"US-02: ongoing-segment arithmetic mirrors the team scope (one feature in flight 40d). Body: {body}");
                Assert.That(building.HasMedianDays, Is.True,
                    $"US-02: portfolio payload carries the same tooltip fields (medianDays present). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_PortfolioWithNoFeatures_ReturnsEmptyStatesArray()
        {
            var portfolioId = SeedPortfolioWithNoFeatures();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(BarUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                using var document = JsonDocument.Parse(body);
                Assert.That(document.RootElement.GetProperty("states").GetArrayLength(), Is.Zero,
                    $"US-02 parity: a portfolio with no features in the window yields an empty states array. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeItems_PortfolioPerItemDaysContributed_SumsToBarTotalForThatState()
        {
            var portfolioId = SeedPortfolioWithKnownVisitsAndInFlightFeatures();

            client.AsPortfolioAdmin(portfolioId);
            var barResponse = await client.GetAsync(BarUrl(portfolioId));
            var itemsResponse = await client.GetAsync(ItemsUrl(portfolioId, Building));

            var barBody = await barResponse.Content.ReadAsStringAsync();
            var itemsBody = await itemsResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(barResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), barBody);
                Assert.That(itemsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), itemsBody);

                var barTotal = StateRow(barBody, Building).TotalDays;
                var rows = ItemRows(itemsBody);
                var perItemSum = rows.Sum(row => row.DaysContributed);
                Assert.That(perItemSum, Is.EqualTo(barTotal).Within(DaysTolerance),
                    $"US-04 parity: portfolio drill-down rows sum to the Building bar height ({barTotal}d) within ±{DaysTolerance}d. Bar: {barBody} Items: {itemsBody}");

                var epic1 = rows.Single(row => row.ReferenceId == "EPIC-1");
                Assert.That(epic1.Title, Is.EqualTo("Epic EPIC-1"),
                    $"US-04 parity: portfolio drill-down 'Title' carries the feature Name. Items: {itemsBody}");
                Assert.That(epic1.Type, Is.EqualTo("Epic"),
                    $"US-04 parity: portfolio drill-down 'Work-Item Type' carries the feature Type. Items: {itemsBody}");
                Assert.That(epic1.State, Is.EqualTo(Done),
                    $"US-04 parity: portfolio drill-down 'Current State' carries the feature's current State. Items: {itemsBody}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_PortfolioWindow_ReturnsIncludedFeaturesWithoutParentReferenceId()
        {
            var portfolioId = SeedPortfolioWithKnownVisitsAndInFlightFeatures();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(CandidatesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                using var document = JsonDocument.Parse(body);
                Assert.That(document.RootElement.GetProperty("items").GetArrayLength(), Is.GreaterThan(0),
                    $"US-05 parity: portfolio candidate set is the D12-included features for the window. Body: {body}");
                Assert.That(CandidateRowsHaveParentReferenceId(body), Is.False,
                    $"D21 parity: portfolio candidate rows no longer expose a parentReferenceId field. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_PortfolioViewer_CanReadTheData()
        {
            var portfolioId = SeedPortfolioWithKnownVisitsAndInFlightFeatures();

            client.AsPortfolioViewer(portfolioId);
            var response = await client.GetAsync(BarUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"A portfolio Viewer (read role, not admin) must be able to read the cumulative state time chart (PortfolioRead guard). Body: {body}");
        }

        [Test]
        public async Task GetCumulativeStateTime_PortfolioAnonymousCaller_IsRejected()
        {
            var portfolioId = SeedPortfolioWithKnownVisitsAndInFlightFeatures();

            client.AsAnonymous();
            var response = await client.GetAsync(BarUrl(portfolioId));

            Assert.That(
                new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound },
                Does.Contain(response.StatusCode),
                $"An unauthenticated caller must not read portfolio cumulative state time (RbacGuard PortfolioRead, DDD-17). Status: {response.StatusCode}");
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_FeatureEnteringFirstStateExactlyAtWindowEnd_IsIncluded()
        {
            var portfolioId = SeedPortfolioWithSingleFeatureEnteringAtWindowEnd(out var boundaryReferenceId);

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(CandidatesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CandidateReferenceIds(body), Does.Contain(boundaryReferenceId),
                    $"A feature entering its first state exactly at windowEnd intersects the window (entry <= endDate inclusive). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_FeatureExitingFirstStateExactlyAtWindowStart_IsIncluded()
        {
            var portfolioId = SeedPortfolioWithSingleFeatureExitingAtWindowStart(out var boundaryReferenceId);

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(CandidatesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CandidateReferenceIds(body), Does.Contain(boundaryReferenceId),
                    $"A feature exiting its first state exactly at windowStart intersects the window (exit >= startDate inclusive). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_FeatureStartedAfterWindowEnd_IsExcluded()
        {
            var portfolioId = SeedPortfolioWithFeatureStartedAfterWindow(out var excludedReferenceId, out var includedReferenceId);

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(CandidatesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            var candidateReferenceIds = CandidateReferenceIds(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(candidateReferenceIds, Does.Contain(includedReferenceId),
                    $"The in-window feature stays a candidate. Body: {body}");
                Assert.That(candidateReferenceIds, Does.Not.Contain(excludedReferenceId),
                    $"A feature whose only visit starts after windowEnd has no overlap (entry <= endDate AND exit >= startDate both required). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_FeatureWithOneVisitBeforeAndOneInsideWindow_IsIncluded()
        {
            var portfolioId = SeedPortfolioWithFeatureSpanningIntoWindow(out var spanningReferenceId);

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(CandidatesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CandidateReferenceIds(body), Does.Contain(spanningReferenceId),
                    $"A feature with one visit before the window and one visit overlapping it must be included (any overlapping visit, not all). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_InFlightFeatureEnteredCurrentStateExactlyAtWindowEnd_IsIncluded()
        {
            var portfolioId = SeedPortfolioWithInFlightFeatureEnteringCurrentStateAtWindowEnd(out var boundaryReferenceId);

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(CandidatesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CandidateReferenceIds(body), Does.Contain(boundaryReferenceId),
                    $"An in-flight feature entering its current state exactly at windowEnd is in-flight-at-window-end (inclusive boundary). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_DoneFeatureWithCurrentTimestampButNoWindowOverlap_IsExcluded()
        {
            var portfolioId = SeedPortfolioWithExcludedDoneFeatureCarryingCurrentTimestamp(out var excludedReferenceId, out var includedReferenceId);

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(CandidatesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            var candidateReferenceIds = CandidateReferenceIds(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(candidateReferenceIds, Does.Contain(includedReferenceId),
                    $"The normal in-window feature stays a candidate. Body: {body}");
                Assert.That(candidateReferenceIds, Does.Not.Contain(excludedReferenceId),
                    $"A Done feature with a current timestamp but no window-overlapping transitions is not in-flight-at-window-end. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_FeatureBelongingToMultiplePortfolios_IsIncludedForEachPortfolio()
        {
            var portfolioId = SeedPortfolioWithFeatureSharedWithAnotherPortfolio(out var sharedReferenceId);

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(CandidatesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CandidateReferenceIds(body), Does.Contain(sharedReferenceId),
                    $"Portfolio membership is satisfied when the feature belongs to ANY matching portfolio, not only when all its portfolios match. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_FeaturesWithSyncedTransitionsButMissingTimestamps_DoNotCrashTheEndpoints()
        {
            var portfolioId = SeedPortfolioWithMalformedButRecoverableFeatures(out var healthyReferenceId);

            client.AsPortfolioAdmin(portfolioId);
            var barResponse = await client.GetAsync(BarUrl(portfolioId));
            var candidatesResponse = await client.GetAsync(CandidatesUrl(portfolioId));

            var barBody = await barResponse.Content.ReadAsStringAsync();
            var candidatesBody = await candidatesResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(barResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), barBody);
                Assert.That(candidatesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), candidatesBody);
                Assert.That(CandidateReferenceIds(candidatesBody), Does.Contain(healthyReferenceId),
                    $"The healthy feature is still resolved alongside the malformed ones. Body: {candidatesBody}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeItems_PortfolioBuildingDrillDown_OrdersRowsByDaysContributedDescending()
        {
            var portfolioId = SeedPortfolioWithKnownVisitsAndInFlightFeatures();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(ItemsUrl(portfolioId, Building));

            var body = await response.Content.ReadAsStringAsync();
            var contributions = ItemRows(body).Select(row => row.DaysContributed).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(contributions, Is.EqualTo(BuildingDrillDownDaysDescending).Within(DaysTolerance),
                    $"US-04 parity: portfolio drill-down rows are ordered by daysContributed descending. Body: {body}");
            }
        }

        private string BarUrl(int portfolioId)
            => $"/api/latest/portfolios/{portfolioId}/metrics/cumulativeStateTime?startDate={windowStart:O}&endDate={windowEnd:O}";

        private string ItemsUrl(int portfolioId, string state)
            => $"/api/latest/portfolios/{portfolioId}/metrics/cumulativeStateTime/items?state={Uri.EscapeDataString(state)}&startDate={windowStart:O}&endDate={windowEnd:O}";

        private string CandidatesUrl(int portfolioId)
            => $"/api/latest/portfolios/{portfolioId}/metrics/cumulativeStateTime/candidates?startDate={windowStart:O}&endDate={windowEnd:O}";

        private int SeedPortfolioWithKnownVisitsAndInFlightFeatures()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            var inWindow = windowStart.AddDays(20);

            // Three completed features walking Analyzing → Building → Validating → Done.
            // Building visits: 10, 20, 30 (sum 60), mirroring the team fixture's Review segment.
            AddCompletedFeature(featureRepository, transitionRepository, portfolio, "EPIC-1", inWindow,
                analyzingDays: 5, buildingDays: 10, validatingDays: 15);
            AddCompletedFeature(featureRepository, transitionRepository, portfolio, "EPIC-2", inWindow.AddDays(2),
                analyzingDays: 5, buildingDays: 20, validatingDays: 15);
            AddCompletedFeature(featureRepository, transitionRepository, portfolio, "EPIC-3", inWindow.AddDays(4),
                analyzingDays: 10, buildingDays: 30, validatingDays: 0);

            // One feature in flight in Building for 40 full days (ongoing segment of Building).
            AddInFlightFeature(featureRepository, portfolio, "EPIC-WIP", Building, currentStateEnteredAt: windowEnd.AddDays(-40));

            featureRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private int SeedPortfolioWithNoFeatures()
        {
            using var scope = factory.Services.CreateScope();
            var portfolio = AddPortfolio(scope.ServiceProvider);
            return portfolio.Id;
        }

        private void AddCompletedFeature(
            IRepository<Feature> featureRepository,
            IFeatureStateTransitionRepository transitionRepository,
            Portfolio portfolio,
            string referenceId,
            DateTime startedDate,
            int analyzingDays,
            int buildingDays,
            int validatingDays)
        {
            var analyzingExit = startedDate.AddDays(analyzingDays);
            var buildingExit = analyzingExit.AddDays(buildingDays);
            var validatingExit = buildingExit.AddDays(validatingDays);

            var feature = NewFeature(portfolio, referenceId, state: Done, category: StateCategories.Done,
                startedDate: startedDate, closedDate: validatingExit, currentStateEnteredAt: null);
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, feature, Analyzing, Building, analyzingExit);
            AddTransition(transitionRepository, feature, Building, Validating, buildingExit);
            if (validatingDays > 0)
            {
                AddTransition(transitionRepository, feature, Validating, Done, validatingExit);
            }
            else
            {
                AddTransition(transitionRepository, feature, Building, Done, buildingExit);
            }
        }

        private void AddInFlightFeature(IRepository<Feature> featureRepository, Portfolio portfolio, string referenceId, string state, DateTime currentStateEnteredAt)
        {
            var feature = NewFeature(portfolio, referenceId, state: state, category: StateCategories.Doing,
                startedDate: currentStateEnteredAt, closedDate: null, currentStateEnteredAt: currentStateEnteredAt);
            featureRepository.Add(feature);
        }

        private static Feature NewFeature(Portfolio portfolio, string referenceId, string state, StateCategories category, DateTime startedDate, DateTime? closedDate, DateTime? currentStateEnteredAt)
        {
            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Epic {referenceId}",
                Type = "Epic",
                State = state,
                StateCategory = category,
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = closedDate,
                CurrentStateEnteredAt = currentStateEnteredAt,
                Order = referenceId,
            };
            feature.Portfolios.Add(portfolio);
            return feature;
        }

        private static void AddTransition(IFeatureStateTransitionRepository repository, Feature feature, string fromState, string toState, DateTime transitionedAt)
        {
            repository.Add(new FeatureStateTransition
            {
                FeatureId = feature.Id,
                FromState = fromState,
                ToState = toState,
                TransitionedAt = transitionedAt,
            });
        }

        private static Portfolio AddPortfolio(IServiceProvider sp)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                ToDoStates = ["To Do"],
                DoingStates = [.. WorkflowDoingStates],
                DoneStates = [Done],
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            return portfolio;
        }

        private int SeedPortfolioWithSingleFeatureEnteringAtWindowEnd(out string boundaryReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            boundaryReferenceId = "ENTER-AT-END";
            var feature = NewFeature(portfolio, boundaryReferenceId, state: Done, category: StateCategories.Done,
                startedDate: windowEnd, closedDate: windowEnd.AddDays(5), currentStateEnteredAt: null);
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, feature, Analyzing, Done, windowEnd.AddDays(5));
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private int SeedPortfolioWithSingleFeatureExitingAtWindowStart(out string boundaryReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            boundaryReferenceId = "EXIT-AT-START";
            var feature = NewFeature(portfolio, boundaryReferenceId, state: Done, category: StateCategories.Done,
                startedDate: windowStart.AddDays(-10), closedDate: windowStart, currentStateEnteredAt: null);
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, feature, Analyzing, Done, windowStart);
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private int SeedPortfolioWithFeatureStartedAfterWindow(out string excludedReferenceId, out string includedReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            includedReferenceId = "IN-WINDOW";
            excludedReferenceId = "AFTER-WINDOW";

            AddCompletedFeature(featureRepository, transitionRepository, portfolio, includedReferenceId, windowStart.AddDays(20),
                analyzingDays: 5, buildingDays: 10, validatingDays: 0);

            var afterWindow = NewFeature(portfolio, excludedReferenceId, state: Done, category: StateCategories.Done,
                startedDate: windowEnd.AddDays(10), closedDate: windowEnd.AddDays(20), currentStateEnteredAt: null);
            featureRepository.Add(afterWindow);
            featureRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, afterWindow, Analyzing, Done, windowEnd.AddDays(20));
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private int SeedPortfolioWithFeatureSpanningIntoWindow(out string spanningReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            spanningReferenceId = "SPANNING";
            var feature = NewFeature(portfolio, spanningReferenceId, state: Done, category: StateCategories.Done,
                startedDate: windowStart.AddDays(-100), closedDate: windowStart.AddDays(10), currentStateEnteredAt: null);
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, feature, Analyzing, Building, windowStart.AddDays(-50));
            AddTransition(transitionRepository, feature, Building, Done, windowStart.AddDays(10));
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private int SeedPortfolioWithInFlightFeatureEnteringCurrentStateAtWindowEnd(out string boundaryReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();

            boundaryReferenceId = "INFLIGHT-AT-END";
            var feature = NewFeature(portfolio, boundaryReferenceId, state: Building, category: StateCategories.Doing,
                startedDate: windowEnd, closedDate: null, currentStateEnteredAt: windowEnd);
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private int SeedPortfolioWithExcludedDoneFeatureCarryingCurrentTimestamp(out string excludedReferenceId, out string includedReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            includedReferenceId = "DONE-INWINDOW";
            excludedReferenceId = "DONE-NO-OVERLAP";

            var inWindow = windowStart.AddDays(20);
            AddCompletedFeature(featureRepository, transitionRepository, portfolio, includedReferenceId, inWindow,
                analyzingDays: 5, buildingDays: 10, validatingDays: 0);

            var noOverlap = NewFeature(portfolio, excludedReferenceId, state: Done, category: StateCategories.Done,
                startedDate: windowStart.AddDays(-1), closedDate: null, currentStateEnteredAt: inWindow);
            noOverlap.StartedDate = null;
            featureRepository.Add(noOverlap);

            featureRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private int SeedPortfolioWithFeatureSharedWithAnotherPortfolio(out string sharedReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var otherPortfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            sharedReferenceId = "SHARED-EPIC";
            var inWindow = windowStart.AddDays(20);
            var feature = NewFeature(portfolio, sharedReferenceId, state: Done, category: StateCategories.Done,
                startedDate: inWindow, closedDate: inWindow.AddDays(15), currentStateEnteredAt: null);
            feature.Portfolios.Add(otherPortfolio);
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, feature, Analyzing, Building, inWindow.AddDays(5));
            AddTransition(transitionRepository, feature, Building, Done, inWindow.AddDays(15));
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private int SeedPortfolioWithMalformedButRecoverableFeatures(out string healthyReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            healthyReferenceId = "HEALTHY-1";
            var inWindow = windowStart.AddDays(20);
            AddCompletedFeature(featureRepository, transitionRepository, portfolio, healthyReferenceId, inWindow,
                analyzingDays: 5, buildingDays: 10, validatingDays: 0);

            var nullStartDone = NewFeature(portfolio, "NULLSTART-DONE", state: Done, category: StateCategories.Done,
                startedDate: inWindow, closedDate: inWindow.AddDays(5), currentStateEnteredAt: null);
            nullStartDone.StartedDate = null;
            featureRepository.Add(nullStartDone);

            var doingNoCurrent = NewFeature(portfolio, "DOING-NO-CURRENT", state: Building, category: StateCategories.Doing,
                startedDate: inWindow, closedDate: null, currentStateEnteredAt: null);
            featureRepository.Add(doingNoCurrent);

            featureRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, nullStartDone, Analyzing, Done, inWindow.AddDays(5));
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private static string[] CandidateReferenceIds(string body)
        {
            using var document = JsonDocument.Parse(body);
            var referenceIds = new List<string>();
            foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
            {
                referenceIds.Add(item.GetProperty("referenceId").GetString() ?? string.Empty);
            }
            return referenceIds.ToArray();
        }

        private static string[] OrderedStateNames(string body)
        {
            using var document = JsonDocument.Parse(body);
            var states = new List<string>();
            foreach (var entry in document.RootElement.GetProperty("states").EnumerateArray())
            {
                states.Add(entry.GetProperty("state").GetString() ?? string.Empty);
            }
            return states.ToArray();
        }

        private static bool CandidateRowsHaveParentReferenceId(string body)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
            {
                if (item.TryGetProperty("parentReferenceId", out _))
                {
                    return true;
                }
            }
            return false;
        }

        private static List<ItemRowView> ItemRows(string itemsBody)
        {
            using var document = JsonDocument.Parse(itemsBody);
            var rows = new List<ItemRowView>();
            foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
            {
                rows.Add(new ItemRowView(
                    ReferenceId: item.GetProperty("referenceId").GetString() ?? string.Empty,
                    Title: item.GetProperty("title").GetString() ?? string.Empty,
                    Type: item.GetProperty("type").GetString() ?? string.Empty,
                    State: item.GetProperty("state").GetString() ?? string.Empty,
                    DaysContributed: item.GetProperty("daysContributed").GetDouble()));
            }
            return rows;
        }

        private static StateRowView StateRow(string body, string state)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var entry in document.RootElement.GetProperty("states").EnumerateArray())
            {
                if (entry.GetProperty("state").GetString() != state)
                {
                    continue;
                }

                return new StateRowView(
                    TotalDays: entry.GetProperty("totalDays").GetDouble(),
                    CompletedContributionDays: entry.GetProperty("completedContributionDays").GetDouble(),
                    OngoingContributionDays: entry.GetProperty("ongoingContributionDays").GetDouble(),
                    HasMedianDays: entry.TryGetProperty("medianDays", out _));
            }

            Assert.Fail($"State '{state}' was expected in the response but was absent. Body: {body}");
            return default;
        }

        private readonly record struct StateRowView(
            double TotalDays,
            double CompletedContributionDays,
            double OngoingContributionDays,
            bool HasMedianDays);

        private readonly record struct ItemRowView(
            string ReferenceId,
            string Title,
            string Type,
            string State,
            double DaysContributed);
    }
}
