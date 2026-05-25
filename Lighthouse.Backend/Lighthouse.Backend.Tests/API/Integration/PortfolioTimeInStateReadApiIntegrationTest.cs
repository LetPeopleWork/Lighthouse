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
    public class PortfolioTimeInStateReadApiIntegrationTest
    {
        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime asOfDate;
        private DateTime elevenDaysBeforeAsOf;

        [SetUp]
        public void Init()
        {
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 60;
            asOfDate = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            elevenDaysBeforeAsOf = asOfDate.Date.AddDays(-11).AddHours(10).AddMinutes(45);

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
        public async Task GetWip_PortfolioFeatureWithStateHistory_ExposesCurrentStateEnteredAtWithinOneDay()
        {
            var portfolioId = SeedPortfolioWithInProgressFeature(
                referenceId: "EPIC-42",
                state: "Active",
                currentStateEnteredAt: elevenDaysBeforeAsOf);

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(WipUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                var feature = SingleFeature(body);
                Assert.That(TryGetDateTime(feature, "currentStateEnteredAt", out var enteredAt), Is.True,
                    $"Portfolio feature JSON must carry currentStateEnteredAt for the Time-in-State column. Body: {body}");
                Assert.That((enteredAt.Date - elevenDaysBeforeAsOf.Date).Duration(), Is.LessThanOrEqualTo(TimeSpan.FromDays(1)),
                    $"The read-API must render the persisted feature CurrentStateEnteredAt within 1 day (connector-agnostic at this boundary; Feature-derivation correctness is covered at the capture layer in DELIVER). Body: {body}");
            }
        }

        [Test]
        public async Task GetWip_FeatureFirstObservedThisSync_CurrentStateEnteredAtIsNull()
        {
            var portfolioId = SeedPortfolioWithInProgressFeature(
                referenceId: "EPIC-99",
                state: "Active",
                currentStateEnteredAt: null);

            client.AsPortfolioAdmin(portfolioId);
            var response = await client.GetAsync(WipUrl(portfolioId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                var feature = SingleFeature(body);
                Assert.That(feature.TryGetProperty("currentStateEnteredAt", out var enteredAtProp), Is.True,
                    $"currentStateEnteredAt must be present on the feature contract even when null. Body: {body}");
                Assert.That(enteredAtProp.ValueKind, Is.EqualTo(JsonValueKind.Null),
                    $"A first-observed feature with no prior transition data must surface currentStateEnteredAt as null. Body: {body}");
            }
        }

        private string WipUrl(int portfolioId)
        {
            return $"/api/latest/portfolios/{portfolioId}/metrics/wip?asOfDate={asOfDate:O}";
        }

        private int SeedPortfolioWithInProgressFeature(string referenceId, string state, DateTime? currentStateEnteredAt)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

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

            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Epic {referenceId}",
                Type = "Epic",
                State = state,
                StateCategory = StateCategories.Doing,
                CreatedDate = elevenDaysBeforeAsOf.AddDays(-1),
                StartedDate = elevenDaysBeforeAsOf,
                ClosedDate = null,
                Order = referenceId,
                CurrentStateEnteredAt = currentStateEnteredAt,
            };
            feature.Portfolios.Add(portfolio);

            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        private static JsonElement SingleFeature(string body)
        {
            using var document = JsonDocument.Parse(body);
            var clone = document.RootElement.Clone();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(clone.ValueKind, Is.EqualTo(JsonValueKind.Array), $"Expected a feature array. Body: {body}");
                Assert.That(clone.GetArrayLength(), Is.EqualTo(1), $"Expected exactly one seeded in-progress feature. Body: {body}");
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
