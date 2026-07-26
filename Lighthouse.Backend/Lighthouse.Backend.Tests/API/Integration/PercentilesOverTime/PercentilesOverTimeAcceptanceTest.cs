using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
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
            SeedPercentilesSnapshot(new PercentilesOverTimeSnapshot
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
        }

        /// <summary>
        /// Metric-family-agnostic seeding: the snapshot table holds every family, so a slice that adds
        /// one (Work Item Age, epic-5427 slice-02) seeds through here rather than growing the CT helper
        /// another argument.
        /// </summary>
        protected void SeedPercentilesSnapshot(PercentilesOverTimeSnapshot snapshot)
        {
            using var scope = Factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPercentilesOverTimeSnapshotRepository>();
            repo.Add(snapshot);
            repo.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Family-agnostic process-behaviour seeding: the snapshot table holds every family
        /// (<see cref="ProcessBehaviorMetricType"/>), so a slice that adds one (the five remaining
        /// families, epic-5427 slice-04) seeds through here rather than growing a per-family helper.
        /// </summary>
        protected void SeedProcessBehaviorSnapshot(ProcessBehaviorSnapshot snapshot)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProcessBehaviorSnapshotRepository>();
            repository.Add(snapshot);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Seeds one item that is still in progress today, aged <paramref name="ageInDays"/> days
        /// (inclusive of its start day — the definition <see cref="Lighthouse.Backend.Models.WorkItemBase.AgeOnDay"/> uses).
        /// </summary>
        protected void SeedInProgressWorkItem(int teamId, string referenceId, int ageInDays)
        {
            using var scope = Factory.Services.CreateScope();
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            // Anchored in UTC: every persisted DateTime goes through UtcDateTimeConverter, which converts a
            // Local-kind value with ToUniversalTime() — a local midnight would land on the previous UTC day
            // and inflate the age by one, exactly as real synced (UTC) work-item dates never would.
            var startedDate = DateTime.UtcNow.Date.AddDays(-(ageInDays - 1));
            workItemRepository.Add(new WorkItem
            {
                TeamId = teamId,
                ReferenceId = referenceId,
                Name = $"Item {referenceId}",
                Type = "Story",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                CreatedDate = startedDate,
                StartedDate = startedDate,
                ClosedDate = null,
                Order = referenceId,
            });
            workItemRepository.Save().GetAwaiter().GetResult();
        }

        // --- Write-side driving-port interaction (the recording pipeline runs off the refresh event) ---

        protected async Task TheTeamMetricsRefreshCompletes(int teamId)
        {
            using var scope = Factory.Services.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            await dispatcher.PublishAsync(new TeamDataRefreshed(teamId));
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

        protected async Task<(HttpStatusCode Status, string Body)> GetTeamPercentilesOverTime(int teamId, MetricType metricType, int? horizon)
        {
            Client.AsTeamAdmin(teamId);
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/percentiles-over-time{BuildQuery(metricType, horizon)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetPortfolioPercentilesOverTime(int portfolioId, MetricType metricType, int? horizon)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}/metrics/percentiles-over-time{BuildQuery(metricType, horizon)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private static string BuildQuery(MetricType metricType, int? horizon)
        {
            var query = $"?metricType={metricType}";
            return horizon.HasValue ? $"{query}&horizon={horizon.Value}" : query;
        }

        /// <summary>
        /// The process-behaviour series read port. The family travels as a RAW string so a slice can
        /// exercise a genuinely-unknown family name (the 400 guard) as well as every declared one.
        /// </summary>
        protected async Task<(HttpStatusCode Status, string Body)> GetTeamProcessBehaviorOverTime(int teamId, string? type)
        {
            Client.AsTeamAdmin(teamId);
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/process-behavior-over-time{BuildTypeQuery(type)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        protected async Task<(HttpStatusCode Status, string Body)> GetPortfolioProcessBehaviorOverTime(int portfolioId, string? type)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}/metrics/process-behavior-over-time{BuildTypeQuery(type)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// An omitted family must be genuinely absent from the query string — sending an empty value
        /// would exercise the model binder, not the endpoint's documented default.
        /// </summary>
        private static string BuildTypeQuery(string? type) => type is null ? string.Empty : $"?type={type}";
    }
}
