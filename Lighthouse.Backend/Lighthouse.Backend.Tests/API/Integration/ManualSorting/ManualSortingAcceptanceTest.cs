using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
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

        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(RootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => LicenseServiceMock.Object);
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
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();

            var feature = new Feature
            {
                Name = name,
                ReferenceId = $"FTR-{Guid.NewGuid():N}",
                Type = "Epic",
                State = stateCategory == StateCategories.Done ? "Done" : "New",
                StateCategory = stateCategory,
                Order = sourceOrder,
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
