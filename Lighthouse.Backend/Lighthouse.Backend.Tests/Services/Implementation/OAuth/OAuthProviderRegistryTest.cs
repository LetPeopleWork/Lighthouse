using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    [TestFixture]
    public class OAuthProviderRegistryTest
    {
        private const string FakeProviderKey = "fake.oauth";
        private const string OtherProviderKey = "other.oauth";

        [Test]
        public void GetByKey_RegisteredProvider_ReturnsThatProvider()
        {
            var provider = CreateProvider(FakeProviderKey);
            var registry = new OAuthProviderRegistry(new[] { provider });

            var resolved = registry.GetByKey(FakeProviderKey);

            Assert.That(resolved, Is.SameAs(provider));
        }

        [Test]
        public void GetByKey_UnknownKey_ThrowsAndNamesTheKey()
        {
            var registry = new OAuthProviderRegistry(new[] { CreateProvider(FakeProviderKey) });

            var ex = Assert.Throws<OAuthProviderNotFoundException>(
                () => registry.GetByKey("nonexistent.oauth"));

            Assert.That(ex!.Message, Does.Contain("nonexistent.oauth"));
        }

        [Test]
        public void Constructor_DuplicateProviderKey_ThrowsAndNamesTheDuplicate()
        {
            var providerA = CreateProvider(FakeProviderKey);
            var providerB = CreateProvider(FakeProviderKey);

            var ex = Assert.Throws<InvalidOperationException>(
                () => new OAuthProviderRegistry(new[] { providerA, providerB }));

            Assert.That(ex!.Message, Does.Contain(FakeProviderKey));
        }

        [Test]
        public void StartupSelfCheck_SchemaAndProvidersMatch_AppStarts()
        {
            using var factory = new TestWebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IOAuthSchemaExtensions>(
                        new OAuthSchemaExtensions(new[] { OtherProviderKey }));
                    services.AddSingleton(CreateProvider(OtherProviderKey));
                }));

            using var client = factory.CreateClient();

            var registry = factory.Services.GetRequiredService<IOAuthProviderRegistry>();
            Assert.That(registry.GetByKey(OtherProviderKey), Is.Not.Null);
        }

        [Test]
        public void StartupSelfCheck_SchemaHasUnmatchedOAuthKey_AppFailsToStart()
        {
            using var factory = new TestWebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IOAuthSchemaExtensions>(
                        new OAuthSchemaExtensions(new[] { OtherProviderKey }));
                    services.RemoveAll<IOAuthProvider>();
                }));

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                using var _ = factory.CreateClient();
            });

            Assert.That(ex!.Message, Does.Contain(OtherProviderKey));
        }

        private static IOAuthProvider CreateProvider(string providerKey)
        {
            var mock = new Mock<IOAuthProvider>();
            mock.SetupGet(p => p.ProviderKey).Returns(providerKey);
            mock.SetupGet(p => p.DefaultScopes).Returns(Array.Empty<string>());
            return mock.Object;
        }
    }
}
