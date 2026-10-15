using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.Health
{
    [TestFixture]
    public class DataProtectionKeyRingTest
    {
        private string keyStoreDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            keyStoreDirectory = Path.Combine(Path.GetTempPath(), $"lighthouse-dp-ring-{Guid.NewGuid():N}");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(keyStoreDirectory))
            {
                Directory.Delete(keyStoreDirectory, recursive: true);
            }
        }

        // At replicaCount>1 every pod must share the Data Protection key ring, or the OIDC login
        // cookie issued by one pod is undecryptable on another and login loops.
        // With Redis configured the ring must persist to Redis, not a per-pod local filesystem.
        // Only the data protection set-up is built here: a full boot also wires the scaling backplane
        // and health checks to the configured Redis, where nothing listens, and spends seconds
        // failing to connect.
        [Test]
        public void WhenRedisConfigured_KeyRingPersistsToRedis_NotFilesystem()
        {
            var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "localhost:6399",
            });

            Backend.Program.ConfigureDataProtection(builder, new KeyStoreLocation(keyStoreDirectory, KeyStoreCase.ExplicitKeyStorePath));

            using var services = builder.Services.BuildServiceProvider();
            var repository = services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository;
            Assert.That(repository?.GetType().Name, Is.EqualTo("RedisXmlRepository"));
        }

        // A full boot, so this also fails if the app stops wiring data protection at all: without it
        // the framework silently falls back to a per-machine key ring outside the key store.
        [Test]
        public void WithoutRedis_TheBootedApp_KeepsItsKeyRingInTheConfiguredKeyStore()
        {
            using var root = new TestWebApplicationFactory<Program>();
            using var factory = root.WithWebHostBuilder(builder => builder.UseSetting("Encryption:KeyStorePath", keyStoreDirectory));

            var repository = factory.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository as FileSystemXmlRepository;

            Assert.That(repository?.Directory.FullName, Is.EqualTo(new DirectoryInfo(keyStoreDirectory).FullName),
                "the key ring must persist to the filesystem, in the key store the app was configured with");
        }
    }
}
