using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace Lighthouse.Backend.Tests.Startup
{
    /// <summary>
    /// Guards the ground every other integration test stands on: each test host carries an encryption
    /// key of its own, and a developer running the backend gets a key store of their own.
    ///
    /// If someone removes either arrangement, these named tests fail and say so. Without them the
    /// symptom would be the entire backend integration suite going red at once, pointing at nothing.
    /// </summary>
    public class EncryptionBootstrapOrderTests
    {
        private const string FixtureRemovedExplanation =
            "A booted test host did not see the fixture encryption key. Every backend integration test " +
            "starts the real application, so all of them depend on this one arrangement in " +
            "TestWebApplicationFactory - restore it rather than working around it here.";

        private TestWebApplicationFactory<Backend.Program> rootFactory = null!;
        private WebApplicationFactory<Backend.Program> authenticatedFactory = null!;

        [OneTimeSetUp]
        public void BootHosts()
        {
            rootFactory = new TestWebApplicationFactory<Backend.Program>();
            authenticatedFactory = TestWebApplicationFactory<Backend.Program>.WithTestAuthentication(rootFactory);
        }

        [OneTimeTearDown]
        public void DisposeHosts()
        {
            authenticatedFactory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public void SharedTestHost_Starts()
        {
            Assert.DoesNotThrow(
                () => _ = rootFactory.Services,
                "The shared integration test host failed to start.");
        }

        [Test]
        public void SharedTestHost_SeesTheFixtureEncryptionKey()
        {
            Assert.That(
                EncryptionKeySeenBy(rootFactory),
                Is.EqualTo(IntegrationTestEncryptionKey.Value),
                FixtureRemovedExplanation);
        }

        [Test]
        public void AuthenticatedHostDerivedFromTheSharedOne_SeesTheFixtureEncryptionKey()
        {
            Assert.That(
                EncryptionKeySeenBy(authenticatedFactory),
                Is.EqualTo(IntegrationTestEncryptionKey.Value),
                FixtureRemovedExplanation);
        }

        [Test]
        public void HostBuiltWithoutTheSharedFactory_SeesTheFixtureEncryptionKey()
        {
            using var independentHost = new HostThatBuildsItsOwnConfiguration();

            Assert.That(
                EncryptionKeySeenBy(independentHost),
                Is.EqualTo(IntegrationTestEncryptionKey.Value),
                "A test host that does not derive from TestWebApplicationFactory did not see the fixture " +
                "encryption key. A few tests build their own host, so the key has to reach the whole test " +
                "process rather than only the shared factory.");
        }

        [Test]
        public void FixtureEncryptionKey_IsNotAKeyPublishedWithTheProduct()
        {
            Assert.That(
                KeysPublishedInTheShippedSettings(),
                Does.Not.Contain(IntegrationTestEncryptionKey.Value),
                "The fixture key is the same value the product ships with. A key checked into a public " +
                "repository as test scaffolding must never be one an instance could actually be running.");
        }

        [Test]
        public void DevelopmentProfile_NamesAKeyStorePathInsteadOfFallingBackToTheDefault()
        {
            var backendProjectDirectory = BackendProjectDirectory();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(backendProjectDirectory, "appsettings.json"))
                .AddJsonFile(Path.Combine(backendProjectDirectory, "appsettings.Development.json"))
                .Build();

            var location = KeyStoreResolver.Resolve(
                configuration["Encryption:KeyStorePath"],
                configuration["Lighthouse:DataProtection:KeyStorePath"],
                configuration["Database:Provider"],
                configuration["Database:ConnectionString"],
                backendProjectDirectory);

            Assert.That(
                location.Case,
                Is.EqualTo(KeyStoreCase.ExplicitKeyStorePath),
                "Running the backend from a fresh clone has to land the key store somewhere named, so " +
                "the instance is allowed to create a key and keeps it across restarts.");
        }

        private static string? EncryptionKeySeenBy(WebApplicationFactory<Backend.Program> factory)
        {
            return factory.Services.GetRequiredService<IConfiguration>()[IntegrationTestEncryptionKey.ConfigurationKey];
        }

        private static List<string> KeysPublishedInTheShippedSettings()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(BackendProjectDirectory(), "appsettings.json"))
                .Build();

            return new[]
            {
                configuration[IntegrationTestEncryptionKey.ConfigurationKey],
                configuration["EncryptionSettings:EncryptionKey"],
            }
            .OfType<string>()
            .ToList();
        }

        // The shipped settings files are not copied into the test output, so they are read from the
        // working tree. The solution file is the landmark because it sits one level above both projects.
        private static string BackendProjectDirectory()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not find Lighthouse.sln above the test output directory.");

            return Path.Combine(directory!.FullName, "Lighthouse.Backend");
        }

        private sealed class HostThatBuildsItsOwnConfiguration : WebApplicationFactory<Backend.Program>
        {
            private readonly string databaseFileName = $"EncryptionBootstrap_{Path.GetRandomFileName().Replace(".", "")}.db";

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IHostedService>();
                    RemoveDbContextRegistrations(services);

                    services.AddDbContext<LighthouseAppContext>(options =>
                    {
                        options.UseSqlite(
                            $"DataSource={databaseFileName};Pooling=False",
                            x => x.MigrationsAssembly("Lighthouse.Migrations.Sqlite"));
                    });
                });
            }

            private static void RemoveDbContextRegistrations(IServiceCollection services)
            {
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<LighthouseAppContext>));

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

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (File.Exists(databaseFileName))
                {
                    File.Delete(databaseFileName);
                }
            }
        }
    }
}
