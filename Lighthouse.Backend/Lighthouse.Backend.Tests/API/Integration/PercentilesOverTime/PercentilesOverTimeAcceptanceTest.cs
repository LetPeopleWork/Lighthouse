using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER acceptance harness (Epic 5427 — Percentiles Over Time). Single source of truth for HOW
    /// scenarios reach the system: through the real ASP.NET host on real SQLite (Pillar 3 —
    /// <see cref="WebApplicationFactory{TEntryPoint}"/>), via the team/portfolio metrics read ports over
    /// real EF. Only the license port (external/non-deterministic) is faked. Per-slice fixtures inherit
    /// these step-support methods and add their own business-language Given/When/Then steps.
    /// </summary>
    public abstract class PercentilesOverTimeAcceptanceTest
    {
        private static int testDateOffset;

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;
        protected Mock<ILicenseService> LicenseServiceMock = null!;
        protected DateTime SyncDay;

        [SetUp]
        public void Init()
        {
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 90;
            SyncDay = new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);

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

        protected int SeedTeam()
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 0,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story", "Bug"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        protected int SeedPortfolio()
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
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

        protected void SeedCycleTimePercentilesSnapshot(int ownerId, OwnerType ownerType, DateOnly recordedAt, int horizon, int p50, int p70, int p85, int p95)
        {
            using var scope = Factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPercentilesOverTimeSnapshotRepository>();
            repo.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                RecordedAt = recordedAt,
                MetricType = MetricType.CycleTime,
                Horizon = horizon,
                P50 = p50,
                P70 = p70,
                P85 = p85,
                P95 = p95,
            });
            repo.Save().GetAwaiter().GetResult();
        }

        // --- Read-side driving-port interactions ---

        protected async Task<(HttpStatusCode Status, string Body)> GetTeamPercentilesOverTime(int teamId, int horizon)
        {
            Client.AsTeamAdmin(teamId);
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/percentiles-over-time?horizon={horizon}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetPortfolioPercentilesOverTime(int portfolioId, int horizon)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}/metrics/percentiles-over-time?horizon={horizon}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}
