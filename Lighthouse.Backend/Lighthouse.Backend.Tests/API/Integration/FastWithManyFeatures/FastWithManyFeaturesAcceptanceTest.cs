using System.Globalization;
using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Serilog;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures
{
    /// <summary>
    /// The production host, over real EF on real SQLite, with what a large instance's screens read: the
    /// Northwind Portfolios and Teams, their Features with forecasts and forecast histograms, dependencies,
    /// parents and Teams working on Features right now. Scenarios reach it over HTTP, the way the Overview,
    /// the Features tabs and the edit page do.
    ///
    /// Two things are observed besides the answers. What the database handed back, counted by type, is how
    /// "this read no longer loads every forecast of the instance" becomes an assertion. What the host logged is
    /// how the warnings an operator reads are checked word for word.
    ///
    /// Faked, as the infrastructure policy says: the tracker, the licence and the forecast service. The
    /// instance's clock is pinned, because the bodies held against approved copies carry dates.
    /// </summary>
    public abstract class FastWithManyFeaturesAcceptanceTest
    {
        protected static readonly DateTimeOffset Now = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

        protected const string ApprovedBodiesFolder = "API/Integration/FastWithManyFeatures/Approved";

        protected const int HistogramRowsOfACompletionForecast = 3;

        protected const int HistogramRowsOfAStartForecast = 2;

        protected const int TotalItemsOfEveryFeature = 8;

        protected const int ItemsLeftOnAnOpenFeature = 5;

        private static readonly Dictionary<int, int> ACompletionHistogram = new() { [10] = 30, [20] = 40, [30] = 30 };

        private static readonly Dictionary<int, int> AStartHistogram = new() { [2] = 60, [6] = 40 };

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;
        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected Mock<IWorkTrackingConnector> ConnectorMock = null!;
        protected CapturedLogMessages CapturedLogs = null!;
        protected CapturedMaterialisations Materialised = null!;

        private readonly List<HttpClient> clientsHandedOut = [];

        private int nextOrder;

        [SetUp]
        public void Init()
        {
            CapturedLogs = new CapturedLogMessages();
            Materialised = new CapturedMaterialisations();
            nextOrder = 0;
            RootFactory = new TestWebApplicationFactory<Program>();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            ConnectorMock = new Mock<IWorkTrackingConnector>();
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => []);

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(ConnectorMock.Object);

            var forecastServiceMock = ForecastServiceDoubles.AnsweringComputed();

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
                        services.AddScoped(_ => forecastServiceMock.Object);

                        services.RemoveAll<TimeProvider>();
                        services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));
                        services.RemoveAll<ILighthouseClock>();
                        services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Now));

                        CapturedMaterialisations.CountInto(services, Materialised);

                        // Serilog is the whole logging pipeline here and ignores an added ILoggerProvider, so
                        // replacing the factory is the only way a scenario reads what the host logged.
                        services.RemoveAll<ILoggerFactory>();
                        services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                            new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(CapturedLogs).CreateLogger(),
                            dispose: true));
                    });
                });

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
                teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            foreach (var client in clientsHandedOut)
            {
                client.Dispose();
            }

            clientsHandedOut.Clear();
            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- The Northwind instance (preconditions only, never the expected output) ---

        /// <summary>The ids of the Northwind instance, so a scenario can name a Portfolio or Team the way a reader does.</summary>
        protected readonly record struct Northwind(
            int Platform,
            int Payments,
            int Strategy,
            int Archive,
            int Mobile,
            int Atlas,
            int Borealis,
            int Cobalt);

        /// <summary>One Feature as the tracker reported it: who works on it, whether it is done, what it waits on.</summary>
        protected readonly record struct HeldFeature(
            string ReferenceId,
            string Name,
            int[] WorkedOnBy,
            bool Completed = false,
            string[]? WaitsOn = null,
            string ParentReferenceId = "");

        protected const string CheckoutRedesign = "NW-1";
        protected const string SearchRevamp = "NW-2";
        protected const string LegacyCleanup = "NW-3";
        protected const string PaymentProviderSwitch = "NP-1";
        protected const string Refunds = "NP-2";
        protected const string Platform2027 = "ST-1";

        protected const string CheckoutRedesignsName = "Checkout redesign";

        private const string InProgress = "In Progress";
        private const string Done = "Done";

        protected const int FeaturesInTheArchive = 20;

        /// <summary>
        /// Northwind Platform (Checkout redesign, Search revamp, Legacy cleanup), Northwind Payments (Payment
        /// provider switch, Refunds), Northwind Strategy (Platform 2027, the parent of Checkout redesign),
        /// Northwind Archive (twenty older Features that stand for the rest of a large instance) and Northwind
        /// Mobile, created and never updated. Atlas and Cobalt work on Checkout redesign, Borealis on Payment
        /// provider switch. Checkout redesign waits on Payment provider switch.
        /// </summary>
        protected Northwind GivenTheNorthwindInstance()
        {
            var atlas = GivenATeam("Atlas");
            var borealis = GivenATeam("Borealis");
            var cobalt = GivenATeam("Cobalt");

            var strategy = GivenAPortfolio("Northwind Strategy");
            var platform = GivenAPortfolio("Northwind Platform");
            var payments = GivenAPortfolio("Northwind Payments");
            var archive = GivenAPortfolio("Northwind Archive");
            var mobile = GivenAPortfolio("Northwind Mobile");

            GivenThePortfolioHolds(strategy, new HeldFeature(Platform2027, "Platform 2027", [atlas]));
            GivenThePortfolioHolds(payments,
                new HeldFeature(PaymentProviderSwitch, "Payment provider switch", [borealis]),
                new HeldFeature(Refunds, "Refunds", [cobalt]));
            GivenThePortfolioHolds(platform,
                new HeldFeature(CheckoutRedesign, "Checkout redesign", [atlas, cobalt], WaitsOn: [PaymentProviderSwitch], ParentReferenceId: Platform2027),
                new HeldFeature(SearchRevamp, "Search revamp", [borealis]),
                new HeldFeature(LegacyCleanup, "Legacy cleanup", [atlas], Completed: true));
            GivenThePortfolioHolds(archive, [.. Enumerable.Range(1, FeaturesInTheArchive)
                .Select(index => new HeldFeature($"AR-{index}", $"Archived Feature {index}", [cobalt]))]);

            GivenTheTeamIsWorkingOn(atlas, CheckoutRedesign);
            GivenTheTeamIsWorkingOn(cobalt, CheckoutRedesign);
            GivenTheTeamIsWorkingOn(borealis, PaymentProviderSwitch);

            return new Northwind(platform, payments, strategy, archive, mobile, atlas, borealis, cobalt);
        }

        protected int GivenATeam(string name)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = TheTracker(context),
                DataRetrievalValue = "project = NORTHWIND",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = [InProgress],
                DoneStates = [Done],
                ThroughputHistory = 30,
                UpdateTime = Now.UtcDateTime,
            };

            context.Teams.Add(team);
            context.SaveChanges();

            return team.Id;
        }

        protected int GivenAPortfolio(string name)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = TheTracker(context),
                DataRetrievalValue = "project = NORTHWIND AND type = Epic",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = [InProgress],
                DoneStates = [Done],
                UpdateTime = Now.UtcDateTime,
            };

            context.Portfolios.Add(portfolio);
            context.SaveChanges();

            return portfolio.Id;
        }

        /// <summary>
        /// Stores the Features as a finished update of the Portfolio would have left them: each with a
        /// completion forecast per Team working on it, a start forecast per Team and one across every Team,
        /// every forecast with its histogram rows.
        /// </summary>
        protected void GivenThePortfolioHolds(int portfolioId, params HeldFeature[] features)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var portfolio = context.Portfolios.Include(p => p.Features).Single(p => p.Id == portfolioId);

            foreach (var held in features)
            {
                portfolio.Features.Add(AStoredFeature(context, held));
            }

            context.SaveChanges();
        }

        /// <summary>A Team has an item of the Feature in progress, so the Feature counts as one the Team is working on.</summary>
        protected void GivenTheTeamIsWorkingOn(int teamId, string featureReferenceId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var team = context.Teams.Single(t => t.Id == teamId);

            context.WorkItems.Add(new WorkItem(new WorkItemBase
            {
                ReferenceId = $"{featureReferenceId}-{team.Name}",
                Name = $"Work on {featureReferenceId} by {team.Name}",
                Type = "Story",
                State = InProgress,
                StateCategory = StateCategories.Doing,
                ParentReferenceId = featureReferenceId,
                StartedDate = Now.UtcDateTime.AddDays(-3),
                CreatedDate = Now.UtcDateTime.AddDays(-5),
                Order = "1",
            }, team));

            context.SaveChanges();
        }

        private Feature AStoredFeature(LighthouseAppContext context, HeldFeature held)
        {
            var dependsOn = (held.WaitsOn ?? []).Select(reference => new FeatureDependencyReference(0, reference, DependencySource.TrackerLink));

            var feature = new Feature(new WorkItemBase
            {
                ReferenceId = held.ReferenceId,
                Name = held.Name,
                Type = "Epic",
                State = held.Completed ? Done : InProgress,
                StateCategory = held.Completed ? StateCategories.Done : StateCategories.Doing,
                ParentReferenceId = held.ParentReferenceId,
                Url = $"https://tracker.northwind.example/browse/{held.ReferenceId}",
                Order = (nextOrder++).ToString("D4", CultureInfo.InvariantCulture),
                CreatedDate = Now.UtcDateTime.AddDays(-40),
                StartedDate = held.Completed ? Now.UtcDateTime.AddDays(-30) : Now.UtcDateTime.AddDays(-10),
                ClosedDate = held.Completed ? Now.UtcDateTime.AddDays(-2) : null,
            }, dependsOn);

            var teams = held.WorkedOnBy.Select(id => context.Teams.Single(team => team.Id == id)).ToList();

            foreach (var team in teams)
            {
                feature.AddOrUpdateWorkForTeam(team, held.Completed ? 0 : ItemsLeftOnAnOpenFeature, TotalItemsOfEveryFeature);
            }

            if (!held.Completed)
            {
                feature.SetFeatureForecasts(teams.Select(ACompletionForecastFor));
                feature.SetStartForecasts([.. teams.Select(team => new StartForecast(AStartHistogram, team, Now.UtcDateTime)), new StartForecast(AStartHistogram, null, Now.UtcDateTime)]);
            }

            return feature;
        }

        private static WhenForecast ACompletionForecastFor(Team team)
            => new(ACompletionHistogram)
            {
                Team = team,
                TeamId = team.Id,
                NumberOfItems = ItemsLeftOnAnOpenFeature,
                HasSufficientData = true,
                CreationTime = Now.UtcDateTime,
            };

        private static WorkTrackingSystemConnection TheTracker(LighthouseAppContext context)
        {
            var existing = context.WorkTrackingSystemConnections.FirstOrDefault();
            if (existing is not null)
            {
                return existing;
            }

            var connection = new WorkTrackingSystemConnection { Name = "Northwind tracker", WorkTrackingSystem = WorkTrackingSystems.Jira };
            context.WorkTrackingSystemConnections.Add(connection);
            context.SaveChanges();

            return connection;
        }

        // --- Who reads ---

        protected HttpClient AClient()
        {
            var client = Factory.CreateClient();
            clientsHandedOut.Add(client);
            return client;
        }

        protected HttpClient AViewerOfThePortfolio(int portfolioId) => AClient().AsPortfolioViewer(portfolioId);

        protected HttpClient AViewerOfTheTeam(int teamId) => AClient().AsTeamViewer(teamId);

        // --- Reading over HTTP, as the screens do ---

        protected static string TheFeaturesInProgressRoute(int teamId)
            => $"/api/latest/teams/{teamId}/metrics/featuresInProgress?asOfDate={Now.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        protected string TheFeaturesByIdRoute(params string[] featureReferenceIds)
            => "/api/latest/features/ids?" + string.Join("&", featureReferenceIds.Select(reference => $"featureIds={TheFeatureIdOf(reference)}"));

        protected static string TheFeaturesByReferenceRoute(params string[] featureReferenceIds)
            => "/api/latest/features/references?" + string.Join("&", featureReferenceIds.Select(reference => $"featureReferences={reference}"));

        protected async Task<string> TheBodyOf(string route, HttpClient? reader = null)
        {
            using var response = await (reader ?? Client).GetAsync(new Uri(route, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{route} answered {(int)response.StatusCode}: {body}");

            return body;
        }

        protected async Task<HttpStatusCode> TheStatusOf(string route, HttpClient reader)
        {
            using var response = await reader.GetAsync(new Uri(route, UriKind.Relative));
            return response.StatusCode;
        }

        protected async Task<JsonElement> TheAnswerTo(string route, HttpClient? reader = null)
        {
            using var document = JsonDocument.Parse(await TheBodyOf(route, reader));
            return document.RootElement.Clone();
        }

        /// <summary>Reads the route while counting what the database hands back, and only for that read.</summary>
        protected async Task<JsonElement> TheAnswerCountingWhatTheDatabaseHandsBack(string route, HttpClient? reader = null)
        {
            Materialised.StartListening();
            try
            {
                return await TheAnswerTo(route, reader);
            }
            finally
            {
                Materialised.StopListening();
            }
        }

        protected int TheFeatureIdOf(string featureReferenceId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return context.Features.AsNoTracking()
                .Where(feature => feature.ReferenceId == featureReferenceId)
                .Select(feature => feature.Id)
                .First();
        }

        protected static List<string> TheNamesIn(JsonElement features)
            => [.. features.EnumerateArray().Select(feature => feature.GetProperty("name").GetString() ?? string.Empty)];

        protected static JsonElement TheFeatureNamed(JsonElement features, string name)
        {
            foreach (var feature in features.EnumerateArray())
            {
                if (feature.GetProperty("name").GetString() == name)
                {
                    return feature;
                }
            }

            Assert.Fail($"No Feature named '{name}' in: {features}");
            return default;
        }
    }
}
