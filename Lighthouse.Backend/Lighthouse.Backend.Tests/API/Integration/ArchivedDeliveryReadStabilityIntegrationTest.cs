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
    /// <summary>
    /// The promise the whole idea of a retired Delivery rests on: it reads the same after the Features
    /// underneath it have moved. A refresh that changed nothing would only show that a function is
    /// deterministic, which nobody doubted - so the refresh here removes one Feature, renames another,
    /// moves a third's counts and brings a fourth into the Portfolio.
    /// </summary>
    [TestFixture]
    public class ArchivedDeliveryReadStabilityIntegrationTest
    {
        private const string RemovedFeatureName = "Checkout";
        private const string RenamedFeatureName = "Search";
        private const string RecountedFeatureName = "Payments";

        private static readonly string[] FeatureNamesAtClosure = [RemovedFeatureName, RenamedFeatureName, RecountedFeatureName];

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
        public async Task ArchivedDelivery_ReadEitherSideOfARefreshThatChangesItsFeatures_ReadsIdentically()
        {
            var (portfolioId, deliveryId) = await SeedAndArchiveDelivery();
            var firstReading = await ArchivedRowJson(portfolioId, deliveryId);

            await RefreshPortfolioChangingTheFeaturesUnderneath();
            var secondReading = await ArchivedRowJson(portfolioId, deliveryId);

            Assert.That(secondReading, Is.EqualTo(firstReading));
        }

        [Test]
        public async Task ArchivedDelivery_ReadAfterFourRefreshes_StillReadsAsItDidAtClosure()
        {
            var (portfolioId, deliveryId) = await SeedAndArchiveDelivery();
            var firstReading = await ArchivedRowJson(portfolioId, deliveryId);

            for (var refresh = 0; refresh < 4; refresh++)
            {
                await RefreshPortfolioChangingTheFeaturesUnderneath();
                Assert.That(await ArchivedRowJson(portfolioId, deliveryId), Is.EqualTo(firstReading), $"after refresh {refresh + 1}");
            }
        }

        /// <summary>
        /// Guards the two tests above against passing because nothing moved. If the refresh stopped
        /// changing the Features, they would pass while proving nothing.
        /// </summary>
        [Test]
        public async Task RefreshUsedByTheseTests_ActuallyMovesTheFeaturesTheDeliveryClosedWith()
        {
            var (_, deliveryId) = await SeedAndArchiveDelivery();

            await RefreshPortfolioChangingTheFeaturesUnderneath();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await LiveFeatureNamesOf(deliveryId), Is.Not.EquivalentTo(FeatureNamesAtClosure));
                Assert.That(await LiveFeatureNamesOf(deliveryId), Does.Not.Contain(RemovedFeatureName));
                Assert.That(await PortfolioFeatureNames(), Has.Some.StartsWith("Reporting"));
            }
        }

        [Test]
        public async Task ArchivedDelivery_AfterTheRefresh_StillListsEveryFeatureRowItClosedWith()
        {
            var (portfolioId, deliveryId) = await SeedAndArchiveDelivery();

            await RefreshPortfolioChangingTheFeaturesUnderneath();

            var rows = (await ArchivedRow(portfolioId, deliveryId))["featureBreakdown"]!.AsArray();
            Assert.That(
                rows.Select(row => row!["name"]!.GetValue<string>()),
                Is.EquivalentTo(FeatureNamesAtClosure));
        }

        private async Task<(int PortfolioId, int DeliveryId)> SeedAndArchiveDelivery()
        {
            var portfolioId = await SeedPortfolioWithThreeForecastableFeatures();
            var featureIds = await FeatureIds();

            var create = await client.PostAsync(
                $"/api/latest/deliveries/portfolio/{portfolioId}",
                JsonContent.Create(new UpdateDeliveryRequest
                {
                    Name = "Q3 Launch",
                    Date = Today.AddDays(30),
                    FeatureIds = featureIds,
                    SelectionMode = DeliverySelectionMode.Manual,
                }));
            create.EnsureSuccessStatusCode();

            int deliveryId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                deliveryId = await context.Deliveries.Select(delivery => delivery.Id).SingleAsync();
            }

            var archive = await client.PostAsync(
                $"/api/latest/deliveries/{deliveryId}/archive",
                JsonContent.Create(new ArchiveDeliveryRequest()));
            Assert.That(archive.StatusCode, Is.EqualTo(HttpStatusCode.OK), await archive.Content.ReadAsStringAsync());

            return (portfolioId, deliveryId);
        }

        private async Task RefreshPortfolioChangingTheFeaturesUnderneath()
        {
            using var scope = factory.Services.CreateScope();
            var provider = scope.ServiceProvider;
            var portfolioRepository = provider.GetRequiredService<IRepository<Portfolio>>();
            var context = provider.GetRequiredService<LighthouseAppContext>();

            var portfolio = portfolioRepository.GetAll().Single();
            var team = portfolio.Features.SelectMany(feature => feature.FeatureWork).Select(work => work.Team).First();

            var gone = portfolio.Features.SingleOrDefault(feature => feature.Name == RemovedFeatureName);
            if (gone is not null)
            {
                portfolio.Features.Remove(gone);
                context.Features.Remove(gone);
            }

            var renamed = portfolio.Features.SingleOrDefault(feature => feature.Name == RenamedFeatureName);
            if (renamed is not null)
            {
                renamed.Name = $"{RenamedFeatureName} (renamed by the refresh)";
            }

            foreach (var work in portfolio.Features.Where(feature => feature.Name == RecountedFeatureName).SelectMany(feature => feature.FeatureWork))
            {
                work.TotalWorkItems += 7;
                work.RemainingWorkItems += 5;
            }

            var arrivalNumber = portfolio.Features.Count + 1;
            portfolio.Features.Add(ForecastableFeature(team, $"Reporting {arrivalNumber}", $"FTR-NEW-{arrivalNumber}"));

            await portfolioRepository.Save();
        }

        private async Task<int> SeedPortfolioWithThreeForecastableFeatures()
        {
            using var scope = factory.Services.CreateScope();
            var provider = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var team = new Team { Name = "Team Alpha", WorkTrackingSystemConnection = connection };

            var teamRepository = provider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([
                ForecastableFeature(team, RemovedFeatureName, "FTR-1"),
                ForecastableFeature(team, RenamedFeatureName, "FTR-2"),
                ForecastableFeature(team, RecountedFeatureName, "FTR-3"),
            ]);

            var portfolioRepository = provider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolio.Id;
        }

        private static Feature ForecastableFeature(Team team, string name, string referenceId)
        {
            var feature = new Feature([(team, 3, 10)]) { Name = name, ReferenceId = referenceId, Order = "11" };

            var simulation = new SimulationResult();
            simulation.SimulationResults[10] = 9000;
            simulation.SimulationResults[20] = 1000;
            feature.SetFeatureForecasts([new WhenForecast(simulation) { HasSufficientData = true, TeamId = team.Id }]);

            return feature;
        }

        private DateTime Today
        {
            get
            {
                using var scope = factory.Services.CreateScope();
                return scope.ServiceProvider.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;
            }
        }

        private async Task<string> ArchivedRowJson(int portfolioId, int deliveryId)
        {
            return (await ArchivedRow(portfolioId, deliveryId)).ToJsonString();
        }

        private async Task<JsonObject> ArchivedRow(int portfolioId, int deliveryId)
        {
            var response = await client.GetAsync($"/api/latest/deliveries/portfolio/{portfolioId}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return JsonNode.Parse(body)!["archived"]!.AsArray()
                .Single(node => node!["id"]!.GetValue<int>() == deliveryId)!
                .AsObject();
        }

        private async Task<List<int>> FeatureIds()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Features.Select(feature => feature.Id).ToListAsync();
        }

        private async Task<List<string>> LiveFeatureNamesOf(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var delivery = await context.Deliveries.Include(d => d.Features).SingleAsync(d => d.Id == deliveryId);

            return [.. delivery.Features.Select(feature => feature.Name)];
        }

        private async Task<List<string>> PortfolioFeatureNames()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Features.Select(feature => feature.Name).ToListAsync();
        }
    }
}
