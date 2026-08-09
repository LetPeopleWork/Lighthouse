using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL acceptance harness (Epic 5687 — Faster Updates, slice 01 / Story #5724). The driving port
    /// is the one the epic is observed through: the scheduled refresh, triggered on an updater and run by
    /// the production update queue in its own DI scope. The observable surface is the log stream — so the
    /// harness replaces the logger factory rather than the services that write to it.
    ///
    /// Real: EF over SQLite, the update queue, <c>TeamDataService</c>, <c>WorkItemService</c>, the
    /// metrics services, the domain-event dispatcher and <c>RefreshLogService</c>. Faked: the
    /// work-tracking connector and <see cref="IForecastService"/> — the two external / non-deterministic
    /// driven ports per docs/architecture/atdd-infrastructure-policy.md — and the licence service.
    ///
    /// Faking the data services (as the Quiet-write-back harness does) would make the noise assertions
    /// vacuous: <c>WorkItemService</c> and <c>TeamDataService</c> are the loudest voices on the update
    /// path and AC-1.7 is a promise about exactly them.
    /// </summary>
    public abstract class FasterUpdatesAcceptanceTest
    {
        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;

        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected Mock<IWorkTrackingConnector> ConnectorMock = null!;
        protected Mock<IForecastService> ForecastServiceMock = null!;

        /// <summary>
        /// Every log line the refresh produced, with its level. The level is half the promise: a line an
        /// operator is meant to read has to arrive at Information, and a line that was demoted has to
        /// still exist at Debug.
        /// </summary>
        protected CapturedLogMessages CapturedLogs = null!;

        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();
            CapturedLogs = new CapturedLogMessages();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            ConnectorMock = new Mock<IWorkTrackingConnector>();
            ConnectorMock.Setup(c => c.SupportsTransitionHistory(It.IsAny<WorkTrackingSystemConnection>())).Returns(false);
            ConnectorMock.Setup(c => c.GetPredefinedAdditionalFields(It.IsAny<WorkTrackingSystemConnection>())).Returns([]);
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
            ConnectorMock
                .Setup(c => c.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ReturnsAsync(new WriteBackResult());

            // Monte Carlo is non-deterministic and this slice asserts nothing about a forecast; faking it
            // keeps the portfolio refresh's duration and log shape reproducible.
            ForecastServiceMock = new Mock<IForecastService>();

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(ConnectorMock.Object);

            Factory = RootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => LicenseServiceMock.Object);

                    services.RemoveAll<IWorkTrackingConnectorFactory>();
                    services.AddScoped(_ => connectorFactoryMock.Object);

                    services.RemoveAll<IForecastService>();
                    services.AddScoped(_ => ForecastServiceMock.Object);

                    // ADR-137 D72: Serilog is the pipeline, so an ILoggerProvider added here would be
                    // inert. Replacing the factory is what makes the refresh's own log readable.
                    services.RemoveAll<ILoggerFactory>();
                    // The framework overrides mirror appsettings.json: "operator-visible" has to mean the
                    // stream the operator actually reads, and EF's SQL never reaches it.
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                        new LoggerConfiguration()
                            .MinimumLevel.Verbose()
                            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                            .WriteTo.Sink(CapturedLogs)
                            .CreateLogger(),
                        dispose: true));
                });
            });

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
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
            using (var teardownScope = Factory.Services.CreateScope())
            {
                teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Seeding (preconditions only — never the expected output) ---

        protected int SeedConnection(WorkTrackingSystems system = WorkTrackingSystems.Jira)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = system };
            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return connection.Id;
        }

        protected int SeedPortfolio(int connectionId, string name)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                UpdateTime = DateTime.UtcNow,
            };

            var repository = sp.GetRequiredService<IRepository<Portfolio>>();
            repository.Add(portfolio);
            repository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        protected int SeedTeam(int connectionId, string name, int? portfolioId = null)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                ThroughputHistory = 30,
                UpdateTime = DateTime.UtcNow,
            };

            if (portfolioId.HasValue)
            {
                team.Portfolios.Add(sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId.Value)!);
            }

            var repository = sp.GetRequiredService<IRepository<Team>>();
            repository.Add(team);
            repository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        // --- The tracker ---

        protected void TheTrackerReturnsWorkItems(int count)
        {
            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>()))
                .ReturnsAsync((Team team) => [.. Enumerable.Range(1, count).Select(index => new WorkItem(new WorkItemBase
                {
                    ReferenceId = $"ITEM-{index}",
                    Name = $"Work Item {index}",
                    Type = "Story",
                    State = "In Progress",
                    StateCategory = StateCategories.Doing,
                    Order = index.ToString(),
                    ParentReferenceId = string.Empty,
                    StartedDate = DateTime.UtcNow.AddDays(-index),
                }, team))]);
        }

        protected void TheTrackerReturnsFeatures(int count)
        {
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync([.. Enumerable.Range(1, count).Select(index => new Feature(new WorkItemBase
                {
                    ReferenceId = $"FEAT-{index}",
                    Name = $"Feature {index}",
                    Type = "Epic",
                    State = "In Progress",
                    StateCategory = StateCategories.Doing,
                    Order = index.ToString(),
                    ParentReferenceId = string.Empty,
                }))]);
        }

        protected void TheTrackerIsUnreachable(Exception failure)
        {
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>())).ThrowsAsync(failure);
            ConnectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>())).ThrowsAsync(failure);
        }

        // --- Driving port: the scheduled refresh ---

        protected Task TheTeamRefreshRuns(int teamId)
            => RunUpdate(sp => sp.GetRequiredService<ITeamUpdater>().TriggerUpdate(teamId));

        protected Task ThePortfolioRefreshRuns(int portfolioId)
            => RunUpdate(sp => sp.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolioId));

        /// <summary>
        /// Triggers one update and waits for the queue to go idle. Admission is synchronous inside
        /// <c>EnqueueUpdate</c>, so the key is already active when this starts polling.
        /// </summary>
        private async Task RunUpdate(Action<IServiceProvider> trigger)
        {
            var statusStore = Factory.Services.GetRequiredService<IUpdateStatusStore>();

            // Host startup and fixture seeding log through the same sink; the line budget is a promise
            // about one update, so counting starts here.
            CapturedLogs.Clear();

            trigger(Factory.Services);

            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (statusStore.HasActiveWork())
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail("The update queue did not go idle within 30s — the refresh never completed.");
                }

                await Task.Delay(20);
            }
        }

        // --- Observation ---

        protected RefreshLog? TheRefreshLogFor(RefreshType type, int entityId)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRefreshLogService>()
                .GetRefreshLogs()
                .SingleOrDefault(log => log.Type == type && log.EntityId == entityId);
        }

        protected IReadOnlyList<string> TheOperatorVisibleLines => CapturedLogs.AtOrAbove(LogEventLevel.Information);
    }
}
