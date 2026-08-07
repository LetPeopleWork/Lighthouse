using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API.Integration.ManualSorting
{
    /// <summary>
    /// DISTILL acceptance harness (Epic 5375 — Manual Sorting). Single source of truth for HOW slice-01
    /// scenarios reach the system: through the real ASP.NET host on real SQLite (Pillar 3 —
    /// <see cref="WebApplicationFactory{TEntryPoint}"/>) over real EF. Only the license port
    /// (external/non-deterministic) is faked, plus the shipped claims-driven RBAC double every
    /// integration test in this project already uses.
    /// </summary>
    public abstract class ManualSortingAcceptanceTest
    {
        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;
        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected Mock<IWorkTrackingConnector> ConnectorMock = null!;
        protected Mock<IForecastUpdater> ForecastUpdaterMock = null!;

        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            // The work-tracking connector is the one driven port a refresh scenario may not reach for real
            // (external/non-deterministic, per docs/architecture/atdd-infrastructure-policy.md). Everything
            // between it and the read port - WorkItemService, EF, the ordering seam - stays production.
            ConnectorMock = new Mock<IWorkTrackingConnector>();
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync(() => []);
            ConnectorMock
                .Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(() => []);

            // The forecast runner is a background queue, so a scenario that waited for it would be timing
            // against a thread rather than asserting a promise. Faking it turns ADR-133's "a move triggers a
            // run" into something a test can see the moment the move commits.
            ForecastUpdaterMock = new Mock<IForecastUpdater>();

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

                        services.RemoveAll<IForecastUpdater>();
                        services.AddSingleton(_ => ForecastUpdaterMock.Object);
                    });
                });

            Client = Factory.CreateClient();

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<Lighthouse.Backend.Services.Interfaces.Seeding.ISeeder>())
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

        // --- Seeding (preconditions only — never the expected output; No Fixture Theater) ---

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
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
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
        /// Attaches a Team to a Portfolio, which is what puts a row in the <c>PortfolioTeam</c> join
        /// table. Every fixture in this suite ran without one until an ordering write over a real
        /// instance re-inserted those rows and failed on their unique key — a whole class of graph-write
        /// bug that a Portfolio with no Teams cannot express.
        /// </summary>
        protected int SeedTeamOn(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            team.Portfolios.Add(portfolioRepository.GetById(portfolioId)!);

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        /// <summary>
        /// Puts a Team on a Feature's work. This is the edge that drags the Team into the Feature read
        /// graph, so the Portfolio the Team belongs to and the Team itself end up tracked together and
        /// the join row between them becomes part of any write over that graph.
        /// </summary>
        protected void SeedWorkOn(int featureId, int teamId, int remainingWorkItems = 3)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var teamRepository = sp.GetRequiredService<IRepository<Team>>();

            var feature = featureRepository.GetById(featureId)!;
            var team = teamRepository.GetById(teamId)!;

            feature.FeatureWork.Add(new FeatureWork(team, remainingWorkItems, remainingWorkItems, feature));

            featureRepository.Update(feature);
            featureRepository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Seeds one Feature carrying the source-system order value verbatim (S1/S2 — the string the
        /// connector wrote), optionally linked to any number of Portfolios. An empty
        /// <paramref name="sourceOrder"/> is the AC-1.8 case; an empty <paramref name="portfolioIds"/>
        /// is the orphan case the premise check found on the dev instance.
        /// </summary>
        protected int SeedFeature(string name, string sourceOrder, StateCategories stateCategory, params int[] portfolioIds)
            => SeedFeature(name, $"FTR-{Guid.NewGuid():N}", sourceOrder, manualRank: null, stateCategory, portfolioIds);

        /// <summary>
        /// The fuller seed slice 02 needs. <paramref name="referenceId"/> is what a refresh matches an
        /// incoming row against, so a scenario that re-syncs a Feature must name it. A non-null
        /// <paramref name="manualRank"/> is a precondition only where the AC is about a rank multiset that
        /// already exists — gaps, duplicates and nulls are all legal (INV-O2), so no scenario may seed the
        /// ranks it is about to assert.
        /// </summary>
        protected int SeedFeature(string name, string referenceId, string sourceOrder, int? manualRank, StateCategories stateCategory, params int[] portfolioIds)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();

            var feature = new Feature
            {
                Name = name,
                ReferenceId = referenceId,
                Type = "Epic",
                State = stateCategory == StateCategories.Done ? "Done" : "New",
                StateCategory = stateCategory,
                Order = sourceOrder,
                ManualRank = manualRank,
            };

            foreach (var portfolioId in portfolioIds)
            {
                feature.Portfolios.Add(portfolioRepository.GetById(portfolioId)!);
            }

            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return feature.Id;
        }

        /// <summary>
        /// Seeds a whole run of ranked Features in one round trip — <see cref="SeedFeature"/> per row costs
        /// a scope and a save each, which at AC-1.9's five hundred rows dominates the scenario.
        /// </summary>
        protected void SeedRankedFeatures(int lastRank, Func<int, int[]> portfoliosForRank)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();

            for (var rank = 1; rank <= lastRank; rank++)
            {
                var feature = new Feature
                {
                    Name = $"Feature ranked {rank}",
                    ReferenceId = $"FTR-{rank}",
                    Type = "Epic",
                    State = "New",
                    StateCategory = StateCategories.ToDo,
                    Order = rank.ToString(),
                };

                foreach (var portfolioId in portfoliosForRank(rank))
                {
                    feature.Portfolios.Add(portfolioRepository.GetById(portfolioId)!);
                }

                featureRepository.Add(feature);
            }

            featureRepository.Save().GetAwaiter().GetResult();
        }

        protected void TheInstanceIsNotLicensedForPremium()
        {
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        }

        // --- Identity (the caller's RBAC scope decides the result set — D11) ---

        protected void TheCallerCanReadPortfolios(params int[] portfolioIds)
        {
            var grants = portfolioIds.Select(id => $"{ClaimsDrivenRbacAdministrationService.ViewerPortfolioGrantPrefix}{id}");
            ApplyIdentity("test-portfolio-viewer", string.Join(',', grants));
        }

        protected void TheCallerCanWritePortfolios(params int[] portfolioIds)
        {
            var grants = portfolioIds.Select(id => $"{ClaimsDrivenRbacAdministrationService.PortfolioAdminGrantPrefix}{id}");
            ApplyIdentity("test-portfolio-admin", string.Join(',', grants));
        }

        /// <summary>
        /// The scope shape ADR-136's rule is really about: the caller runs one Portfolio and can only look
        /// at another. Applying the two grants separately would not express it — each call replaces the
        /// caller's identity outright, so the second would silently drop the first.
        /// </summary>
        protected void TheCallerCanWriteSomePortfoliosAndOnlyReadOthers(int[] writablePortfolioIds, int[] readablePortfolioIds)
        {
            var grants = writablePortfolioIds
                .Select(id => $"{ClaimsDrivenRbacAdministrationService.PortfolioAdminGrantPrefix}{id}")
                .Concat(readablePortfolioIds.Select(id => $"{ClaimsDrivenRbacAdministrationService.ViewerPortfolioGrantPrefix}{id}"));

            ApplyIdentity("test-portfolio-admin", string.Join(',', grants));
        }

        protected void TheCallerAdministersTheWholeInstance()
        {
            ApplyIdentity("test-admin", ClaimsDrivenRbacAdministrationService.SystemAdminGrant);
        }

        // --- Driving-port interaction ---

        /// <summary>
        /// The Features view's read port (D17/AC-1.2). Not premium-gated (D12).
        /// </summary>
        protected async Task<(HttpStatusCode Status, string Body)> GetAllFeatures()
        {
            var response = await Client.GetAsync("/api/latest/features");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// The Portfolio detail read port. Its Features come off the second ordering call site
        /// (<c>PortfolioDto</c>), which is why K4 can only be judged by comparing it against the first.
        /// </summary>
        protected async Task<(HttpStatusCode Status, string Body)> GetPortfolio(int portfolioId)
        {
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Reads who owns the order on this instance. Open to any caller: every feature list asks it to
        /// name its position column, so only changing the answer takes an instance administrator.
        /// </summary>
        protected async Task<(HttpStatusCode Status, string Body)> GetOrderingPolicy()
        {
            var response = await Client.GetAsync("/api/latest/appsettings/FeatureOrdering");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Hands ordering ownership over, or gives it back. <c>SystemAdmin</c> + premium (AC-2.5, AC-2.7).
        /// The body stays raw JSON rather than the shipped DTO, so these scenarios judge the wire contract
        /// a client really sends and cannot be kept green by a rename on the server side.
        /// </summary>
        protected async Task<(HttpStatusCode Status, string Body)> SetOrderingPolicy(string policy)
        {
            var body = new StringContent($"{{\"policy\":\"{policy}\"}}", System.Text.Encoding.UTF8, "application/json");
            var response = await Client.PutAsync("/api/latest/appsettings/FeatureOrdering", body);
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Moves one Feature relative to another (D18's single endpoint shape). The body stays raw JSON so
        /// these scenarios judge the wire contract a client really sends: a rename on the server side
        /// cannot keep them green, and no scenario can pass by compiling against a type somebody added.
        /// <paramref name="targetJson"/> carries exactly one of <c>beforeFeatureId</c> / <c>afterFeatureId</c>;
        /// <c>"beforeFeatureId":null</c> is Move to Bottom.
        /// </summary>
        protected Task<(HttpStatusCode Status, string Body)> MoveFeature(int featureId, string targetJson)
            => MoveFeatureWithBody(featureId, $"{{{targetJson}}}");

        /// <summary>
        /// The same port with the body written out in full, for the shapes that are not an object with a
        /// target in it — which a caller can send and the endpoint therefore has to answer.
        /// </summary>
        protected async Task<(HttpStatusCode Status, string Body)> MoveFeatureWithBody(int featureId, string rawBody)
        {
            var body = new StringContent(rawBody, System.Text.Encoding.UTF8, "application/json");
            var response = await Client.PatchAsync($"/api/latest/features/{featureId}/rank", body);

            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Gives a Team a run chart with a different number of items closed on each of the last days.
        /// AC-3.6 needs the variation: with the same count every day every Feature finishes on the same
        /// simulated day, so a sequencing change has nothing to show up in (Epic 5459's lesson).
        /// <paramref name="itemsClosedPerDay"/> reads most-recent-day first.
        /// </summary>
        protected void SeedThroughputFor(int teamId, params int[] itemsClosedPerDay)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var today = sp.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var closed = 0;

            for (var daysAgo = 0; daysAgo < itemsClosedPerDay.Length; daysAgo++)
            {
                for (var item = 0; item < itemsClosedPerDay[daysAgo]; item++)
                {
                    workItemRepository.Add(new WorkItem
                    {
                        Name = $"Closed item {++closed}",
                        ReferenceId = $"WI-{teamId}-{closed}",
                        Type = "Story",
                        State = "Done",
                        StateCategory = StateCategories.Done,
                        TeamId = teamId,
                        ParentReferenceId = string.Empty,
                        Order = string.Empty,
                        StartedDate = today.AddDays(-(daysAgo + 5)),
                        ClosedDate = today.AddDays(-daysAgo),
                    });
                }
            }

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Runs the production Monte Carlo forecast for a Portfolio. Scenarios drive it themselves rather
        /// than waiting on the queue: whether a move *schedules* a run is a separate promise, asserted
        /// against <see cref="ForecastUpdaterMock"/>.
        /// </summary>
        protected async Task DriveAForecastRun(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            await sp.GetRequiredService<IForecastService>().UpdateForecastsForPortfolio(portfolio);
        }

        /// <summary>
        /// Drives one real work-item refresh for a Portfolio through the production
        /// <see cref="IWorkItemService"/>, with the connector handing back the rows given. This is the
        /// path that overwrites <c>WorkItemBase.Order</c> on every sync (S2) — the thing AC-2.2 asserts
        /// no longer moves anybody.
        /// </summary>
        protected async Task DriveAPortfolioRefresh(int portfolioId, params (string ReferenceId, string Name, string SourceOrder)[] rowsFromTheTracker)
        {
            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .ReturnsAsync(() => rowsFromTheTracker
                    .Select(row => new Feature
                    {
                        ReferenceId = row.ReferenceId,
                        Name = row.Name,
                        Type = "Epic",
                        State = "New",
                        StateCategory = StateCategories.ToDo,
                        Order = row.SourceOrder,
                        CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Tags = [],
                        SyncedTransitions = [],
                    })
                    .ToList());

            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            var portfolio = portfolioRepository.GetById(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            await sp.GetRequiredService<IWorkItemService>().UpdateFeaturesForPortfolio(portfolio);
        }

        // --- Driven-port probes (the store, read directly — see the red-classification note on AC-5.2) ---

        /// <summary>
        /// The persisted <c>(ReferenceId, ManualRank, Order)</c> triple for every Feature. Retention
        /// (AC-5.2) and the bounded-change complement (<c>Order</c> untouched, D5) have no port-side
        /// observable while the switch is off, so they are judged against the store.
        /// </summary>
        protected List<(string ReferenceId, int? ManualRank, string SourceOrder)> ReadStoredOrderingColumns()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return context.Features
                .AsNoTracking()
                .OrderBy(feature => feature.Id)
                .Select(feature => new { feature.ReferenceId, feature.ManualRank, feature.Order })
                .ToList()
                .Select(row => (row.ReferenceId, row.ManualRank, row.Order))
                .ToList();
        }

        // --- Writable-batch port under test (OQ-1 closure) ---

        /// <summary>
        /// Builds the real <see cref="RbacAdministrationService"/> over an isolated in-memory store so
        /// the four early-return branches of <c>GetWritablePortfolioIdsAsync</c> are exercised against
        /// production logic, not against the claims-driven double. Mirrors
        /// <c>RbacAdministrationServiceTest.CreateSubject</c>.
        /// </summary>
        protected static RbacAdministrationService BuildRealRbacService(
            LighthouseAppContext context,
            Mock<ILicenseService> licenseService,
            Mock<ICurrentUserProfileService> currentUserProfileService,
            bool rbacEnabled)
        {
            var configuration = Options.Create(new AuthorizationConfiguration
            {
                Enabled = rbacEnabled,
                EmergencySystemAdminSubjects = [],
            });

            return new RbacAdministrationService(
                context,
                configuration,
                licenseService.Object,
                currentUserProfileService.Object,
                Mock.Of<ILogger<RbacAdministrationService>>());
        }

        protected static LighthouseAppContext BuildIsolatedContext()
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new LighthouseAppContext(options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
        }

        protected static ClaimsPrincipal PrincipalFor(string subject)
        {
            return new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], "TestAuthentication"));
        }

        private void ApplyIdentity(string subject, string grants)
        {
            Client.DefaultRequestHeaders.Remove(TestAuthHandler.SubjectHeader);
            Client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
            Client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, subject);
            if (!string.IsNullOrEmpty(grants))
            {
                Client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, grants);
            }
        }
    }
}
