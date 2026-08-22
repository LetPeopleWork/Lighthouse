using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
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
    [TestFixture]
    public class DeliveryArchiveClosurePinIntegrationTest
    {
        private const int TotalWorkItems = 10;
        private const int RemainingWorkItems = 3;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            licenseServiceMock = new Mock<ILicenseService>();
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
        public async Task Archive_WhenNoSnapshotWasEverRecorded_PinsOneCompleteRecord()
        {
            var deliveryId = await SeedDeliveryWithForecastableWork();

            var response = await Archive(deliveryId, null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

            var pins = await ClosureRecordsFor(deliveryId);
            var pin = pins.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pin.ArchivedOn, Is.EqualTo(Today));
                Assert.That(pin.TargetDateAtClosure, Is.Not.Null);
                Assert.That(pin.TotalWork, Is.EqualTo(TotalWorkItems));
                Assert.That(pin.DoneWork, Is.EqualTo(TotalWorkItems - RemainingWorkItems));
                Assert.That(pin.RemainingWork, Is.EqualTo(RemainingWorkItems));
                Assert.That(pin.LikelihoodPercentage, Is.Not.Null);
                Assert.That(pin.WhenDistributionJson, Is.Not.Null);
                Assert.That(pin.FeatureBreakdownJson, Is.Not.Null);
                Assert.That(pin.HasSufficientData, Is.True);
                Assert.That(pin.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
            }
        }

        [Test]
        public async Task ArchiveUnarchiveArchive_WithinOneDay_LeavesExactlyOnePin()
        {
            var deliveryId = await SeedDeliveryWithForecastableWork();

            await EnsureOk(Archive(deliveryId, null));
            await EnsureOk(Unarchive(deliveryId, null));
            await EnsureOk(Archive(deliveryId, null));

            Assert.That(await ClosureRecordsFor(deliveryId), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Unarchive_PutsTheDeliveryBackOnTheActiveListAndLeavesThePin()
        {
            var deliveryId = await SeedDeliveryWithForecastableWork();
            await EnsureOk(Archive(deliveryId, null));

            await EnsureOk(Unarchive(deliveryId, null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await ArchivedOnFor(deliveryId), Is.Null);
                Assert.That(await ClosureRecordsFor(deliveryId), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task Archive_ADeliveryThatIsAlreadyArchived_IsRefusedAsAConflict()
        {
            var deliveryId = await SeedDeliveryWithForecastableWork();
            await EnsureOk(Archive(deliveryId, null));

            var response = await Archive(deliveryId, null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), await response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task Archive_WithoutAPremiumLicense_IsRefused()
        {
            var deliveryId = await SeedDeliveryWithForecastableWork();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

            var response = await Archive(deliveryId, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(await ClosureRecordsFor(deliveryId), Is.Empty);
            }
        }

        [Test]
        public async Task Archive_CarryingATokenAnEditHasAlreadySuperseded_IsRefusedAsAConflict()
        {
            var deliveryId = await SeedDeliveryWithForecastableWork();
            var portfolioId = await PortfolioIdFor(deliveryId);
            var staleToken = await ConcurrencyTokenFor(portfolioId, deliveryId);

            await EnsureOk(RenameDelivery(deliveryId, "Renamed By Somebody Else", staleToken));

            var response = await Archive(deliveryId, staleToken);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), body);
                Assert.That(body, Does.Contain(ConcurrencyTokenTestHelpers.ConcurrencyConflictCode));
                Assert.That(await ArchivedOnFor(deliveryId), Is.Null);
            }
        }

        [Test]
        public async Task Delete_AnArchivedDelivery_StillRemovesItAndTakesThePinWithIt()
        {
            var deliveryId = await SeedDeliveryWithForecastableWork();
            await EnsureOk(Archive(deliveryId, null));

            var response = await client.DeleteAsync($"/api/latest/deliveries/{deliveryId}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(await ClosureRecordsFor(deliveryId), Is.Empty);
            }
        }

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

        private Task<HttpResponseMessage> Archive(int deliveryId, Guid? concurrencyToken)
        {
            return client.PostAsync(
                $"/api/latest/deliveries/{deliveryId}/archive",
                JsonContent.Create(new ArchiveDeliveryRequest { ConcurrencyToken = concurrencyToken }));
        }

        private Task<HttpResponseMessage> Unarchive(int deliveryId, Guid? concurrencyToken)
        {
            return client.PostAsync(
                $"/api/latest/deliveries/{deliveryId}/unarchive",
                JsonContent.Create(new ArchiveDeliveryRequest { ConcurrencyToken = concurrencyToken }));
        }

        private async Task<HttpResponseMessage> RenameDelivery(int deliveryId, string name, Guid concurrencyToken)
        {
            var featureIds = await FeatureIds();

            var request = new UpdateDeliveryRequest
            {
                Name = name,
                Date = Today.AddDays(30),
                FeatureIds = featureIds,
                SelectionMode = DeliverySelectionMode.Manual,
                ConcurrencyToken = concurrencyToken,
            };

            return await client.PutAsync($"/api/latest/deliveries/{deliveryId}", JsonContent.Create(request));
        }

        private async Task<Guid> ConcurrencyTokenFor(int portfolioId, int deliveryId)
        {
            var response = await client.GetAsync($"/api/latest/deliveries/portfolio/{portfolioId}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var delivery = JsonNode.Parse(body)!.AsArray()
                .Single(node => node!["id"]!.GetValue<int>() == deliveryId)!
                .AsObject();

            return ConcurrencyTokenTestHelpers.GetToken(delivery);
        }

        private async Task<int> SeedDeliveryWithForecastableWork()
        {
            var portfolioId = await SeedPortfolioWithOneForecastableFeature();
            var featureIds = await FeatureIds();

            var request = new UpdateDeliveryRequest
            {
                Name = "Q3 Launch",
                Date = Today.AddDays(30),
                FeatureIds = featureIds,
                SelectionMode = DeliverySelectionMode.Manual,
            };

            var response = await client.PostAsync(
                $"/api/latest/deliveries/portfolio/{portfolioId}", JsonContent.Create(request));
            response.EnsureSuccessStatusCode();

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Deliveries.Select(delivery => delivery.Id).SingleAsync();
        }

        private async Task<int> SeedPortfolioWithOneForecastableFeature()
        {
            using var scope = factory.Services.CreateScope();
            var provider = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var team = new Team { Name = "Team Alpha", WorkTrackingSystemConnection = connection };

            var teamRepository = provider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var feature = new Feature([(team, RemainingWorkItems, TotalWorkItems)]) { Name = "Checkout", Order = "11" };
            var simulation = new SimulationResult();
            simulation.SimulationResults[10] = 9000;
            simulation.SimulationResults[20] = 1000;
            feature.SetFeatureForecasts([new WhenForecast(simulation) { HasSufficientData = true, TeamId = team.Id }]);

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([feature]);

            var portfolioRepository = provider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolio.Id;
        }

        private async Task<List<int>> FeatureIds()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Features.Select(feature => feature.Id).ToListAsync();
        }

        private async Task<int> PortfolioIdFor(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Deliveries
                .Where(delivery => delivery.Id == deliveryId)
                .Select(delivery => delivery.PortfolioId)
                .SingleAsync();
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

        private async Task<List<DeliveryClosureRecord>> ClosureRecordsFor(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.DeliveryClosureRecords
                .Where(record => record.DeliveryId == deliveryId)
                .ToListAsync();
        }
    }
}
