using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// Where nobody signs in and no roles are enforced, every caller reads every Portfolio, so no target
    /// may be refused for being out of the caller's sight. This needs a plain host: the manual sorting
    /// scenarios run on one that always authenticates.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("auth-off")]
    public class FeatureMoveAuthOffIntegrationTest : IntegrationFixture
    {
        private const string SearchIndex = "Rebuild the search index";
        private const string LegacyImporter = "Retire the legacy importer";

        private readonly List<(int OwnerId, OwnerType OwnerType)> ownersSeeded = [];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private Mock<IForecastUpdater> forecastUpdaterMock = null!;

        [OneTimeSetUp]
        public void StartTheHost()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            forecastUpdaterMock = new Mock<IForecastUpdater>();

            factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "false",
                        ["Authorization:Enabled"] = "false",
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);

                    services.RemoveAll<IForecastUpdater>();
                    services.AddSingleton(_ => forecastUpdaterMock.Object);
                });
            });
        }

        [SetUp]
        public async Task StartFromAnEmptyInstance()
        {
            ownersSeeded.Clear();
            forecastUpdaterMock.Reset();

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(service => service.CanUsePremiumFeatures()).Returns(true);

            rootFactory.Clock.Reset();
            rootFactory.UseFreshDatabase();
            factory.ForgetTheCachedMetrics();

            client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
            }
        }

        [TearDown]
        public async Task LeaveTheHostIdle()
        {
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
            factory?.Dispose();
            rootFactory?.Dispose();
        }

        [Test]
        public async Task A_feature_moves_next_to_a_feature_of_another_portfolio()
        {
            var platform = SeedPortfolio("Platform");
            var billing = SeedPortfolio("Billing");
            var target = SeedFeature(SearchIndex, "1", billing);
            var moved = SeedFeature(LegacyImporter, "2", platform);
            await HandTheOrderToThisInstance();

            var response = await MoveAbove(moved, target);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(await TheOrder(), Is.EqualTo(new[] { LegacyImporter, SearchIndex }));
            }

            forecastUpdaterMock.Verify(updater => updater.TriggerImmediateUpdate(platform), Times.AtLeastOnce);
        }

        [Test]
        public async Task A_target_that_does_not_exist_is_still_not_found()
        {
            var platform = SeedPortfolio("Platform");
            SeedFeature(SearchIndex, "1", platform);
            var moved = SeedFeature(LegacyImporter, "2", platform);
            await HandTheOrderToThisInstance();

            var response = await MoveAbove(moved, 987654);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(await TheOrder(), Is.EqualTo(new[] { SearchIndex, LegacyImporter }));
            }
        }

        private async Task HandTheOrderToThisInstance()
        {
            using var manualOrder = new StringContent("{\"policy\":\"ManualOrder\"}", Encoding.UTF8, "application/json");
            var response = await client.PutAsync("/api/latest/appsettings/FeatureOrdering", manualOrder);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Every move rests on this instance owning the order.");

            // The handover asks for a forecast of its own, which would otherwise answer for the move.
            forecastUpdaterMock.Invocations.Clear();
        }

        private async Task<HttpStatusCode> MoveAbove(int featureId, int targetFeatureId)
        {
            using var body = new StringContent($"{{\"beforeFeatureId\":{targetFeatureId}}}", Encoding.UTF8, "application/json");
            var response = await client.PatchAsync($"/api/latest/features/{featureId}/rank", body);

            return response.StatusCode;
        }

        private async Task<string[]> TheOrder()
        {
            using var document = JsonDocument.Parse(await client.GetStringAsync("/api/latest/features"));

            return [.. document.RootElement.EnumerateArray().Select(feature => feature.GetProperty("name").GetString()!)];
        }

        private int SeedPortfolio(string name)
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            repository.Add(portfolio);
            repository.Save().GetAwaiter().GetResult();

            ownersSeeded.Add((portfolio.Id, OwnerType.Portfolio));

            return portfolio.Id;
        }

        private int SeedFeature(string name, string sourceOrder, int portfolioId)
        {
            using var scope = factory.Services.CreateScope();
            var portfolioRepository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

            var feature = new Feature
            {
                Name = name,
                ReferenceId = $"FTR-{sourceOrder}",
                Type = "Epic",
                State = "New",
                StateCategory = StateCategories.ToDo,
                Order = sourceOrder,
            };

            feature.Portfolios.Add(portfolioRepository.GetById(portfolioId)!);

            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return feature.Id;
        }
    }
}
