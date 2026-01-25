using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public sealed class TestWebApplicationFactory<T> : WebApplicationFactory<T> where T : class
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
    }
}