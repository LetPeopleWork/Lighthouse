using Lighthouse.Backend.Services.Implementation.Encryption;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.Health
{
    [TestFixture]
    public class DataProtectionKeyRingTest
    {
        // At replicaCount>1 every pod must share the Data Protection key ring, or the OIDC login
        // cookie issued by one pod is undecryptable on another and login loops.
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

        // Only the data protection set-up is built, not the whole app: a full boot also wires the
        // scaling backplane and health checks to the configured Redis, and nothing listens there, so
        // those spent seconds failing to connect before the assertion was reached.
        private static string ResolveXmlRepositoryTypeName(bool redisConfigured)
        {
            var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
            if (redisConfigured)
            {
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Redis"] = "localhost:6399",
                });
            }

            var keyStoreDirectory = Path.Combine(Path.GetTempPath(), $"lighthouse-dp-ring-{Guid.NewGuid():N}");
            Backend.Program.ConfigureDataProtection(builder, new KeyStoreLocation(keyStoreDirectory, KeyStoreCase.ExplicitKeyStorePath));

            using var services = builder.Services.BuildServiceProvider();
            var options = services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
            Assert.That(options.XmlRepository, Is.Not.Null, "a key repository must be configured");
            return options.XmlRepository!.GetType().Name;
        }
    }
}
