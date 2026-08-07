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
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
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
        /// Reads who owns the order on this instance. <c>SystemAdmin</c>-guarded (AC-2.7), not premium —
        /// an unlicensed instance must still be able to see that it follows the tracker.
        /// </summary>
        protected Task<(HttpStatusCode Status, string Body)> GetOrderingPolicy()
            => Send(() => Client.GetAsync("/api/latest/appsettings/FeatureOrdering"));

        /// <summary>
        /// Hands ordering ownership over, or gives it back. <c>SystemAdmin</c> + premium (AC-2.5, AC-2.7).
        /// The body is written as raw JSON on purpose: the policy is a wire contract this slice does not
        /// yet own a type for, so the scenario cannot go green by compiling against one.
        /// </summary>
        protected Task<(HttpStatusCode Status, string Body)> SetOrderingPolicy(string policy)
        {
            var body = new StringContent($"{{\"policy\":\"{policy}\"}}", System.Text.Encoding.UTF8, "application/json");
            return Send(() => Client.PutAsync("/api/latest/appsettings/FeatureOrdering", body));
        }

        /// <summary>
        /// OWED AT GREEN — delete this once the ordering-policy routes are mapped. An unmapped route falls
        /// through to the SPA fallback, which throws when the test host has no <c>wwwroot</c>: the scenario
        /// then dies on host plumbing and its own assertion never runs (SETUP_FAILURE, not RED). This
        /// reports the 404 the unmapped route really is, so every scenario fails on its own Then. Leaving
        /// it after the routes exist would report a future accidental un-mapping as "unimplemented"
        /// instead of surfacing it as the routing regression it would be. Same trap, same fix and same
        /// debt as slice 01's <c>GetAllFeatures</c> catch.
        /// </summary>
        private static async Task<(HttpStatusCode Status, string Body)> Send(Func<Task<HttpResponseMessage>> request)
        {
            try
            {
                var response = await request();
                return (response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("SPA default page", StringComparison.Ordinal))
            {
                return (HttpStatusCode.NotFound, "<no route mapped for the ordering policy port>");
            }
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
