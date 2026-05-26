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
    public class AgeInStatePercentilesPortfolioReadApiIntegrationTest
    {
        private const string Analyzing = "Analyzing";
        private const string Building = "Building";
        private const string Validating = "Validating";

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
        public async Task GetAgeInStatePercentiles_PortfolioWithCompletedFeaturesAcrossThreeStates_ReturnsExactRisingPercentilesPerState()
        {
            var portfolioId = SeedPortfolioWithKnownStateExitAges();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(PercentilesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var orderedStates = OrderedStateNames(body);
                Assert.That(orderedStates, Is.EqualTo(new[] { Analyzing, Building, Validating }),
                    $"Portfolio states must be returned in workflow order. Body: {body}");

                var analyzing = PercentilesForState(body, Analyzing);
                var building = PercentilesForState(body, Building);
                var validating = PercentilesForState(body, Validating);

                Assert.That(analyzing[50], Is.EqualTo(6), $"Analyzing p50. Body: {body}");
                Assert.That(analyzing[95], Is.EqualTo(11), $"Analyzing p95. Body: {body}");
                Assert.That(building[50], Is.EqualTo(12), $"Building p50. Body: {body}");
                Assert.That(building[95], Is.EqualTo(21), $"Building p95. Body: {body}");
                Assert.That(validating[50], Is.EqualTo(21), $"Validating p50. Body: {body}");
                Assert.That(validating[95], Is.EqualTo(35), $"Validating p95. Body: {body}");

                foreach (var percentile in new[] { 50, 70, 85, 95 })
                {
                    Assert.That(building[percentile], Is.GreaterThan(analyzing[percentile]),
                        $"Building p{percentile} must exceed Analyzing — cumulative age rises left to right. Body: {body}");
                    Assert.That(validating[percentile], Is.GreaterThan(building[percentile]),
                        $"Validating p{percentile} must exceed Building. Body: {body}");
                }
            }
        }

        [Test]
        public async Task GetAgeInStatePercentiles_PortfolioWithNoCompletedFeaturesInWindow_ReturnsEmptyArray()
        {
            var portfolioId = SeedPortfolioWithNoCompletedFeatures();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(PercentilesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                using var document = JsonDocument.Parse(body);
                Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array),
                    $"Response must be a (possibly empty) array. Body: {body}");
                Assert.That(document.RootElement.GetArrayLength(), Is.Zero,
                    $"A portfolio with no completed features in the window yields an empty band array. Body: {body}");
            }
        }

        private string PercentilesUrl(int portfolioId)
        {
            return $"/api/latest/portfolios/{portfolioId}/metrics/ageInStatePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";
        }

        private int SeedPortfolioWithKnownStateExitAges()
        {
            var analyzingAges = new[] { 2, 3, 3, 4, 5, 6, 7, 9, 10, 12 };
            var buildingAges = new[] { 6, 7, 9, 10, 11, 13, 15, 18, 20, 24 };
            var validatingAges = new[] { 12, 14, 16, 18, 20, 23, 27, 30, 34, 38 };

            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            for (var i = 0; i < 10; i++)
            {
                var startedDate = windowStart.AddDays(10 + i);
                var feature = AddCompletedFeature(featureRepository, portfolio, $"EPIC-{i}", startedDate, closedAfterAgeDays: validatingAges[i]);

                AddExitTransition(transitionRepository, feature, fromState: Analyzing, toState: Building, ageAtExitDays: analyzingAges[i], startedDate);
                AddExitTransition(transitionRepository, feature, fromState: Building, toState: Validating, ageAtExitDays: buildingAges[i], startedDate);
                AddExitTransition(transitionRepository, feature, fromState: Validating, toState: "Done", ageAtExitDays: validatingAges[i], startedDate);
            }

            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private int SeedPortfolioWithNoCompletedFeatures()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();

            var inFlight = new Feature
            {
                ReferenceId = "EPIC-WIP",
                Name = "Still building",
                Type = "Epic",
                State = Building,
                StateCategory = StateCategories.Doing,
                CreatedDate = windowStart.AddDays(5),
                StartedDate = windowStart.AddDays(6),
                ClosedDate = null,
                Order = "EPIC-WIP",
            };
            inFlight.Portfolios.Add(portfolio);
            featureRepository.Add(inFlight);
            featureRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private Feature AddCompletedFeature(IRepository<Feature> repository, Portfolio portfolio, string referenceId, DateTime startedDate, int closedAfterAgeDays)
        {
            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Epic {referenceId}",
                Type = "Epic",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = startedDate.AddDays(closedAfterAgeDays),
                Order = referenceId,
            };
            feature.Portfolios.Add(portfolio);
            repository.Add(feature);
            repository.Save().GetAwaiter().GetResult();
            return feature;
        }

        private static void AddExitTransition(IFeatureStateTransitionRepository repository, Feature feature, string fromState, string toState, int ageAtExitDays, DateTime startedDate)
        {
            repository.Add(new FeatureStateTransition
            {
                FeatureId = feature.Id,
                FromState = fromState,
                ToState = toState,
                TransitionedAt = startedDate.AddDays(ageAtExitDays),
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
            foreach (var entry in document.RootElement.EnumerateArray())
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
