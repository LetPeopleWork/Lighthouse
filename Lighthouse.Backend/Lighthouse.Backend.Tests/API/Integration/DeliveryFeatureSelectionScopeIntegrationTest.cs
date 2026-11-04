using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WorkItemRules;
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
    /// A Delivery's answer carries a forecast for every Feature it holds, so a Feature picked from
    /// another Portfolio would hand that Portfolio's forecast to somebody who may only read this one.
    /// That is why the refusal is asserted against what the Portfolio's Deliveries read back over
    /// HTTP, and not only against the status of the write.
    /// </summary>
    [TestFixture]
    public class DeliveryFeatureSelectionScopeIntegrationTest
    {
        private const string FeatureType = "Epic";
        private const string StoredName = "Q3 Launch";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;

        // The memo keeps notes per owner id for as long as the host lives, and every test's fresh
        // database numbers its owners from one again, so each test hands back the owners it seeded.
        private readonly List<(int OwnerId, OwnerType OwnerType)> ownersSeeded = [];

        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            rootFactory = new TestWebApplicationFactory<Program>();

            licenseServiceMock = new Mock<ILicenseService>();

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });
        }

        [SetUp]
        public void Init()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            ownersSeeded.Clear();
            licenseServiceMock.Reset();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            rootFactory.Clock.Reset();
            rootFactory.UseFreshDatabase();
            factory.ForgetTheCachedMetrics();

            client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public async Task LeaveTheHostIdle()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            try
            {
                await SharedHostTearDown.LeaveTheHostIdleOrFail(factory.Services);
            }
            finally
            {
                await SharedHostCleanup.ForgetOwners(factory.Services, ownersSeeded);

                using (var scope = factory.Services.CreateScope())
                {
                    await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeletedAsync();
                }

                client.Dispose();
            }
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            factory?.Dispose();
            rootFactory?.Dispose();
        }

        [Test]
        public async Task APortfolioAdminCannotPullAFeatureOfAnotherPortfolioIntoTheirDelivery()
        {
            var seeded = await SeedTwoPortfolios();
            client.AsPortfolioAdmin(seeded.OwnPortfolioId);

            var response = await CreateDelivery(seeded.OwnPortfolioId, [seeded.OwnFeatureId, seeded.ForeignFeatureId]);
            var whatTheAdminCanRead = await ReadDeliveriesOf(seeded.OwnPortfolioId);

            using (Assert.EnterMultipleScope())
            {
                await AssertRefusedLikeAnUnknownId(response, seeded.ForeignFeatureId);
                Assert.That(ForecastedFeatureIdsIn(whatTheAdminCanRead), Does.Not.Contain(seeded.ForeignFeatureId), whatTheAdminCanRead);
                Assert.That(await DeliveryCount(), Is.Zero, "a refused create stores nothing");
            }
        }

        [Test]
        public async Task ASystemAdminIsHeldToTheSameRule()
        {
            var seeded = await SeedTwoPortfolios();
            client.AsSystemAdmin();

            var response = await CreateDelivery(seeded.OwnPortfolioId, [seeded.ForeignFeatureId]);

            using (Assert.EnterMultipleScope())
            {
                await AssertRefusedLikeAnUnknownId(response, seeded.ForeignFeatureId);
                Assert.That(await DeliveryCount(), Is.Zero);
            }
        }

        [Test]
        public async Task AFeatureInNoPortfolioCannotBePicked()
        {
            var seeded = await SeedTwoPortfolios();
            await LeavePortfolio(seeded.SharedFeatureId, seeded.OwnPortfolioId);
            await LeavePortfolio(seeded.SharedFeatureId, seeded.OtherPortfolioId);
            client.AsPortfolioAdmin(seeded.OwnPortfolioId);

            var response = await CreateDelivery(seeded.OwnPortfolioId, [seeded.SharedFeatureId]);

            await AssertRefusedLikeAnUnknownId(response, seeded.SharedFeatureId);
        }

        [Test]
        public async Task AddingAFeatureOfAnotherPortfolioOnUpdateLeavesTheDeliveryAsStored()
        {
            var seeded = await SeedTwoPortfolios();
            client.AsPortfolioAdmin(seeded.OwnPortfolioId);
            var deliveryId = await CreateDeliveryHolding(seeded.OwnPortfolioId, [seeded.OwnFeatureId]);
            var stored = await StoredDelivery(deliveryId);

            var response = await UpdateDelivery(deliveryId, "Renamed", Today.AddDays(90), [seeded.OwnFeatureId, seeded.ForeignFeatureId]);
            var afterwards = await StoredDelivery(deliveryId);

            using (Assert.EnterMultipleScope())
            {
                await AssertRefusedLikeAnUnknownId(response, seeded.ForeignFeatureId);
                Assert.That(afterwards.Name, Is.EqualTo(StoredName));
                Assert.That(afterwards.Date, Is.EqualTo(stored.Date));
                Assert.That(afterwards.FeatureIds, Is.EquivalentTo(new[] { seeded.OwnFeatureId }));
            }
        }

        [Test]
        public async Task AFeatureSharedWithTheDeliverysPortfolioCanBePickedOnCreate()
        {
            var seeded = await SeedTwoPortfolios();
            client.AsPortfolioAdmin(seeded.OwnPortfolioId);

            var deliveryId = await CreateDeliveryHolding(seeded.OwnPortfolioId, [seeded.SharedFeatureId]);

            Assert.That((await StoredDelivery(deliveryId)).FeatureIds, Is.EquivalentTo(new[] { seeded.SharedFeatureId }));
        }

        [Test]
        public async Task AFeatureSharedWithTheDeliverysPortfolioCanBeAddedOnUpdate()
        {
            var seeded = await SeedTwoPortfolios();
            client.AsPortfolioAdmin(seeded.OwnPortfolioId);
            var deliveryId = await CreateDeliveryHolding(seeded.OwnPortfolioId, [seeded.OwnFeatureId]);

            var response = await UpdateDelivery(deliveryId, StoredName, Today.AddDays(30), [seeded.OwnFeatureId, seeded.SharedFeatureId]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                Assert.That(
                    (await StoredDelivery(deliveryId)).FeatureIds,
                    Is.EquivalentTo(new[] { seeded.OwnFeatureId, seeded.SharedFeatureId }));
            }
        }

        [Test]
        public async Task AnUpdateKeepsAStoredFeatureThatHasLeftThePortfolio()
        {
            var seeded = await SeedTwoPortfolios();
            client.AsPortfolioAdmin(seeded.OwnPortfolioId);
            var deliveryId = await CreateDeliveryHolding(seeded.OwnPortfolioId, [seeded.OwnFeatureId, seeded.SharedFeatureId]);
            await LeavePortfolio(seeded.SharedFeatureId, seeded.OwnPortfolioId);

            var response = await UpdateDelivery(deliveryId, "Renamed", Today.AddDays(30), [seeded.OwnFeatureId, seeded.SharedFeatureId]);
            var afterwards = await StoredDelivery(deliveryId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                Assert.That(afterwards.Name, Is.EqualTo("Renamed"));
                Assert.That(afterwards.FeatureIds, Is.EquivalentTo(new[] { seeded.OwnFeatureId, seeded.SharedFeatureId }));
            }
        }

        [Test]
        public async Task AFeatureThatLeftThePortfolioCannotBeAddedBackOnceDropped()
        {
            var seeded = await SeedTwoPortfolios();
            client.AsPortfolioAdmin(seeded.OwnPortfolioId);
            var deliveryId = await CreateDeliveryHolding(seeded.OwnPortfolioId, [seeded.OwnFeatureId, seeded.SharedFeatureId]);
            await LeavePortfolio(seeded.SharedFeatureId, seeded.OwnPortfolioId);
            await EnsureOk(UpdateDelivery(deliveryId, StoredName, Today.AddDays(30), [seeded.OwnFeatureId]));

            var response = await UpdateDelivery(deliveryId, StoredName, Today.AddDays(30), [seeded.OwnFeatureId, seeded.SharedFeatureId]);

            using (Assert.EnterMultipleScope())
            {
                await AssertRefusedLikeAnUnknownId(response, seeded.SharedFeatureId);
                Assert.That((await StoredDelivery(deliveryId)).FeatureIds, Is.EquivalentTo(new[] { seeded.OwnFeatureId }));
            }
        }

        [Test]
        public async Task ARuleBasedDeliveryIgnoresTheIdsInThePayloadAndReadsItsOwnPortfolio()
        {
            var seeded = await SeedTwoPortfolios();
            client.AsPortfolioAdmin(seeded.OwnPortfolioId);

            var response = await client.PostAsync(
                $"/api/latest/deliveries/portfolio/{seeded.OwnPortfolioId}",
                JsonContent.Create(new UpdateDeliveryRequest
                {
                    Name = StoredName,
                    Date = Today.AddDays(30),
                    FeatureIds = [seeded.ForeignFeatureId],
                    SelectionMode = DeliverySelectionMode.RuleBased,
                    Mode = WorkItemRuleSet.ModeAnd,
                    Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = RuleOperators.Equals, Value = FeatureType }],
                }));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                Assert.That(
                    (await StoredDelivery(await NewestDeliveryId())).FeatureIds,
                    Is.EquivalentTo(new[] { seeded.OwnFeatureId, seeded.SharedFeatureId }));
            }
        }

        private sealed record SeededPortfolios(
            int OwnPortfolioId, int OtherPortfolioId, int OwnFeatureId, int ForeignFeatureId, int SharedFeatureId);

        private sealed record DeliveryAsStored(string Name, DateTime Date, List<int> FeatureIds);

        private async Task<SeededPortfolios> SeedTwoPortfolios()
        {
            using var scope = factory.Services.CreateScope();
            var provider = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var team = new Team { Name = "Team Alpha", WorkTrackingSystemConnection = connection };

            var teamRepository = provider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var ownFeature = ForecastableFeature(team, "Checkout", "FTR-1");
            var foreignFeature = ForecastableFeature(team, "Payroll", "FTR-2");
            var sharedFeature = ForecastableFeature(team, "Single Sign-On", "FTR-3");

            var ownPortfolio = new Portfolio { Name = "Own Portfolio", WorkTrackingSystemConnection = connection };
            ownPortfolio.UpdateFeatures([ownFeature, sharedFeature]);

            var otherPortfolio = new Portfolio { Name = "Other Portfolio", WorkTrackingSystemConnection = connection };
            otherPortfolio.UpdateFeatures([foreignFeature, sharedFeature]);

            var portfolioRepository = provider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(ownPortfolio);
            portfolioRepository.Add(otherPortfolio);
            await portfolioRepository.Save();

            ownersSeeded.Add((ownPortfolio.Id, OwnerType.Portfolio));
            ownersSeeded.Add((otherPortfolio.Id, OwnerType.Portfolio));

            return new SeededPortfolios(ownPortfolio.Id, otherPortfolio.Id, ownFeature.Id, foreignFeature.Id, sharedFeature.Id);
        }

        private static Feature ForecastableFeature(Team team, string name, string referenceId)
        {
            var feature = new Feature([(team, 3, 10)]) { Name = name, ReferenceId = referenceId, Order = "11", Type = FeatureType };

            var simulation = new SimulationResult();
            simulation.SimulationResults[10] = 9000;
            simulation.SimulationResults[20] = 1000;
            feature.SetFeatureForecasts([new WhenForecast(simulation, DateTime.UtcNow) { HasSufficientData = true, TeamId = team.Id }]);

            return feature;
        }

        private async Task LeavePortfolio(int featureId, int portfolioId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var portfolio = await context.Portfolios.Include(p => p.Features).SingleAsync(p => p.Id == portfolioId);
            portfolio.Features.RemoveAll(feature => feature.Id == featureId);
            await context.SaveChangesAsync();
        }

        private Task<HttpResponseMessage> CreateDelivery(int portfolioId, List<int> featureIds)
        {
            return client.PostAsync(
                $"/api/latest/deliveries/portfolio/{portfolioId}",
                JsonContent.Create(new UpdateDeliveryRequest
                {
                    Name = StoredName,
                    Date = Today.AddDays(30),
                    FeatureIds = featureIds,
                    SelectionMode = DeliverySelectionMode.Manual,
                }));
        }

        private async Task<int> CreateDeliveryHolding(int portfolioId, List<int> featureIds)
        {
            await EnsureOk(CreateDelivery(portfolioId, featureIds));
            return await NewestDeliveryId();
        }

        private Task<HttpResponseMessage> UpdateDelivery(int deliveryId, string name, DateTime date, List<int> featureIds)
        {
            return client.PutAsync(
                $"/api/latest/deliveries/{deliveryId}",
                JsonContent.Create(new UpdateDeliveryRequest
                {
                    Name = name,
                    Date = date,
                    FeatureIds = featureIds,
                    SelectionMode = DeliverySelectionMode.Manual,
                }));
        }

        private async Task<string> ReadDeliveriesOf(int portfolioId)
        {
            var response = await client.GetAsync($"/api/latest/deliveries/portfolio/{portfolioId}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return body;
        }

        private static List<int> ForecastedFeatureIdsIn(string portfolioDeliveries)
        {
            return [.. JsonNode.Parse(portfolioDeliveries)!["active"]!.AsArray()
                .SelectMany(delivery => delivery!["featureLikelihoods"]!.AsArray())
                .Select(likelihood => likelihood!["featureId"]!.GetValue<int>())];
        }

        private static async Task AssertRefusedLikeAnUnknownId(HttpResponseMessage response, int featureId)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), body);
                Assert.That(body, Is.EqualTo($"Feature with ID {featureId} does not exist"));
            }
        }

        private static async Task EnsureOk(Task<HttpResponseMessage> call)
        {
            var response = await call;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        private DateTime Today
        {
            get
            {
                using var scope = factory.Services.CreateScope();
                return scope.ServiceProvider.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;
            }
        }

        private async Task<int> DeliveryCount()
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Deliveries.CountAsync();
        }

        private async Task<int> NewestDeliveryId()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Deliveries.MaxAsync(delivery => delivery.Id);
        }

        private async Task<DeliveryAsStored> StoredDelivery(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var delivery = await context.Deliveries.Include(d => d.Features).SingleAsync(d => d.Id == deliveryId);
            return new DeliveryAsStored(delivery.Name, delivery.Date, [.. delivery.Features.Select(feature => feature.Id)]);
        }
    }
}
