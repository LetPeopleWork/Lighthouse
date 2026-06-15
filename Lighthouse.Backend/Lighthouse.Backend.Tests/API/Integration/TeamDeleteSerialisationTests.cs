using System.Collections.Concurrent;
using System.Net;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
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

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class TeamDeleteSerialisationTests
    {
        private TestWebApplicationFactory<Program> factory;
        private WebApplicationFactory<Program> hostedFactory;
        private HttpClient client;
        private GateableWorkItemService gateableWorkItemService;
        private CapturingLoggerProvider capturingLoggerProvider;

        [SetUp]
        public void Init()
        {
            factory = new TestWebApplicationFactory<Program>();
            gateableWorkItemService = new GateableWorkItemService();
            capturingLoggerProvider = new CapturingLoggerProvider();

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

                builder.ConfigureLogging(logging =>
                {
                    logging.AddProvider(capturingLoggerProvider);
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
            capturingLoggerProvider.Dispose();
        }

        [Test]
        public async Task DeleteTeam_WhileTeamUpdateInFlight_AwaitsQueueDrain_Returns204_NoConcurrencyException()
        {
            var team = await SeedTeamWithFeatureWork();
            await SeedRefreshLogForTeam(team.Id);
            gateableWorkItemService.GateNextTeamCallFor(team.Id);

            var teamUpdater = hostedFactory.Services.GetRequiredService<ITeamUpdater>();
            teamUpdater.TriggerUpdate(team.Id);

            await WaitForInflightUpdate(team.Id, UpdateType.Team, TimeSpan.FromSeconds(5));

            var deleteTask = client.DeleteAsync($"/api/latest/teams/{team.Id}");

            await Task.WhenAny(deleteTask, Task.Delay(TimeSpan.FromMilliseconds(500)));
            Assert.That(deleteTask.IsCompleted, Is.False, "DELETE must not complete while the gated team update is in flight");

            gateableWorkItemService.ReleaseAll();

            var deleteResponse = await deleteTask.WaitAsync(TimeSpan.FromSeconds(15));

            await WaitForNoActiveUpdates(TimeSpan.FromSeconds(10));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(capturingLoggerProvider.ContainsMessageFragment("DbUpdateConcurrencyException"), Is.False);
                Assert.That(TeamExists(team.Id), Is.False);
                Assert.That(FeatureWorkExistsForTeam(team.Id), Is.False);
                Assert.That(RefreshLogsExistForTeam(team.Id), Is.False);
            }
        }

        [Test]
        public async Task DeleteTeam_NoOtherActivity_RemovesTeamFeatureWorkAndRefreshLogs()
        {
            var team = await SeedTeamWithFeatureWork();
            await SeedRefreshLogForTeam(team.Id);

            var response = await client.DeleteAsync($"/api/latest/teams/{team.Id}");

            await WaitForNoActiveUpdates(TimeSpan.FromSeconds(10));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(TeamExists(team.Id), Is.False);
                Assert.That(FeatureWorkExistsForTeam(team.Id), Is.False);
                Assert.That(RefreshLogsExistForTeam(team.Id), Is.False);
                Assert.That(capturingLoggerProvider.ContainsMessageFragment("DbUpdateConcurrencyException"), Is.False);
            }
        }

        private async Task<Team> SeedTeamWithFeatureWork()
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
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = workTrackingSystemConnection,
            };
            portfolio.UpdateFeatures([
                new Feature(team, 5) { Name = "Feature A", Order = "1" },
            ]);

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return teamRepository.GetById(team.Id)!;
        }

        private async Task SeedRefreshLogForTeam(int teamId)
        {
            using var scope = hostedFactory.Services.CreateScope();
            var refreshLogService = scope.ServiceProvider.GetRequiredService<IRefreshLogService>();
            await refreshLogService.LogRefreshAsync(new RefreshLog
            {
                Type = RefreshType.Team,
                EntityId = teamId,
                EntityName = "Team",
                ItemCount = 1,
                DurationMs = 1,
                ExecutedAt = DateTime.UtcNow,
                Success = true,
            });
        }

        private bool TeamExists(int teamId)
        {
            using var scope = hostedFactory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            return repo.Exists(teamId);
        }

        private bool FeatureWorkExistsForTeam(int teamId)
        {
            using var scope = hostedFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            return dbContext.Set<FeatureWork>().Any(fw => fw.TeamId == teamId);
        }

        private bool RefreshLogsExistForTeam(int teamId)
        {
            using var scope = hostedFactory.Services.CreateScope();
            var refreshLogService = scope.ServiceProvider.GetRequiredService<IRefreshLogService>();
            return refreshLogService.GetRefreshLogs().Any(l => l.Type == RefreshType.Team && l.EntityId == teamId);
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
            private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> teamGates = new();

            public void GateNextTeamCallFor(int teamId)
            {
                teamGates[teamId] = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public void ReleaseAll()
            {
                foreach (var gate in teamGates.Values)
                {
                    gate.TrySetResult(true);
                }
            }

            public Task UpdateFeaturesForPortfolio(Portfolio portfolio)
            {
                return Task.CompletedTask;
            }

            public async Task UpdateWorkItemsForTeam(Team team)
            {
                if (teamGates.TryGetValue(team.Id, out var gate))
                {
                    await gate.Task;
                }
            }
        }

        private sealed class CapturingLoggerProvider : ILoggerProvider
        {
            private readonly ConcurrentBag<string> messages = new();

            public ILogger CreateLogger(string categoryName) => new CapturingLogger(messages);

            public bool ContainsMessageFragment(string fragment)
            {
                return messages.Any(m => m.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }

            public void Dispose() { }

            private sealed class CapturingLogger(ConcurrentBag<string> messages) : ILogger
            {
                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    var message = formatter(state, exception);
                    if (exception != null)
                    {
                        message = $"{message} :: {exception.GetType().FullName}: {exception.Message}";
                    }
                    messages.Add(message);
                }
            }
        }
    }
}
