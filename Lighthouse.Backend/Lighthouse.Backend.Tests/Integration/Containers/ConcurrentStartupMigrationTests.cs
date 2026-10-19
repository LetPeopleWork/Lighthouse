using System.Data;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class ConcurrentStartupMigrationTests
    {
        private const string MigrationsAssemblyName = "Lighthouse.Migrations.Postgres";

        // A host that never reaches the starting line would leave the others waiting forever, so every
        // wait on the hosts is bounded and a hang fails the test instead of stalling the run.
        private static readonly TimeSpan HostsMustFinishWithin = TimeSpan.FromSeconds(30);

        // Started by the first Postgres case, so a run of the SQLite cases alone needs no Docker.
        private SharedPostgresContainer? postgres;

        [OneTimeTearDown]
        public async Task StopPostgres()
        {
            if (postgres is not null)
            {
                await postgres.DisposeAsync();
            }
        }

        [Test]
        public async Task Startup_ThreeConcurrentHostsOneRealPostgres_MigrationsAppliedExactlyOnce()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();

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
            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();

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
            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();

            var failures = await StartHostsConcurrentlyAsync(connectionString, hostCount: 1);

            Assert.That(failures, Is.Empty,
                "A single instance acquires the advisory lock uncontended and migrates on boot exactly as today " +
                "(the lock degrades to a no-op at one instance). Failures: " +
                string.Join(" | ", failures.Select(f => f.Message)));

            await AssertSchemaFullyMigratedAsync(connectionString);
            await AssertSchemaServesQueriesAsync(connectionString);
        }

        [Test]
        public async Task Startup_PostgresProvider_ClosesTheMigrationConnectionItOpened()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();

            await using var provider = BuildHostServiceProvider(await postgres.ConnectionStringToANewCreatedDatabaseAsync());
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            DatabaseConfigurator.ApplyMigrations(scope.ServiceProvider);

            Assert.That(context.Database.GetDbConnection().State, Is.EqualTo(ConnectionState.Closed),
                "the advisory-lock path opens a dedicated connection to hold the lock across the migration and " +
                "must close it afterwards, leaving the connection exactly as it found it");
        }

        [Test]
        public async Task Startup_NonPostgresProvider_AppliesMigrationsViaPlainMigrate()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-slice04-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildSqliteHostServiceProvider($"Data Source={databaseFile}");
                using var scope = provider.CreateScope();

                DatabaseConfigurator.ApplyMigrations(scope.ServiceProvider);

                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var pending = await context.Database.GetPendingMigrationsAsync();
                var teamCount = await context.Teams.CountAsync();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(pending, Is.Empty,
                        "a non-Postgres provider must migrate on boot via the plain Migrate() path (no advisory lock)");
                    Assert.That(teamCount, Is.Zero,
                        "the migrated SQLite schema serves queries against migrated tables");
                }
            }
            finally
            {
                // Disposing the context hands its connection back to the pool rather than closing it, so
                // the file is still open here. Deleting it then fails on Windows, while Linux allows it
                // and says nothing.
                SqliteConnection.ClearAllPools();

                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        // Each host blocks until every host is ready and then migrates synchronously. On pool threads
        // that parks one worker per host and stalls every other test running alongside until the pool
        // grows, so each host gets a thread of its own.
        private static async Task<IReadOnlyList<Exception>> StartHostsConcurrentlyAsync(string connectionString, int hostCount)
        {
            using var allHostsReady = new Barrier(hostCount);

            var hosts = Enumerable.Range(0, hostCount)
                .Select(_ => Task.Factory.StartNew(
                    () => ApplyMigrationsOnFreshHost(connectionString, allHostsReady),
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default))
                .ToList();

            try
            {
                var results = await Task.WhenAll(hosts).WaitAsync(HostsMustFinishWithin);
                return results.OfType<Exception>().ToList();
            }
            catch (TimeoutException)
            {
                return [new TimeoutException($"The hosts did not all finish starting up within {HostsMustFinishWithin.TotalSeconds} s; one is stuck.")];
            }
        }

        private static Exception? ApplyMigrationsOnFreshHost(string connectionString, Barrier allHostsReady)
        {
            try
            {
                using var provider = BuildHostServiceProvider(connectionString);
                if (!allHostsReady.SignalAndWait(HostsMustFinishWithin))
                {
                    return new TimeoutException($"A host waited {HostsMustFinishWithin.TotalSeconds} s for the other hosts to be ready; one never arrived.");
                }

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

        private static ServiceProvider BuildSqliteHostServiceProvider(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(connectionString,
                    sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

            return services.BuildServiceProvider();
        }
    }
}
