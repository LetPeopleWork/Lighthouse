using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// Real-provider migration probe for the additive <c>BlockedStalenessThresholdDays</c> column
    /// (twin of StalenessThresholdDays). Proves against SQLite + Postgres that the generated migration
    /// applies cleanly and the column defaults to 0 for both Team and Portfolio entities.
    /// InMemory misses migrations — real providers required.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedStalenessThresholdMigrationTests
    {
        [Test]
        public async Task Migration_OnSqlite_PersistsColumnAndDefaultsToZero()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-bst-5074-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteProvider($"Data Source={databaseFile};Pooling=False");
                await MigrateAsync(provider);
                await AssertColumnDefaultsToZeroAsync(provider);
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        [Test]
        public async Task Migration_OnPostgres_PersistsColumnAndDefaultsToZero()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();

            await using var provider = BuildPostgresProvider(postgres.GetConnectionString());
            await MigrateAsync(provider);
            await AssertColumnDefaultsToZeroAsync(provider);
        }

        private static async Task MigrateAsync(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var pending = await context.Database.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty, "the generated AddBlockedStalenessThresholdDays migration must apply cleanly on a real provider");
        }

        private static async Task AssertColumnDefaultsToZeroAsync(ServiceProvider provider)
        {
            int teamId;
            int portfolioId;
            using (var writeScope = provider.CreateScope())
            {
                var context = writeScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

                var team = NewTeam();
                var portfolio = NewPortfolio();

                context.Teams.Add(team);
                context.Portfolios.Add(portfolio);
                await context.SaveChangesAsync();

                teamId = team.Id;
                portfolioId = portfolio.Id;
            }

            using var readScope = provider.CreateScope();
            var readContext = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var reloadedTeam = await readContext.Teams.SingleAsync(t => t.Id == teamId);
            var reloadedPortfolio = await readContext.Portfolios.SingleAsync(p => p.Id == portfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloadedTeam.BlockedStalenessThresholdDays, Is.Zero,
                    "blockedStalenessThresholdDays must default to 0 on Team");
                Assert.That(reloadedPortfolio.BlockedStalenessThresholdDays, Is.Zero,
                    "blockedStalenessThresholdDays must default to 0 on Portfolio");
            }
        }

        private static Team NewTeam()
        {
            return new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DoneItemsCutoffDays = 0,
            };
        }

        private static Portfolio NewPortfolio()
        {
            return new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DoneItemsCutoffDays = 0,
            };
        }

        private static ServiceProvider BuildSqliteProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

            return services.BuildServiceProvider();
        }

        private static ServiceProvider BuildPostgresProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            return services.BuildServiceProvider();
        }
    }
}
