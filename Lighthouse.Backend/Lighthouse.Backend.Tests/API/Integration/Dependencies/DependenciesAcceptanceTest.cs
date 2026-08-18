using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Acceptance harness for what a Feature waits on. Scenarios reach the system the way a refresh
    /// really runs: the real ASP.NET host over real EF on real SQLite, with only the work tracking
    /// connector faked, because that is the one driven port a refresh scenario cannot reach for real.
    /// Everything between it and the store - the work item service, the reconciler, EF - stays production.
    /// </summary>
    public abstract class DependenciesAcceptanceTest
    {
        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;
        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected Mock<IWorkTrackingConnector> ConnectorMock = null!;
        protected Mock<IForecastService> ForecastServiceMock = null!;

        protected CapturedLogMessages CapturedLogs = null!;

        /// <summary>
        /// One row the tracker hands back for a Feature, and the ids of the Features it is waiting on.
        /// </summary>
        protected readonly record struct TrackedFeature(string ReferenceId, string Name, string[] WaitsOn);

        /// <summary>
        /// One stored reference, read back beside both ids it can be judged against: the Feature row it
        /// hangs off, and the Feature the reference itself claims to belong to.
        /// </summary>
        protected readonly record struct StoredDependency(
            string FeatureReferenceId,
            int OwningFeatureId,
            int KeyedToFeatureId,
            string WaitsOnReferenceId,
            DependencySource Source);

        [SetUp]
        public void Init()
        {
            CapturedLogs = new CapturedLogMessages();
            RootFactory = new TestWebApplicationFactory<Program>();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            ConnectorMock = new Mock<IWorkTrackingConnector>();
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync(() => []);
            ConnectorMock
                .Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(() => []);

            // A Monte Carlo run over seeded throughput would make every scenario here time against a
            // simulation it says nothing about.
            ForecastServiceMock = new Mock<IForecastService>();

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(ConnectorMock.Object);

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(RootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => LicenseServiceMock.Object);

                        services.RemoveAll<IWorkTrackingConnectorFactory>();
                        services.AddScoped(_ => connectorFactoryMock.Object);

                        services.RemoveAll<IForecastService>();
                        services.AddScoped(_ => ForecastServiceMock.Object);

                        // Serilog is the whole logging pipeline here, and it ignores anything added as
                        // an ILoggerProvider, so replacing the factory is the only way a scenario gets
                        // to read what the refresh logged.
                        services.RemoveAll<ILoggerFactory>();
                        services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                            new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(CapturedLogs).CreateLogger(),
                            dispose: true));
                    });
                });

            Client = Factory.CreateClient();

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
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Seeding (preconditions only - never the expected output) ---

        protected int SeedPortfolio(string name)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "[System.WorkItemType] = 'Epic'",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        // --- Driving-port interaction ---

        /// <summary>
        /// Drives one real refresh of a Portfolio through the production work item service, with the
        /// connector handing back the rows given. Each row carries its dependency references the way a
        /// connector builds them - against a Feature that has not been saved yet, so every reference
        /// still names Feature nought.
        /// </summary>
        protected async Task DriveAPortfolioRefresh(int portfolioId, params TrackedFeature[] rowsFromTheTracker)
        {
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync(() => rowsFromTheTracker.Select(TheConnectorPayloadFor).ToList());

            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            // Host startup and fixture seeding log through the same sink, so a scenario asking what the
            // refresh had to say has to start listening here.
            CapturedLogs.Clear();

            await sp.GetRequiredService<IWorkItemService>().UpdateFeaturesForPortfolio(portfolio);
        }

        protected IDependencyReconciler TheReconcilerTheHostResolves()
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IDependencyReconciler>();
        }

        // --- Driven-port probes (read straight from the store, through a context that saw none of the write) ---

        protected List<StoredDependency> ReadStoredDependencies()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return context.Features
                .AsNoTracking()
                .Include(feature => feature.DependsOnReferences)
                .ToList()
                .SelectMany(feature => feature.DependsOnReferences.Select(reference => new StoredDependency(
                    feature.ReferenceId,
                    feature.Id,
                    reference.FeatureId,
                    reference.ReferenceId,
                    reference.Source)))
                .ToList();
        }

        /// <summary>
        /// What a Feature waits on, worked out the way anything reading the graph has to work it out:
        /// the stored id strings matched against the Features Lighthouse actually holds. An id matching
        /// none of them yields nothing here, and yields something again the day it does match one.
        /// </summary>
        protected List<string> ReadWhatItWaitsOnAmongTheFeaturesHeld(string featureReferenceId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var featuresHeld = context.Features
                .AsNoTracking()
                .Include(feature => feature.DependsOnReferences)
                .ToList();

            var idsHeld = featuresHeld.Select(feature => feature.ReferenceId).ToHashSet();

            return featuresHeld
                .Where(feature => feature.ReferenceId == featureReferenceId)
                .SelectMany(feature => feature.DependsOnReferences.Select(reference => reference.ReferenceId))
                .Where(idsHeld.Contains)
                .Order()
                .ToList();
        }

        protected Feature? ReadTheFeatureRow(string featureReferenceId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return context.Features.AsNoTracking().FirstOrDefault(feature => feature.ReferenceId == featureReferenceId);
        }

        protected List<string> ReadProblemsLogged() => [.. CapturedLogs.AtOrAbove(LogEventLevel.Error)];

        private static Feature TheConnectorPayloadFor(TrackedFeature row)
        {
            var feature = new Feature
            {
                ReferenceId = row.ReferenceId,
                Name = row.Name,
                Type = "Epic",
                State = "New",
                StateCategory = StateCategories.ToDo,
                Order = string.Empty,
                CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Tags = [],
                SyncedTransitions = [],
            };

            feature.ReplaceDependsOnReferences(row.WaitsOn.Select(
                waitsOn => new FeatureDependencyReference(feature.Id, waitsOn, DependencySource.TrackerLink)));

            return feature;
        }
    }
}
