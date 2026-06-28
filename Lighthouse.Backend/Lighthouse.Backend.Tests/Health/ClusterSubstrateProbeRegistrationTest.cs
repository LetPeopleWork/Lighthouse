using System.Data.Common;
using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.Health
{
    [TestFixture]
    public class ClusterSubstrateProbeRegistrationTest
    {
        private const string ReadyTag = "ready";
        private const string StartupTag = "startup";

        // The substrate probe is destructive by design (pg_terminate_backend to test reclaim). It must
        // gate startup but NOT run on every readiness probe, or a healthy fleet logs 57P01 forever
        // (epic-5306 manual-test finding). Lock it to the startup endpoint only.
        [Test]
        public void ClusterSubstrate_WhenRedisConfigured_IsStartupOnly_NotReadiness()
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                    builder.UseSetting("ConnectionStrings:Redis", "localhost:6399");
                    builder.ConfigureServices(ReplaceWithSqlite);
                });

            var registration = factory.Services
                .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
                .Value.Registrations
                .SingleOrDefault(r => r.Name == "cluster-substrate");

            Assert.That(registration, Is.Not.Null, "cluster-substrate must be registered when Redis is configured");
            Assert.Multiple(() =>
            {
                Assert.That(registration!.Tags, Does.Contain(StartupTag), "substrate must gate startup");
                Assert.That(registration.Tags, Does.Not.Contain(ReadyTag), "substrate must NOT run on readiness (it is destructive)");
            });
        }

        private static void ReplaceWithSqlite(IServiceCollection services)
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
                options.UseSqlite(
                    $"DataSource=SubstrateReg_{Path.GetRandomFileName().Replace(".", "")}.db;Pooling=False",
                    sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));
        }
    }
}
