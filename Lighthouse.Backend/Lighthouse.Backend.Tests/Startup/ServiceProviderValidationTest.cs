using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace Lighthouse.Backend.Tests.Startup
{
    public class ServiceProviderValidationTest
    {
        [Test]
        public void ServiceContainer_BuildsWithoutScopeViolations_WhenValidateScopesIsEnforced()
        {
            using var factory = new ValidatingFactory();

            Assert.DoesNotThrow(
                () => _ = factory.Services,
                "Service container failed strict scope validation. A Singleton is consuming a Scoped service.");
        }

        private sealed class ValidatingFactory : WebApplicationFactory<Backend.Program>
        {
            private readonly string databaseFileName = $"DiValidation_{Path.GetRandomFileName().Replace(".", "")}.db";

            protected override IHost CreateHost(IHostBuilder builder)
            {
                builder.UseDefaultServiceProvider(options =>
                {
                    options.ValidateScopes = true;
                    options.ValidateOnBuild = true;
                });

                return base.CreateHost(builder);
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IHostedService>();
                    RemoveDbContextRegistrations(services);

                    services.AddDbContext<LighthouseAppContext>(options =>
                    {
                        options.UseSqlite(
                            $"DataSource={databaseFileName}",
                            DatabaseConfigurator.ConfigureSqlite);
                    });
                });
            }

            // The product's own configuration step must go too: left registered, it runs before ours whenever
            // a scope builds options and opens a connection to the product's database file.
            private static void RemoveDbContextRegistrations(IServiceCollection services)
            {
                services.RemoveAll<DbContextOptions<LighthouseAppContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<LighthouseAppContext>>();
                services.RemoveAll<DbConnection>();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (File.Exists(databaseFileName))
                {
                    File.Delete(databaseFileName);
                }
            }
        }
    }
}
