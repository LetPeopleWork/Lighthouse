using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Health;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace Lighthouse.Backend.Tests.TestHelpers.Health
{
    public sealed class HealthCheckTestHost : IDisposable
    {
        public const string LivePath = "/health/live";
        public const string ReadyPath = "/health/ready";
        public const string StartupPath = "/health/startup";

        private readonly string databaseFileName = $"HealthChecks_{Path.GetRandomFileName().Replace(".", "")}.db";
        private readonly WebApplicationFactory<Program> factory;
        private readonly HttpClient client;

        public HealthCheckTestHost(HealthDatabaseState databaseState)
        {
            factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                    builder.ConfigureServices(services =>
                    {
                        ReplaceDbContext(services, ConnectionStringFor(databaseState));
                    });
                });

            ApplyDatabaseState(databaseState);

            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        public IReadinessState ReadinessState => factory.Services.GetRequiredService<IReadinessState>();

        public void TriggerApplicationStopping()
            => factory.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        public Task<ProbeResponse> GetLiveAsync() => GetProbeAsync(LivePath);

        public Task<ProbeResponse> GetReadyAsync() => GetProbeAsync(ReadyPath);

        public Task<ProbeResponse> GetStartupAsync() => GetProbeAsync(StartupPath);

        private async Task<ProbeResponse> GetProbeAsync(string path)
        {
            using var response = await client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            return new ProbeResponse(response.StatusCode, body);
        }

        private string ConnectionStringFor(HealthDatabaseState databaseState)
        {
            if (databaseState == HealthDatabaseState.Unreachable)
            {
                return $"DataSource=health-unreachable-{databaseFileName};Mode=ReadOnly;Pooling=False";
            }

            return $"DataSource={databaseFileName};Pooling=False";
        }

        private void ApplyDatabaseState(HealthDatabaseState databaseState)
        {
            if (databaseState == HealthDatabaseState.Unreachable)
            {
                return;
            }

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            if (databaseState == HealthDatabaseState.ReachableAndMigrated)
            {
                context.Database.Migrate();
                return;
            }

            context.Database.EnsureCreated();
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
