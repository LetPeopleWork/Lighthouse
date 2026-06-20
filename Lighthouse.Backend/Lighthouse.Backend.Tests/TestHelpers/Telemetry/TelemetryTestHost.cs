using System.Data.Common;
using System.Net;
using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Lighthouse.Backend.Tests.TestHelpers.Telemetry
{
    public sealed class TelemetryTestHost : IDisposable
    {
        public const string MetricsPath = "/metrics";

        private readonly string databaseFileName = $"Telemetry_{Path.GetRandomFileName().Replace(".", "")}.db";
        private readonly WebApplicationFactory<Program> factory;
        private readonly HttpClient client;

        public TelemetryTestHost(bool? telemetryEnabled = null)
        {
            factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                    if (telemetryEnabled.HasValue)
                    {
                        builder.UseSetting("Telemetry:Enabled", telemetryEnabled.Value ? "true" : "false");
                    }
                    builder.ConfigureServices(services =>
                    {
                        ReplaceDbContext(services, $"DataSource={databaseFileName};Pooling=False");
                    });
                });

            MigrateDatabase();

            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        public async Task<TelemetryResponse> GetMetricsAsync()
        {
            using var response = await client.GetAsync(MetricsPath);
            var body = await response.Content.ReadAsStringAsync();
            return new TelemetryResponse(response.StatusCode, body);
        }

        public async Task<HttpStatusCode> GetAsync(string path)
        {
            using var response = await client.GetAsync(path);
            return response.StatusCode;
        }

        private void MigrateDatabase()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            context.Database.Migrate();
        }

        private static void ReplaceDbContext(IServiceCollection services, string connectionString)
        {
            var dbContextOptions = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LighthouseAppContext>));
            if (dbContextOptions != null)
            {
                services.Remove(dbContextOptions);
            }

            var dbConnection = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            if (dbConnection != null)
            {
                services.Remove(dbConnection);
            }

            services.RemoveAll<IHostedService>();

            services.AddDbContext<LighthouseAppContext>(options =>
            {
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite"));
            });
        }

        public void Dispose()
        {
            client.Dispose();
            factory.Dispose();
            DeleteDatabaseFile();
        }

        private void DeleteDatabaseFile()
        {
            if (File.Exists(databaseFileName))
            {
                File.Delete(databaseFileName);
            }
        }
    }
}
