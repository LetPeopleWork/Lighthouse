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

        private static readonly string[] WorkflowDoingStates = [Analyzing, Building, Validating];

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
                Assert.That(orderedStates, Is.EqualTo(new[] { Analyzing, Building, Validating, Done }),
                    $"US-02: portfolio bars must come back one-per-state in workflow order, identical shape to the team scope. Body: {body}");

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
        [Ignore("pending — DELIVER step 02-02 (portfolio items drill-down endpoint)")]
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
                var perItemSum = SumDaysContributed(itemsBody);
                Assert.That(perItemSum, Is.EqualTo(barTotal).Within(DaysTolerance),
                    $"US-04 parity: portfolio drill-down rows sum to the Building bar height ({barTotal}d) within ±{DaysTolerance}d. Bar: {barBody} Items: {itemsBody}");
            }
        }

        [Test]
        [Ignore("pending — DELIVER step 02-03 (portfolio candidates endpoint)")]
        public async Task GetCumulativeStateTimeCandidates_PortfolioWindow_ReturnsIncludedFeaturesWithParentReferences()
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
            }
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

        private static double SumDaysContributed(string itemsBody)
        {
            using var document = JsonDocument.Parse(itemsBody);
            var total = 0.0;
            foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
            {
                total += item.GetProperty("daysContributed").GetDouble();
            }
            return total;
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
    }
}
