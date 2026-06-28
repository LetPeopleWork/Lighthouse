using System.Data.Common;
using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.Health
{
    [TestFixture]
    public class DataProtectionKeyRingTest
    {
        // At replicaCount>1 every pod must share the Data Protection key ring, or the OIDC login
        // cookie issued by one pod is undecryptable on another and login loops (epic-5306 finding).
        // With Redis configured the ring must persist to Redis, not a per-pod local filesystem.
        [Test]
        public void WhenRedisConfigured_KeyRingPersistsToRedis_NotFilesystem()
        {
            var repository = ResolveXmlRepositoryTypeName(redisConfigured: true);
            Assert.That(repository, Is.EqualTo("RedisXmlRepository"));
        }

        [Test]
        public void WithoutRedis_KeyRingPersistsToFilesystem()
        {
            var repository = ResolveXmlRepositoryTypeName(redisConfigured: false);
            Assert.That(repository, Is.EqualTo("FileSystemXmlRepository"));
        }

        private static string ResolveXmlRepositoryTypeName(bool redisConfigured)
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                    if (redisConfigured)
                    {
                        builder.UseSetting("ConnectionStrings:Redis", "localhost:6399");
                    }

                    builder.ConfigureServices(ReplaceWithSqlite);
                });

            var options = factory.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
            Assert.That(options.XmlRepository, Is.Not.Null, "a key repository must be configured");
            return options.XmlRepository!.GetType().Name;
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
                    $"DataSource=DpRing_{Path.GetRandomFileName().Replace(".", "")}.db;Pooling=False",
                    sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));
        }
    }
}
