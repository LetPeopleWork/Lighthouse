using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public sealed class TestWebApplicationFactory<T> : WebApplicationFactory<T> where T : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                RemoveServices(services);

                // Create open SqliteConnection so EF won't automatically close it.
                services.AddSingleton<DbConnection>(container =>
                {
                    var connection = new SqliteConnection("DataSource=:memory:");
                    connection.Open();

                    return connection;
                });

                services.AddDbContext<Data.LighthouseAppContext>((container, options) =>
                {
                    var connection = container.GetRequiredService<DbConnection>();
                    options.UseSqlite(connection);
                });
            });
        }

        private void RemoveServices(IServiceCollection services)
        {
            RemoveAllDbContextFromServices(services);
            RemoveHostedServices(services);
        }

        private void RemoveHostedServices(IServiceCollection services)
        {
            services.RemoveAll<IHostedService>();
        }

        private void RemoveAllDbContextFromServices(IServiceCollection services)
        {
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<Data.LighthouseAppContext>));

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
    }
}
