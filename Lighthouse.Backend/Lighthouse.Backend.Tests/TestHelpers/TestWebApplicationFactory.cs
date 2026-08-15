using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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

    public class TestWebApplicationFactory<T> : WebApplicationFactory<T> where T : class
    {
        private readonly string databaseFileName = $"IntegrationTests_{Path.GetRandomFileName().Replace(".", "")}.db";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Set test environment to skip migrations in Program.cs
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(IntegrationTestEncryptionKey.AsConfiguration());
            });

            builder.ConfigureServices(services =>
            {
                RemoveServices(services);

                services.AddDbContext<LighthouseAppContext>(options =>
                {
                    options.UseSqlite($"DataSource={databaseFileName};Pooling=False",
                        x => x.MigrationsAssembly("Lighthouse.Migrations.Sqlite"));
                });
            });
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

        private static void RemoveAllDbContextFromServices(IServiceCollection services)
        {
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LighthouseAppContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }
        }

        private void DeleteDatabaseFile()
        {
            if (File.Exists(databaseFileName))
            {
                File.Delete(databaseFileName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DeleteDatabaseFile();
        }

        public static WebApplicationFactory<T> WithTestAuthentication(TestWebApplicationFactory<T> root)
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