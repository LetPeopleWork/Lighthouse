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

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public class TestWebApplicationFactory<T> : WebApplicationFactory<T> where T : class
    {
        private readonly string databaseFileName = $"IntegrationTests_{Path.GetRandomFileName().Replace(".", "")}.db";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Set test environment to skip migrations in Program.cs
            builder.UseEnvironment("Testing");
            
            builder.ConfigureServices(services =>
            {
                RemoveServices(services);

                services.AddDbContext<LighthouseAppContext>(options =>
                {
                    options.UseSqlite($"DataSource={databaseFileName}",
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