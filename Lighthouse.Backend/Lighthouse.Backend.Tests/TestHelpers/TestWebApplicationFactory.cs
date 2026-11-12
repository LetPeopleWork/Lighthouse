using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Time.Testing;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Every backend integration test starts the real application, and the real application needs an
    /// encryption key. Handing one to each test host means no integration test depends on what the
    /// settings file shipped with the product happens to contain, so an edit to that file cannot turn
    /// the whole integration suite red at once for a reason unrelated to the change being made.
    ///
    /// The value is checked into a public repository deliberately. Decoded it reads
    /// "LighthouseTestFixtureKeyNotReal!", so nobody can mistake it for a secret worth keeping or
    /// paste it into a running instance by accident.
    /// </summary>
    public static class IntegrationTestEncryptionKey
    {
        public const string ConfigurationKey = "Encryption:Key";

        public const string EnvironmentVariableName = "Encryption__Key";

        public const string Value = "TGlnaHRob3VzZVRlc3RGaXh0dXJlS2V5Tm90UmVhbCE=";

        public static Dictionary<string, string?> AsConfiguration()
        {
            return new Dictionary<string, string?>
            {
                [ConfigurationKey] = Value,
            };
        }

        // Not every host in this test project is built by TestWebApplicationFactory - a handful of tests
        // stand up their own. An environment variable is the one channel all of them read, and it has to
        // be set before the first of them starts, which is what running at module load guarantees.
#pragma warning disable CA2255 // Test assembly, not a library anyone references: nothing here is imported into another program.
        [ModuleInitializer]
#pragma warning restore CA2255
        internal static void SupplyToEveryHostInThisProcess()
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, Value);
        }
    }

    /// <summary>
    /// By default SQLite waits for the disk to confirm every commit, and with several test hosts writing at
    /// once that wait was most of the time it took to create and seed a test database. Each test's database is
    /// thrown away afterwards, so it has no crash to survive and its commits need not wait. Only test databases
    /// get this; the product keeps its own setting.
    /// </summary>
    internal sealed class NoWaitingForTheDisk : DbConnectionInterceptor
    {
        public static readonly NoWaitingForTheDisk Instance = new();

        private const string CommitWithoutWaitingForTheDisk = "PRAGMA synchronous=OFF;";

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            if (connection is not SqliteConnection)
            {
                return;
            }

            using var pragma = connection.CreateCommand();
            pragma.CommandText = CommitWithoutWaitingForTheDisk;
            pragma.ExecuteNonQuery();
        }

        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (connection is not SqliteConnection)
            {
                return;
            }

            await using var pragma = connection.CreateCommand();
            pragma.CommandText = CommitWithoutWaitingForTheDisk;
            await pragma.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// A fake clock only ever moves forward, which a host shared by many tests cannot live with. This one
    /// can be put back at its starting instant by shifting what it reports; the host keeps the same
    /// instance, so nothing has to be re-registered. Move it with Advance only: the inherited SetUtcNow
    /// knows nothing of the shift, so after a Reset it would land at the wrong instant.
    /// Reset shifts the reported time only: timers and GetTimestamp stay on the underlying timeline, so a
    /// fixture that keeps timers across tests must not rely on Reset for them.
    /// </summary>
    public sealed class ResettableFakeTimeProvider : FakeTimeProvider
    {
        private readonly DateTimeOffset start;

        private TimeSpan shift = TimeSpan.Zero;

        public ResettableFakeTimeProvider(DateTimeOffset start) : base(start)
        {
            this.start = start;
        }

        public override DateTimeOffset GetUtcNow() => base.GetUtcNow() - shift;

        public void Reset() => shift = base.GetUtcNow() - start;
    }

    public class TestWebApplicationFactory<T> : WebApplicationFactory<T> where T : class
    {
        private readonly List<string> usedDatabaseFileNames = [];

        private string databaseFileName = NewDatabaseFileName();

        private readonly Lock templateCopyLock = new();

        // When on, the first scope to see a new database path starts from a copy of the prebuilt schema
        // instead of an empty file. Off by default.
        public bool UseSchemaTemplate { get; set; }

        // The host's registered TimeProvider is this clock. A fixture that keeps one host for all its tests moves it
        // freely and calls Clock.Reset() before each test to put it back at TestToday.FrozenNow.
        public ResettableFakeTimeProvider Clock { get; } = new(TestToday.FrozenNow);

        // Work that outlives its request can still hold a connection to the previous test's database. A
        // recreated database at the same path shares that connection's file locks and its -journal rollback
        // file (test databases do not use the write-ahead log), and the two corrupt each other's locks:
        // SQLite error 15 ('locking protocol'), 'disk I/O error', or a malformed database, in whichever
        // test runs next. A path no earlier test used cannot be shared.
        public void UseFreshDatabase()
        {
            usedDatabaseFileNames.Add(databaseFileName);
            databaseFileName = NewDatabaseFileName();
        }

        private static string NewDatabaseFileName()
        {
            return $"IntegrationTests_{Path.GetRandomFileName().Replace(".", "")}.db";
        }

        // Hosts are built lazily, in a test's SetUp or in a fixture's OneTimeSetUp, so the time is
        // credited to the running fixture rather than to any setup phase the fixture measures itself.
        protected override IHost CreateHost(IHostBuilder builder)
        {
            using var _ = FixtureSetupTimer.Measure(RunningFixtureName(), FixtureSetupTimer.MeasurementKind.HostBuild);
            return base.CreateHost(builder);
        }

        private static string RunningFixtureName()
        {
            var className = TestContext.CurrentContext.Test.ClassName;
            return string.IsNullOrEmpty(className) ? "(outside a test)" : className[(className.LastIndexOf('.') + 1)..];
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Set test environment to skip migrations in Program.cs
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(IntegrationTestEncryptionKey.AsConfiguration());

                // A host with the rate limiters on stays reachable after it is disposed, because their refill
                // timers capture it. Most tests do not need throttling, so hosts run without it; a fixture that
                // tests rate limiting switches it back on in its own configuration, which is added later and wins.
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimits:Enabled"] = "false" });
            });

            builder.ConfigureServices(services =>
            {
                RemoveServices(services);

                // A host that used an HTTP client stays reachable until the handler lifetime plus the
                // factory's fixed 10 s cleanup tick. No test relies on connection reuse, so keep it short.
                services.ConfigureAll<HttpClientFactoryOptions>(options => options.HandlerLifetime = TimeSpan.FromSeconds(1));

                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);

                services.AddDbContext<LighthouseAppContext>(options =>
                {
                    var path = databaseFileName;
                    if (UseSchemaTemplate)
                    {
                        CopyTemplateOnFirstUse(path);
                    }

                    options.UseSqlite($"DataSource={path};Pooling=False", DatabaseConfigurator.ConfigureSqlite);
                    options.AddInterceptors(NoWaitingForTheDisk.Instance);
                });
            });
        }

        private void CopyTemplateOnFirstUse(string path)
        {
            lock (templateCopyLock)
            {
                if (!File.Exists(path))
                {
                    TestDatabaseTemplate.CopyTo(path);
                }
            }
        }

        private static void RemoveServices(IServiceCollection services)
        {
            RemoveAllDbContextFromServices(services);
            RemoveHostedServices(services);
        }

        private static void RemoveHostedServices(IServiceCollection services)
        {
            services.RemoveAll<IHostedService>();
        }

        // Removing the built options alone is not enough: the product's own configuration step stays
        // registered and runs first every time a scope builds options, opening a connection to the
        // product's database file that nothing ever closes.
        private static void RemoveAllDbContextFromServices(IServiceCollection services)
        {
            services.RemoveAll<DbContextOptions<LighthouseAppContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<LighthouseAppContext>>();
            services.RemoveAll<DbConnection>();
        }

        private static void DeleteDatabaseFiles(string fileName)
        {
            foreach (var path in new[] { fileName, $"{fileName}-wal", $"{fileName}-shm", $"{fileName}-journal" })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreach (var fileName in usedDatabaseFileNames.Append(databaseFileName))
            {
                DeleteDatabaseFiles(fileName);
            }
        }

        public static WebApplicationFactory<T> WithTestAuthentication(WebApplicationFactory<T> root)
        {
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "true",
                        ["Authentication:Authority"] = "https://example.test/oidc",
                        ["Authentication:ClientId"] = "lighthouse-test",
                        ["Authentication:ClientSecret"] = "test-secret",
                        ["Authentication:MetadataAddress"] = "https://example.test/oidc/.well-known/openid-configuration",
                        ["Authentication:RequireHttpsMetadata"] = "false",
                        ["Authorization:Enabled"] = "true",
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(defaultOptions =>
                    {
                        defaultOptions.DefaultScheme = TestAuthHandler.SchemeName;
                        defaultOptions.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        defaultOptions.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                        defaultOptions.DefaultForbidScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });

                    services.RemoveAll<IRbacAdministrationService>();
                    services.AddScoped<IRbacAdministrationService, ClaimsDrivenRbacAdministrationService>();
                });
            });
        }
    }
}