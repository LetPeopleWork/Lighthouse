using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.Archived;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// The one writer no screen can see. A Portfolio refresh reads its Deliveries, spends a while
    /// talking to the work tracking system, and only then writes back a re-matched Feature set - so
    /// somebody retiring a Delivery in the middle of that is holding the newer truth while the
    /// refresh is holding the older one. These tests pin what happens when the two meet.
    /// </summary>
    [TestFixture]
    public class ArchivedDeliveryStaleAggregateRaceIntegrationTest
    {
        private const string MatchedType = "Epic";

        private static readonly string[] TheFeatureItHeldWhenItWasRetired = ["Checkout"];

        private static readonly JsonSerializerOptions PayloadReadOptions =
            new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);
                });
            });

            client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var scope = factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task ARefreshHoldingADeliveryRetiredUnderIt_CannotWriteItsRematchedFeaturesBack()
        {
            var seeded = await SeedRuleBasedDeliveryHoldingOneOfTwoMatchingFeatures();

            using var refreshScope = factory.Services.CreateScope();
            var refresh = RefreshInFlight(refreshScope, seeded.PortfolioId);

            await EnsureOk(Archive(seeded.DeliveryId));

            refresh.RuleService.RecomputeRuleBasedDeliveries(refresh.Portfolio, refresh.Deliveries);

            Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => refresh.DeliveryRepository.Save(),
                "The refresh is holding a Delivery from before it was retired. Its write has to be refused, " +
                "or retiring a Delivery is only a request that nothing changes it rather than a guarantee.");

            var featureNames = await FeatureNamesOf(seeded.DeliveryId);
            var archivedOn = await ArchivedOnFor(seeded.DeliveryId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(featureNames, Is.EquivalentTo(TheFeatureItHeldWhenItWasRetired));
                Assert.That(archivedOn, Is.Not.Null);
            }
        }

        [Test]
        public async Task ARefreshThatLosesTheRace_SaysSoQuietlyAndCarriesOnWithoutReplayingTheWrite()
        {
            var seeded = await SeedRuleBasedDeliveryHoldingOneOfTwoMatchingFeatures();

            using var refreshScope = factory.Services.CreateScope();
            var refresh = RefreshInFlight(refreshScope, seeded.PortfolioId);

            await EnsureOk(Archive(seeded.DeliveryId));

            refresh.RuleService.RecomputeRuleBasedDeliveries(refresh.Portfolio, refresh.Deliveries);

            var saved = await refresh.DeliveryRepository.TrySaveRecomputedDeliveries();

            // Whatever the refresh does next writes through the same session, so if the refused write
            // were still sitting there it would be tried again - and this time nothing would be left
            // to refuse it, because the version it is holding has just been brought up to date.
            refresh.Portfolio.Name = "Renamed by the rest of the refresh";
            var portfolioRepository = refreshScope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();

            Assert.DoesNotThrowAsync(() => portfolioRepository.Save(),
                "A refresh that lost a Delivery to somebody else still has the rest of its work to finish.");

            var featureNames = await FeatureNamesOf(seeded.DeliveryId);
            var archivedOn = await ArchivedOnFor(seeded.DeliveryId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saved, Is.False, "Letting go of a Delivery is something the caller has to be able to see.");
                Assert.That(featureNames, Is.EquivalentTo(TheFeatureItHeldWhenItWasRetired));
                Assert.That(archivedOn, Is.Not.Null);
            }
        }

        [Test]
        public async Task ARefreshThatRematchesADeliverysFeatures_InvalidatesTheTokenAnOpenEditorIsHolding()
        {
            var seeded = await SeedRuleBasedDeliveryHoldingOneOfTwoMatchingFeatures();
            var tokenTheEditorIsHolding = await ConcurrencyTokenFor(seeded.PortfolioId, seeded.DeliveryId);

            using (var refreshScope = factory.Services.CreateScope())
            {
                var refresh = RefreshInFlight(refreshScope, seeded.PortfolioId);
                refresh.RuleService.RecomputeRuleBasedDeliveries(refresh.Portfolio, refresh.Deliveries);
                await refresh.DeliveryRepository.Save();
            }

            var response = await RenameDelivery(seeded.DeliveryId, "Renamed By The Editor", tokenTheEditorIsHolding);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), body);
                Assert.That(body, Does.Contain(ConcurrencyTokenTestHelpers.ConcurrencyConflictCode));
            }
        }

        [Test]
        public async Task ARetiredDelivery_IsNotAmongTheDeliveriesARefreshIsHanded()
        {
            var seeded = await SeedRuleBasedDeliveryHoldingOneOfTwoMatchingFeatures();
            await EnsureOk(Archive(seeded.DeliveryId));

            using var scope = factory.Services.CreateScope();
            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryRepository.GetRecordableByPortfolio(seeded.PortfolioId), Is.Empty);
                Assert.That(deliveryRepository.GetByPortfolioAsync(seeded.PortfolioId).ToList(), Has.Count.EqualTo(1),
                    "The Portfolio screen still has to be able to list a retired Delivery, so the plain read must keep returning it.");
            }
        }

        [Test]
        public void ADeliveryThatHasBeenRetired_CannotBePutIntoTheCollectionARefreshReadsFrom()
        {
            var retired = new Delivery("Q3 Launch", DateTime.UtcNow.AddDays(30), 1, TestToday.Ambient) { Id = 7 };
            retired.Archive(DateTime.UtcNow);

            Assert.Throws<ArgumentException>(() => new RecordableDeliveries([retired]));
        }

        [Test]
        public async Task RemovingAnArchivedDeliverysFeaturesFromThePortfolio_LeavesItsNumbersWhereTheyWere()
        {
            var seeded = await SeedRuleBasedDeliveryHoldingOneOfTwoMatchingFeatures();
            await EnsureOk(Archive(seeded.DeliveryId));

            var numbersAtClosure = await ArchivedRowFor(seeded.PortfolioId, seeded.DeliveryId);

            await EmptyThePortfolioOfFeatures(seeded.PortfolioId);

            var numbersNow = await ArchivedRowFor(seeded.PortfolioId, seeded.DeliveryId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(numbersAtClosure.TotalWork, Is.EqualTo(10));
                Assert.That(numbersNow.TotalWork, Is.EqualTo(numbersAtClosure.TotalWork));
                Assert.That(numbersNow.DoneWork, Is.EqualTo(numbersAtClosure.DoneWork));
                Assert.That(numbersNow.RemainingWork, Is.EqualTo(numbersAtClosure.RemainingWork));
                Assert.That(numbersNow.Progress, Is.EqualTo(numbersAtClosure.Progress));
                Assert.That(numbersNow.LikelihoodPercentage, Is.EqualTo(numbersAtClosure.LikelihoodPercentage));
            }
        }

        [Test]
        public async Task ARetiredDelivery_LeavesTheActiveListAndAppearsUnderTheRetiredOnes()
        {
            var seeded = await SeedRuleBasedDeliveryHoldingOneOfTwoMatchingFeatures();
            await EnsureOk(Archive(seeded.DeliveryId));

            var payload = await PortfolioDeliveries(seeded.PortfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(payload.Active, Is.Empty);
                Assert.That(payload.Archived, Has.Count.EqualTo(1));
                Assert.That(payload.Archived[0].Id, Is.EqualTo(seeded.DeliveryId));
                Assert.That(payload.Archived[0].Name, Is.EqualTo("Q3 Launch"));
                Assert.That(payload.Archived[0].ArchivedOn, Is.EqualTo(Today));
            }
        }

        private RefreshInFlightState RefreshInFlight(IServiceScope refreshScope, int portfolioId)
        {
            var provider = refreshScope.ServiceProvider;
            var portfolioRepository = provider.GetRequiredService<IRepository<Portfolio>>();
            var deliveryRepository = provider.GetRequiredService<IDeliveryRepository>();

            return new RefreshInFlightState(
                portfolioRepository.GetById(portfolioId)!,
                deliveryRepository,
                deliveryRepository.GetRecordableByPortfolio(portfolioId),
                provider.GetRequiredService<IDeliveryRuleService>());
        }

        private sealed record RefreshInFlightState(
            Portfolio Portfolio,
            IDeliveryRepository DeliveryRepository,
            RecordableDeliveries Deliveries,
            IDeliveryRuleService RuleService);

        private sealed record SeededDelivery(int PortfolioId, int DeliveryId);

        private DateTime Today
        {
            get
            {
                using var scope = factory.Services.CreateScope();
                return scope.ServiceProvider.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;
            }
        }

        private static async Task EnsureOk(Task<HttpResponseMessage> call)
        {
            var response = await call;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        private Task<HttpResponseMessage> Archive(int deliveryId)
        {
            return client.PostAsync(
                $"/api/latest/deliveries/{deliveryId}/archive",
                JsonContent.Create(new ArchiveDeliveryRequest()));
        }

        private async Task<HttpResponseMessage> RenameDelivery(int deliveryId, string name, Guid concurrencyToken)
        {
            var request = new UpdateDeliveryRequest
            {
                Name = name,
                Date = Today.AddDays(30),
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.Manual,
                ConcurrencyToken = concurrencyToken,
            };

            return await client.PutAsync($"/api/latest/deliveries/{deliveryId}", JsonContent.Create(request));
        }

        private async Task<PortfolioDeliveriesDto> PortfolioDeliveries(int portfolioId)
        {
            var response = await client.GetAsync($"/api/latest/deliveries/portfolio/{portfolioId}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return JsonSerializer.Deserialize<PortfolioDeliveriesDto>(body, PayloadReadOptions)!;
        }

        private async Task<ArchivedDeliveryDto> ArchivedRowFor(int portfolioId, int deliveryId)
        {
            var payload = await PortfolioDeliveries(portfolioId);
            return payload.Archived.Single(delivery => delivery.Id == deliveryId);
        }

        private async Task<Guid> ConcurrencyTokenFor(int portfolioId, int deliveryId)
        {
            var payload = await PortfolioDeliveries(portfolioId);
            return payload.Active.Single(delivery => delivery.Id == deliveryId).ConcurrencyToken;
        }

        private async Task<List<string>> FeatureNamesOf(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var delivery = await context.Deliveries
                .Include(d => d.Features)
                .SingleAsync(d => d.Id == deliveryId);

            return [.. delivery.Features.Select(feature => feature.Name)];
        }

        private async Task<DateTime?> ArchivedOnFor(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Deliveries
                .Where(delivery => delivery.Id == deliveryId)
                .Select(delivery => delivery.ArchivedOn)
                .SingleAsync();
        }

        private async Task EmptyThePortfolioOfFeatures(int portfolioId)
        {
            using var scope = factory.Services.CreateScope();
            var portfolioRepository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var portfolio = portfolioRepository.GetById(portfolioId)!;

            portfolio.UpdateFeatures([]);
            await portfolioRepository.Save();
        }

        private async Task<SeededDelivery> SeedRuleBasedDeliveryHoldingOneOfTwoMatchingFeatures()
        {
            using var scope = factory.Services.CreateScope();
            var provider = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var team = new Team { Name = "Team Alpha", WorkTrackingSystemConnection = connection };

            var teamRepository = provider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var checkout = new Feature([(team, 3, 10)]) { Name = "Checkout", Order = "11", Type = MatchedType };
            var payments = new Feature([(team, 4, 12)]) { Name = "Payments", Order = "12", Type = MatchedType };

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([checkout, payments]);

            var portfolioRepository = provider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            var ruleSet = new WorkItemRuleSet
            {
                Version = WorkItemRuleSet.SchemaVersion,
                Conditions = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = MatchedType }],
            };

            var delivery = new Delivery("Q3 Launch", DateTime.UtcNow.AddDays(30), portfolio.Id, TestToday.Ambient)
            {
                SelectionMode = DeliverySelectionMode.RuleBased,
                RuleDefinitionJson = JsonSerializer.Serialize(ruleSet),
                RuleSchemaVersion = WorkItemRuleSet.SchemaVersion,
            };

            // Deliberately holding only one of the two Features the rule matches, so a re-match is a
            // real change to the Delivery rather than a write of the values it already has.
            delivery.ReplaceFeatures([checkout]);

            var deliveryRepository = provider.GetRequiredService<IDeliveryRepository>();
            deliveryRepository.Add(delivery);
            await deliveryRepository.Save();

            return new SeededDelivery(portfolio.Id, delivery.Id);
        }
    }
}
