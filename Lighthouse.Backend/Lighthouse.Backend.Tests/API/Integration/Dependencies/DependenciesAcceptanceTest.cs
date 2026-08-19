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
using System.Globalization;
using System.Text.Json;

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

            // Scenarios here are about what a Feature waits on, not about who may read it, so the client
            // reads everything. A narrower identity would make a count come back short for a reason no
            // scenario states.
            Client = Factory.CreateClient().AsSystemAdmin();

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

        /// <summary>
        /// Puts a Feature in a place the way the ranking service puts one there: a single column write,
        /// never a save over the loaded graph, which would re-insert join rows nobody asked for.
        /// </summary>
        protected void PlaceTheFeatureByHand(string featureReferenceId, int place)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var rowsPlaced = context.Features
                .Where(feature => feature.ReferenceId == featureReferenceId)
                .ExecuteUpdate(setters => setters.SetProperty(feature => feature.ManualRank, place));

            if (rowsPlaced != 1)
            {
                throw new InvalidOperationException($"There was no single {featureReferenceId} on file to place by hand.");
            }
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

        /// <summary>
        /// One Feature as a client is handed it, over the real HTTP route the Features screen calls.
        /// Reading the store instead would say nothing about whether the count ever left the server.
        /// </summary>
        protected async Task<JsonElement?> ReadTheFeatureThePayloadCarries(string featureReferenceId)
        {
            using var response = await Client.GetAsync("/api/latest/features");
            response.EnsureSuccessStatusCode();

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            foreach (var feature in payload.RootElement.EnumerateArray())
            {
                if (feature.GetProperty("referenceId").GetString() == featureReferenceId)
                {
                    return feature.Clone();
                }
            }

            return null;
        }

        protected Feature? ReadTheFeatureRow(string featureReferenceId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return context.Features.AsNoTracking().FirstOrDefault(feature => feature.ReferenceId == featureReferenceId);
        }

        /// <summary>
        /// The id a route asks for, found by the reference id a scenario writes. A scenario names a Feature
        /// the way the work tracking system does; whichever id it was given on import is nobody's fact.
        /// </summary>
        protected int TheFeatureIdOf(string featureReferenceId)
        {
            return ReadTheFeatureRow(featureReferenceId)?.Id
                ?? throw new InvalidOperationException($"There is no {featureReferenceId} on file to be asked about.");
        }

        /// <summary>
        /// Every value the Feature row holds, taken column by column from the mapping itself rather than
        /// from a list somebody typed out. A field added to a Feature next year is compared here without
        /// anyone remembering to come back, which is the point: this is the probe for "and nothing else
        /// moved", and a hand-written list only ever covers what its author already suspected.
        /// </summary>
        protected Dictionary<string, string> ReadEveryValueRecordedFor(string featureReferenceId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var feature = context.Features.FirstOrDefault(candidate => candidate.ReferenceId == featureReferenceId)
                ?? throw new InvalidOperationException($"There is no {featureReferenceId} on file to read.");

            var row = context.Entry(feature);

            return row.Metadata.GetProperties().ToDictionary(
                column => column.Name,
                column => AsComparableText(row.Property(column.Name).CurrentValue));
        }

        private static string AsComparableText(object? value)
        {
            return value switch
            {
                null => "<nothing>",
                string text => text,
                IEnumerable<KeyValuePair<int, string?>> pairs => string.Join(", ",
                    pairs.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={AsComparableText(pair.Value)}")),
                System.Collections.IEnumerable items => string.Join(", ", items.Cast<object?>().Select(AsComparableText)),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            };
        }

        protected List<string> ReadProblemsLogged() => [.. CapturedLogs.AtOrAbove(LogEventLevel.Error)];

        private static Feature TheConnectorPayloadFor(TrackedFeature row)
        {
            var feature = new Feature
            {
                ReferenceId = row.ReferenceId,
                Name = row.Name,
                Url = $"https://tracker.example/{row.ReferenceId}",
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
