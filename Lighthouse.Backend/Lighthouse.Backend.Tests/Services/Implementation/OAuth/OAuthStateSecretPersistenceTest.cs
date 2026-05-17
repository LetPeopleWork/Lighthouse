using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    [TestFixture]
    public class OAuthStateSecretPersistenceTest
    {
        private string keyStoreDir = null!;

        [SetUp]
        public void SetUp()
        {
            keyStoreDir = Path.Combine(
                Path.GetTempPath(),
                $"lighthouse-oauth-state-{Guid.NewGuid():N}");
            Directory.CreateDirectory(keyStoreDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(keyStoreDir))
            {
                Directory.Delete(keyStoreDir, recursive: true);
            }
        }

        [Test]
        public void OAuthStateSecret_SurvivesProcessRestart_WhenBothFactoriesPointAtSameKeyStore()
        {
            var firstSecret = ReadOAuthStateSecretFromNewFactory();
            var secondSecret = ReadOAuthStateSecretFromNewFactory();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstSecret, Is.Not.Null.And.Not.Empty,
                    "first factory must produce an OAuthStateSecret");
                Assert.That(secondSecret, Is.EqualTo(firstSecret),
                    "second factory must reuse the persisted OAuthStateSecret");
            }
        }

        private string ReadOAuthStateSecretFromNewFactory()
        {
            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Lighthouse:DataProtection:KeyStorePath"] = keyStoreDir,
                    });
                });
            });

            _ = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var serviceConfig = scope.ServiceProvider.GetRequiredService<IServiceConfig>();
            return serviceConfig.OAuthStateSecret;
        }
    }
}
