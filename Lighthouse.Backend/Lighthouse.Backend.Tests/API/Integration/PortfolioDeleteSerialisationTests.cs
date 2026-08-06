using System.Collections.Concurrent;
using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class PortfolioDeleteSerialisationTests
    {
        private TestWebApplicationFactory<Program> factory;
        private WebApplicationFactory<Program> hostedFactory;
        private HttpClient client;
        private GateableWorkItemService gateableWorkItemService;
        private CapturedLogMessages capturedLogMessages;

        [SetUp]
        public void Init()
        {
            factory = new TestWebApplicationFactory<Program>();
            gateableWorkItemService = new GateableWorkItemService();
            capturedLogMessages = new CapturedLogMessages();

            var permissiveLicense = new Mock<ILicenseService>();
            permissiveLicense.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            hostedFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IWorkItemService>();
                    services.AddSingleton<IWorkItemService>(gateableWorkItemService);
                    services.RemoveAll<ILicenseService>();
                    services.AddSingleton<ILicenseService>(permissiveLicense.Object);
                });

                // D72: an ILoggerProvider added here would be inert. Serilog is the pipeline.
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILoggerFactory>();
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                        new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(capturedLogMessages).CreateLogger(),
                        dispose: true));
                });
            });

            client = hostedFactory.CreateClient();

            using var setupScope = hostedFactory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = hostedFactory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            hostedFactory.Dispose();
            factory.Dispose();
        }

        [Test]
        public async Task DeletePortfolio_WhileQueueTaskInFlight_AwaitsQueueDrain_Returns204_NoConcurrencyException()
        {
            var portfolio = await SeedPortfolioWithFeatureAndTeam();
            gateableWorkItemService.GateNextCallFor(portfolio.Id);

            var portfolioUpdater = hostedFactory.Services.GetRequiredService<IPortfolioUpdater>();
            portfolioUpdater.TriggerUpdate(portfolio.Id);

            await WaitForInflightUpdate(portfolio.Id, UpdateType.Features, TimeSpan.FromSeconds(5));

            var deleteTask = client.DeleteAsync($"/api/latest/portfolios/{portfolio.Id}");

            var firstFinished = await Task.WhenAny(deleteTask, Task.Delay(TimeSpan.FromMilliseconds(500)));
            Assert.That(deleteTask.IsCompleted, Is.False, "DELETE must not complete while the queued update is gated");

            gateableWorkItemService.ReleaseAll();

            var deleteResponse = await deleteTask.WaitAsync(TimeSpan.FromSeconds(15));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                capturedLogMessages.AssertNothingLoggedMatching("DbUpdateConcurrencyException");
                Assert.That(PortfolioExists(portfolio.Id), Is.False);
            }
        }

        [Test]
        public async Task DeletePortfolio_NoOtherActivity_DeletesPortfolioAndOrphanFeatures()
        {
            var portfolio = await SeedPortfolioWithFeatureAndTeam();
            var featureIdsBefore = GetAllFeatureIdsForPortfolio(portfolio.Id);

            var response = await client.DeleteAsync($"/api/latest/portfolios/{portfolio.Id}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(PortfolioExists(portfolio.Id), Is.False);
                Assert.That(featureIdsBefore, Is.Not.Empty, "Sanity: features must have been seeded so we can verify orphan removal");
                foreach (var featureId in featureIdsBefore)
                {
                    Assert.That(FeatureExists(featureId), Is.False, $"Feature {featureId} should be removed as orphan");
                }
            }
        }

        [Test]
        public async Task DeletePortfolio_NonExistentId_Returns404_NoQueueWork()
        {
            var statuses = hostedFactory.Services.GetRequiredService<ConcurrentDictionary<UpdateKey, UpdateStatus>>();
            statuses.Clear();

            var response = await client.DeleteAsync("/api/latest/portfolios/99999");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(statuses.Keys.Any(k => k.Id == 99999), Is.False);
            }
        }

        [Test]
        public async Task DeletePortfolio_TwoConcurrentRequestsSamePortfolio_BothSucceedOrSecondIs404_NoException()
        {
            var portfolio = await SeedPortfolioWithFeatureAndTeam();

            var deleteOne = client.DeleteAsync($"/api/latest/portfolios/{portfolio.Id}");
            var deleteTwo = client.DeleteAsync($"/api/latest/portfolios/{portfolio.Id}");

            var responses = await Task.WhenAll(deleteOne, deleteTwo);

            var acceptableStatusCodes = new[] { HttpStatusCode.NoContent, HttpStatusCode.NotFound };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(responses[0].StatusCode, Is.AnyOf(acceptableStatusCodes));
                Assert.That(responses[1].StatusCode, Is.AnyOf(acceptableStatusCodes));
                Assert.That(responses.Any(r => r.StatusCode == HttpStatusCode.NoContent), Is.True, "At least one DELETE must succeed");
                Assert.That(PortfolioExists(portfolio.Id), Is.False);
                capturedLogMessages.AssertNothingLoggedMatching("DbUpdateConcurrencyException");
            }
        }

        [Test]
        public async Task RefreshAfterDelete_Serialised_RefreshShortCircuitsOnMissingPortfolio()
        {
            var portfolio = await SeedPortfolioWithFeatureAndTeam();

            var deleteResponse = await client.DeleteAsync($"/api/latest/portfolios/{portfolio.Id}");
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            var refreshResponse = await client.PostAsync($"/api/latest/portfolios/{portfolio.Id}/refresh", content: null);

            await WaitForNoActiveUpdates(TimeSpan.FromSeconds(10));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refreshResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(PortfolioExists(portfolio.Id), Is.False);
                capturedLogMessages.AssertNothingLoggedMatching("DbUpdateConcurrencyException");
            }
        }

        [Test]
        public async Task DeleteDuringInFlightUpdate_QueueDrainsWithoutConcurrencyException_StatusEndpointReturnsClean()
        {
            var portfolio = await SeedPortfolioWithFeatureAndTeam();
            gateableWorkItemService.GateNextCallFor(portfolio.Id);

            var portfolioUpdater = hostedFactory.Services.GetRequiredService<IPortfolioUpdater>();
            portfolioUpdater.TriggerUpdate(portfolio.Id);

            await WaitForInflightUpdate(portfolio.Id, UpdateType.Features, TimeSpan.FromSeconds(5));

            var deleteTask = client.DeleteAsync($"/api/latest/portfolios/{portfolio.Id}");

            await Task.Delay(200);

            gateableWorkItemService.ReleaseAll();

            var deleteResponse = await deleteTask.WaitAsync(TimeSpan.FromSeconds(15));

            await WaitForNoActiveUpdates(TimeSpan.FromSeconds(10));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                capturedLogMessages.AssertNothingLoggedMatching("DbUpdateConcurrencyException");
                Assert.That(PortfolioExists(portfolio.Id), Is.False);
            }
        }

        private async Task<Portfolio> SeedPortfolioWithFeatureAndTeam(string portfolioName = "Test Portfolio")
        {
            using var scope = hostedFactory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = workTrackingSystemConnection,
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var portfolio = new Portfolio
            {
                Name = portfolioName,
                WorkTrackingSystemConnection = workTrackingSystemConnection,
            };
            portfolio.UpdateFeatures([
                new Feature(team, 5) { Name = "Feature A", Order = "1" },
            ]);

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolioRepository.GetAll().Single(p => p.Name == portfolioName);
        }

        private bool PortfolioExists(int portfolioId)
        {
            using var scope = hostedFactory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            return repo.Exists(portfolioId);
        }

        private bool FeatureExists(int featureId)
        {
            using var scope = hostedFactory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            return repo.Exists(featureId);
        }

        private List<int> GetAllFeatureIdsForPortfolio(int portfolioId)
        {
            using var scope = hostedFactory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var portfolio = repo.GetById(portfolioId);
            return portfolio?.Features.Select(f => f.Id).ToList() ?? [];
        }

        private async Task WaitForInflightUpdate(int id, UpdateType updateType, TimeSpan timeout)
        {
            var statuses = hostedFactory.Services.GetRequiredService<ConcurrentDictionary<UpdateKey, UpdateStatus>>();
            var key = new UpdateKey(updateType, id);
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (statuses.TryGetValue(key, out var status) && status.Status == UpdateProgress.InProgress)
                {
                    return;
                }
                await Task.Delay(20);
            }
            throw new TimeoutException($"Timed out waiting for in-flight update {updateType}:{id}");
        }

        private async Task WaitForNoActiveUpdates(TimeSpan timeout)
        {
            var statuses = hostedFactory.Services.GetRequiredService<ConcurrentDictionary<UpdateKey, UpdateStatus>>();
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                var activeCount = statuses.Values.Count(s => s.Status is UpdateProgress.Queued or UpdateProgress.InProgress);
                if (activeCount == 0)
                {
                    return;
                }
                await Task.Delay(50);
            }
            throw new TimeoutException("Timed out waiting for active updates to drain");
        }

        private sealed class GateableWorkItemService : IWorkItemService
        {
            private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> gates = new();

            public void GateNextCallFor(int portfolioId)
            {
                gates[portfolioId] = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public void ReleaseAll()
            {
                foreach (var gate in gates.Values)
                {
                    gate.TrySetResult(true);
                }
            }

            public async Task UpdateFeaturesForPortfolio(Portfolio portfolio)
            {
                if (gates.TryGetValue(portfolio.Id, out var gate))
                {
                    await gate.Task;
                }
            }

            public Task UpdateWorkItemsForTeam(Team team)
            {
                return Task.CompletedTask;
            }
        }
    }
}
