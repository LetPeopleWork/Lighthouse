using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
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
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance harness for Epic #5511 (Task Manager). The driving port is the one the whole
    /// Epic is observed through: a refresh triggered on an updater and run by the production update
    /// queue in its own DI scope.
    ///
    /// The observable this Epic turns on is what the browser is told, so the harness replaces the SignalR
    /// hub context with one that records the pushes. Everything on the path between the updater and that
    /// push stays production: the queue, the status store, the execution lock, the write-back round and
    /// the refresh log.
    ///
    /// Faked: the work-tracking connector, <see cref="IForecastService"/> and the licence service — the
    /// external and non-deterministic driven ports per docs/architecture/atdd-infrastructure-policy.md.
    /// </summary>
    public abstract class TaskManagerAcceptanceTest
    {
        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;

        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected Mock<IWorkTrackingConnector> ConnectorMock = null!;
        protected Mock<IForecastService> ForecastServiceMock = null!;

        protected CapturedUpdateNotifications TheBrowserWasTold = null!;
        protected CapturedLogMessages CapturedLogs = null!;
        protected FlushCount WriteBackFlushes = null!;

        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();
            TheBrowserWasTold = new CapturedUpdateNotifications();
            CapturedLogs = new CapturedLogMessages();
            WriteBackFlushes = new FlushCount();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            ConnectorMock = new Mock<IWorkTrackingConnector>();
            ConnectorMock.Setup(c => c.SupportsTransitionHistory(It.IsAny<WorkTrackingSystemConnection>())).Returns(false);
            ConnectorMock.Setup(c => c.SupportsIncrementalSync(It.IsAny<WorkTrackingSystemConnection>())).Returns(false);
            ConnectorMock.Setup(c => c.GetPredefinedAdditionalFields(It.IsAny<WorkTrackingSystemConnection>())).Returns([]);
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.SweepWorkItemsForTeam(It.IsAny<Team>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.SweepFeaturesForPortfolio(It.IsAny<Portfolio>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.SweepParentFeatures(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync([]);
            ConnectorMock
                .Setup(c => c.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ReturnsAsync(new WriteBackResult());

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

                    services.RemoveAll<IHubContext<UpdateNotificationHub>>();
                    services.AddSingleton(_ => RecordingHubContext());

                    // Wraps the real collector: the staging and flushing under test stays production, and
                    // only the fact that an execution reported itself finished is recorded on the way past.
                    services.RemoveAll<IWriteBackCollector>();
                    services.AddScoped<WriteBackCollector>();
                    services.AddScoped<IWriteBackCollector>(sp =>
                        new FlushRecordingWriteBackCollector(sp.GetRequiredService<WriteBackCollector>(), WriteBackFlushes));

                    // Serilog is the pipeline, so an ILoggerProvider added here would be inert; replacing
                    // the factory is what makes the refresh's own log readable. The framework overrides
                    // mirror appsettings.json so that "operator-visible" means the stream operators read.
                    services.RemoveAll<ILoggerFactory>();
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

        private IHubContext<UpdateNotificationHub> RecordingHubContext()
        {
            var clientProxy = new Mock<IClientProxy>();
            clientProxy
                .Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback((string _, object[] arguments, CancellationToken _) =>
                {
                    if (arguments.Length > 0 && arguments[0] is UpdateStatus status)
                    {
                        TheBrowserWasTold.Record(status);
                    }
                });

            var hubContext = new Mock<IHubContext<UpdateNotificationHub>>();
            hubContext.Setup(context => context.Clients.Group(It.IsAny<string>())).Returns(clientProxy.Object);

            return hubContext.Object;
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

        protected int SeedTeam(int connectionId, string name)
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

            var repository = sp.GetRequiredService<IRepository<Team>>();
            repository.Add(team);
            repository.Save().GetAwaiter().GetResult();

            return team.Id;
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

        // --- The tracker ---

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
        /// Triggers one refresh and waits for the queue to go idle. Admission is synchronous inside
        /// <c>EnqueueUpdate</c>, so the key is already active when this starts polling.
        /// </summary>
        protected async Task RunUpdate(Action<IServiceProvider> trigger)
        {
            var statusStore = Factory.Services.GetRequiredService<IUpdateStatusStore>();

            // Host startup and fixture seeding push and log through the same seams; every promise here is
            // about one refresh, so observation starts at the trigger.
            TheBrowserWasTold.Clear();
            CapturedLogs.Clear();
            WriteBackFlushes.Reset();

            trigger(Factory.Services);

            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (statusStore.HasActiveWork())
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail("The update queue did not go idle within 30s — the refresh never finished. "
                        + $"The browser was told: {TheBrowserWasTold.Describe()}");
                }

                await Task.Delay(20);
            }

            // The terminal push is raised after the key leaves the store, so an idle queue is not yet
            // proof the browser has been told. Give the push the moment it needs to land.
            await WaitForTheTerminalPush();
        }

        private async Task WaitForTheTerminalPush()
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);

            while (DateTime.UtcNow < deadline)
            {
                if (TheBrowserWasTold.Describe().Contains("Completed", StringComparison.Ordinal)
                    || TheBrowserWasTold.Describe().Contains("Failed", StringComparison.Ordinal))
                {
                    return;
                }

                await Task.Delay(20);
            }
        }

        // --- Observation ---

        protected RefreshLog? TheRecordedRefreshFor(RefreshType type, int entityId)
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IRefreshLogService>()
                .GetRefreshLogs()
                .Where(log => log.Type == type && log.EntityId == entityId)
                .OrderByDescending(log => log.ExecutedAt)
                .FirstOrDefault();
        }

        protected IReadOnlyList<string> TheOperatorVisibleLines => CapturedLogs.AtOrAbove(LogEventLevel.Information);
    }
}
