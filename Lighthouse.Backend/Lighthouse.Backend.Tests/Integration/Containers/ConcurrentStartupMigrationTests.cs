using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class ConcurrentStartupMigrationTests
    {
        private const string MigrationsAssemblyName = "Lighthouse.Migrations.Postgres";

        [Test]
        public async Task Startup_ThreeConcurrentHostsOneRealPostgres_MigrationsAppliedExactlyOnce()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            var connectionString = postgres.GetConnectionString();

            var failures = await StartHostsConcurrentlyAsync(connectionString, hostCount: 3);

            Assert.That(failures, Is.Empty,
                "Three replicas booting against one fresh Postgres must apply migrations exactly once: " +
                "exactly one acquires the advisory lock and migrates, the others wait then observe an up-to-date " +
                "schema and no-op. Without coordination they race Database.Migrate() and throw. Failures: " +
                string.Join(" | ", failures.Select(f => f.Message)));

            await AssertSchemaFullyMigratedAsync(connectionString);
        }

        [Test]
        public async Task Startup_MigrationLockContended_FollowersWaitThenServe()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            var connectionString = postgres.GetConnectionString();

            var failures = await StartHostsConcurrentlyAsync(connectionString, hostCount: 5);

            Assert.That(failures, Is.Empty,
                "Under heavy contention the followers must block on the advisory lock until the leader finishes " +
                "migrating, then observe an up-to-date schema and serve — never error on a half-migrated schema. " +
                "Failures: " + string.Join(" | ", failures.Select(f => f.Message)));

            await AssertSchemaServesQueriesAsync(connectionString);
        }

        [Test]
        public async Task Startup_SingleInstance_AutoMigratesOnBootUnchanged()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            var connectionString = postgres.GetConnectionString();

            var failures = await StartHostsConcurrentlyAsync(connectionString, hostCount: 1);

            Assert.That(failures, Is.Empty,
                "A single instance acquires the advisory lock uncontended and migrates on boot exactly as today " +
                "(the lock degrades to a no-op at one instance, D1). Failures: " +
                string.Join(" | ", failures.Select(f => f.Message)));

            await AssertSchemaFullyMigratedAsync(connectionString);
            await AssertSchemaServesQueriesAsync(connectionString);
        }

        private static async Task<IReadOnlyList<Exception>> StartHostsConcurrentlyAsync(string connectionString, int hostCount)
        {
            using var allHostsReady = new Barrier(hostCount);

            var tasks = Enumerable.Range(0, hostCount)
                .Select(_ => Task.Run(() => ApplyMigrationsOnFreshHost(connectionString, allHostsReady)))
                .ToList();

            var results = await Task.WhenAll(tasks);
            return results.Where(result => result is not null).Select(result => result!).ToList();
        }

        private static Exception? ApplyMigrationsOnFreshHost(string connectionString, Barrier allHostsReady)
        {
            using var provider = BuildHostServiceProvider(connectionString);
            try
            {
                allHostsReady.SignalAndWait();
                using var scope = provider.CreateScope();
                DatabaseConfigurator.ApplyMigrations(scope.ServiceProvider);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private static async Task AssertSchemaFullyMigratedAsync(string connectionString)
        {
            await using var provider = BuildHostServiceProvider(connectionString);
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var pending = await context.Database.GetPendingMigrationsAsync();
            Assert.That(pending, Is.Empty, "schema must be fully migrated after concurrent startup");

            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            var defined = context.Database.GetMigrations().ToList();
            Assert.That(applied, Is.EquivalentTo(defined),
                "every defined migration must be recorded exactly once in __EFMigrationsHistory");
        }

        private static async Task AssertSchemaServesQueriesAsync(string connectionString)
        {
            await using var provider = BuildHostServiceProvider(connectionString);
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var teamCount = await context.Teams.CountAsync();
            Assert.That(teamCount, Is.Zero,
                "a fully-migrated schema serves queries against migrated tables");
        }

        private static ServiceProvider BuildHostServiceProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseNpgsql(connectionString,
                    npgsql => npgsql.MigrationsAssembly(MigrationsAssemblyName)));

            return services.BuildServiceProvider();
        }
    }
}
