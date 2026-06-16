using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
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
    public class WorkItemAgePercentilesPortfolioReadApiIntegrationTest
    {
        private const string Building = "Building";

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

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();
        }

        [OneTimeTearDown]
        public void OneTimeCleanup()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [SetUp]
        public void Init()
        {
            today = DateTime.UtcNow.Date;
            windowEnd = today;
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset);
            windowStart = today.AddDays(-180 - offsetDays);

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
                var metricsService = teardownScope.ServiceProvider.GetRequiredService<IPortfolioMetricsService>();
                foreach (var seededPortfolio in dbContext.Portfolios.ToList())
                {
                    metricsService.InvalidatePortfolioMetrics(seededPortfolio);
                }

                dbContext.Database.EnsureDeleted();
            }
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_PortfolioWithInProgressFeaturesOfKnownAges_ReturnsExactPercentilesOfThoseAges()
        {
            var portfolioId = SeedPortfolioWithKnownInProgressAges();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(PercentilesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var percentiles = PercentilesByKey(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(percentiles[50], Is.EqualTo(3), $"p50 of the current in-progress feature ages. Body: {body}");
                Assert.That(percentiles[70], Is.EqualTo(5), $"p70. Body: {body}");
                Assert.That(percentiles[85], Is.EqualTo(6), $"p85. Body: {body}");
                Assert.That(percentiles[95], Is.EqualTo(7), $"p95. Body: {body}");
            }
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_SameEndDateDifferentStartDate_ReturnsIdenticalPercentiles()
        {
            // Given the same portfolio, two requests with the SAME endDate but DIFFERENT startDate (D4 invariance)
            var portfolioId = SeedPortfolioWithKnownInProgressAges();

            client.AsPortfolioAdmin(portfolioId);
            var wideStart = windowEnd.AddDays(-365);
            var narrowStart = windowEnd.AddDays(-7);
            var wideResponse = await client.GetAsync(
                $"/api/latest/portfolios/{portfolioId}/metrics/workItemAgePercentiles?startDate={wideStart:O}&endDate={windowEnd:O}");
            var narrowResponse = await client.GetAsync(
                $"/api/latest/portfolios/{portfolioId}/metrics/workItemAgePercentiles?startDate={narrowStart:O}&endDate={windowEnd:O}");

            var wideBody = await wideResponse.Content.ReadAsStringAsync();
            var narrowBody = await narrowResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(wideResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), wideBody);
                Assert.That(narrowResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), narrowBody);
            }

            Assert.That(PercentilesByKey(wideBody), Is.EqualTo(PercentilesByKey(narrowBody)),
                $"Portfolio WIA is a current-WIP snapshot keyed on endDate only; startDate must NOT filter the population (D4). " +
                $"wideStart: {wideBody} narrowStart: {narrowBody}");
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_PortfolioWithNoInProgressFeatures_ReturnsGracefulZeroValuedSet()
        {
            // Given a portfolio with zero in-progress features (D6 empty-WIP path)
            var portfolioId = SeedPortfolioWithNoInProgressFeatures();

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(PercentilesUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var percentiles = PercentilesByKey(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(percentiles.Keys, Is.EquivalentTo(ExpectedPercentileKeys),
                    $"Empty WIP yields the four-entry set, never a crash. Body: {body}");
                Assert.That(percentiles.Values, Is.All.Zero,
                    $"Empty WIP yields all-zero percentile values. Body: {body}");
            }
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_AnonymousCaller_IsRejected()
        {
            var portfolioId = SeedPortfolioWithKnownInProgressAges();

            client.AsAnonymous();
            var response = await client.GetAsync(PercentilesUrl(portfolioId));

            Assert.That(
                DeniedStatusCodes,
                Does.Contain(response.StatusCode),
                $"An unauthenticated caller must not read portfolio WIP-age percentiles (class-level RbacGuard PortfolioRead). Status: {response.StatusCode}");
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var portfolioId = SeedPortfolioWithKnownInProgressAges();

            client.AsPortfolioAdmin(portfolioId);
            var url = $"/api/latest/portfolios/{portfolioId}/metrics/workItemAgePercentiles?startDate={windowEnd:O}&endDate={windowStart:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"startDate after endDate must be rejected with 400, mirroring cycleTimePercentiles validation. Body: {body}");
        }

        [Test]
        public async Task GetWorkItemAgePercentiles_StartDateEqualsEndDate_IsAccepted()
        {
            var portfolioId = SeedPortfolioWithKnownInProgressAges();

            client.AsPortfolioAdmin(portfolioId);
            var url = $"/api/latest/portfolios/{portfolioId}/metrics/workItemAgePercentiles?startDate={windowEnd:O}&endDate={windowEnd:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"startDate equal to endDate is a valid single-day window and must be accepted; only strictly-after is rejected. Body: {body}");
        }

        private string PercentilesUrl(int portfolioId)
        {
            return $"/api/latest/portfolios/{portfolioId}/metrics/workItemAgePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";
        }

        private int SeedPortfolioWithKnownInProgressAges()
        {
            var ages = new[] { 1, 2, 2, 3, 3, 4, 5, 6, 7, 9 };

            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();

            for (var i = 0; i < ages.Length; i++)
            {
                featureRepository.Add(InProgressFeatureAged(portfolio, $"EPIC-{i}", ages[i]));
            }

            featureRepository.Save().GetAwaiter().GetResult();
            return portfolio.Id;
        }

        private int SeedPortfolioWithNoInProgressFeatures()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp);
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();

            var startedLongAgo = today.AddDays(-30);
            var done = new Feature
            {
                ReferenceId = "EPIC-DONE",
                Name = "Already finished",
                Type = "Epic",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = startedLongAgo.AddDays(-1),
                StartedDate = startedLongAgo,
                ClosedDate = today.AddDays(-5),
                Order = "EPIC-DONE",
            };
            done.Portfolios.Add(portfolio);
            featureRepository.Add(done);
            featureRepository.Save().GetAwaiter().GetResult();
            return portfolio.Id;
        }

        private Feature InProgressFeatureAged(Portfolio portfolio, string referenceId, int ageDays)
        {
            var startedDate = today.AddDays(-(ageDays - 1));
            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Epic {referenceId}",
                Type = "Epic",
                State = Building,
                StateCategory = StateCategories.Doing,
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = null,
                Order = referenceId,
            };
            feature.Portfolios.Add(portfolio);
            return feature;
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
                DoingStates = [Building],
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            return portfolio;
        }

        private static Dictionary<int, int> PercentilesByKey(string body)
        {
            var looksLikeJsonArray = body.TrimStart().StartsWith('[');
            Assert.That(looksLikeJsonArray, Is.True,
                $"Response must be a JSON percentile array; a non-JSON body (e.g. the SPA fallback HTML) means the " +
                $"workItemAgePercentiles endpoint is not wired yet. Body: {body}");

            using var document = JsonDocument.Parse(body);
            var byPercentile = new Dictionary<int, int>();
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                var key = entry.GetProperty("percentile").GetInt32();
                byPercentile[key] = entry.GetProperty("value").GetInt32();
            }
            return byPercentile;
        }
    }
}
