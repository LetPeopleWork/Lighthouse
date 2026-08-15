using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
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

        private const string Credential = "a stored credential";

        // The block the published key used to live in. Nothing shipped may name it any more, and it is spelled
        // out here rather than derived so that deleting it from a settings file cannot also delete the check.
        private const string TheBlockThePublishedKeyLivedIn = "EncryptionSettings";

        private static readonly string[] ShippedSettingsFileNames = ["appsettings.json", "appsettings.Development.json"];

        private static readonly string[] CredentialsStoredBeforeTheUpgrade =
        [
            "a personal access token",
            "another instance's client secret",
        ];

        private static readonly SecretState[] EveryOneAReadableEnvelope =
            [.. Enumerable.Repeat(SecretState.Envelope, CredentialsStoredBeforeTheUpgrade.Length)];

        private const string CommandLineRoute = "the command line";

        private const string EnvironmentRoute = "an environment variable";

        private const string SettingsFileRoute = "the settings file";

        private const string MountedFileRoute = "a file the settings point at";

        // The name the code read before this release, while the documentation named a different one. It is
        // not accepted as an alias, so nothing here should ever reach a key ring.
        private const string SettingTheCodeUsedToRead = "EncryptionSettings:EncryptionKey";

        private const string KeyMeantForTheSettingTheCodeUsedToRead = "T2xkU2V0dGluZ05hbWVLZXlOb3RJblVzZUFueW1vcmU=";

        // Each route names the setting that carries it and the provider that has to be the one parsing that
        // setting. Naming the provider is the point: the environment provider has binding rules of its own,
        // and a key only ever handed over in a dictionary would never have to meet them.
        private static readonly SupplyRoute[] SupplyRoutes =
        [
            new(CommandLineRoute, IntegrationTestEncryptionKey.ConfigurationKey, "CommandLineConfigurationProvider"),
            new(EnvironmentRoute, IntegrationTestEncryptionKey.ConfigurationKey, "EnvironmentVariablesConfigurationProvider"),
            new(SettingsFileRoute, IntegrationTestEncryptionKey.ConfigurationKey, "JsonConfigurationProvider"),
            new(MountedFileRoute, MountedFileKeyRingSource.PathSettingKey, "CommandLineConfigurationProvider"),
        ];

        private static readonly string[] SupplyRouteNames = [.. SupplyRoutes.Select(route => route.Name)];

        private static readonly byte[] TheDocumentedKey = Convert.FromBase64String(IntegrationTestEncryptionKey.Value);

        private readonly List<string> temporaryDirectories = [];

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

            foreach (var directory in temporaryDirectories.Where(Directory.Exists))
            {
                Directory.Delete(directory, recursive: true);
            }
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
                EveryValueInTheShippedSettings(),
                Does.Not.Contain(IntegrationTestEncryptionKey.Value),
                "The fixture key is the same value the product ships with. A key checked into a public " +
                "repository as test scaffolding must never be one an instance could actually be running.");
        }

        // Deliberately asked by shape rather than by looking for the one value that was removed. Whatever the
        // next such value would be called, it would still be a key, and it would still be in every copy of the
        // product and in the public source - which is the whole reason no key can ship in a settings file.
        [Test]
        public void TheShippedSettingsFile_CarriesNoValueThatCouldBeAnEncryptionKey()
        {
            Assert.That(
                SettingsWhoseValueDecodesToAKey(),
                Is.Empty,
                "A value in the shipped settings file decodes to exactly the length of an encryption key. " +
                "A key that ships with the product is one every copy of it shares, so it protects nothing, " +
                "whatever the setting it sits under happens to be called.");
        }

        [Test]
        public void NoShippedSettingsFile_StillNamesTheBlockThePublishedKeyLivedIn()
        {
            var stillNamingIt = ShippedSettingsFileNames
                .Where(name => File.ReadAllText(Path.Combine(BackendProjectDirectory(), name))
                    .Contains(TheBlockThePublishedKeyLivedIn, StringComparison.Ordinal))
                .ToList();

            Assert.That(
                stillNamingIt,
                Is.Empty,
                "A shipped settings file still carries the block the published key used to live in. Nothing " +
                "reads that name any more, so anything left under it is a setting an operator can change " +
                "without changing anything.");
        }

        [Test]
        public void AnInstanceUpgradingFromThePublishedKey_ReadsEveryCredentialItHadAndAsksForNothing()
        {
            var upgraded = RingResolvedWithNothingSupplied(ADurableKeyStore());
            var storedBeforeTheUpgrade = WrittenUnderThePublishedKeyOn(upgraded);

            var read = storedBeforeTheUpgrade.ConvertAll(CryptoServiceHolding(upgraded).Read);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(upgraded.ActiveKey.Id, Is.Not.EqualTo(LegacyDefaultEncryptionKey.Id),
                    "The upgraded instance is still writing under the published key, so the upgrade moved it " +
                    "nowhere.");
                Assert.That(read.ConvertAll(result => result.State), Is.EqualTo(EveryOneAReadableEnvelope),
                    "A credential stored before the upgrade no longer reads as a readable secret, which is " +
                    "the state that puts an operator in front of a field asking them to type it in again.");
                Assert.That(read.ConvertAll(result => result.PlainText), Is.EqualTo(CredentialsStoredBeforeTheUpgrade));
            }
        }

        [Test]
        public void AfterTheUpgrade_ACredentialSavedNowNamesTheInstancesOwnKeyAndTheOlderOnesStillNameThePublishedOne()
        {
            var upgraded = RingResolvedWithNothingSupplied(ADurableKeyStore());
            var storedBeforeTheUpgrade = WrittenUnderThePublishedKeyOn(upgraded);

            var savedAfterTheUpgrade = CryptoServiceHolding(upgraded).Encrypt(Credential);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(savedAfterTheUpgrade, Does.Contain(upgraded.ActiveKey.Id));
                Assert.That(savedAfterTheUpgrade, Does.Not.Contain(LegacyDefaultEncryptionKey.Id));
                Assert.That(
                    storedBeforeTheUpgrade.TrueForAll(
                        stored => stored.Contains(LegacyDefaultEncryptionKey.Id, StringComparison.Ordinal)),
                    Is.True,
                    "A credential stored before the upgrade stopped naming the key it was written under, so " +
                    "something rewrote it.");
            }
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

        [Test]
        public void TheKeySuppliedTheWayTheConfigurationPageDescribes_IsTheKeyNewSecretsAreWrittenUnder()
        {
            var ring = RingHeldBy(rootFactory);

            using var scope = rootFactory.Services.CreateScope();
            var stored = scope.ServiceProvider.GetRequiredService<ICryptoService>().Encrypt(Credential);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(TheDocumentedKey),
                    "The running instance is not using the key it was given, so an operator who set one is " +
                    "protecting their credentials with something else entirely.");
                Assert.That(stored, Does.Contain(ring.ActiveKey.Id),
                    "A newly stored secret does not name the key on the ring, so nothing later can tell which " +
                    "key it would take to read it back.");
            }
        }

        [Test]
        public void AKeyAnOperatorSupplied_IsReportedAsSuppliedRatherThanAsOneLighthouseMadeForItself()
        {
            Assert.That(
                RingHeldBy(rootFactory).Custody,
                Is.EqualTo(KeyCustody.SuppliedByConfiguration),
                "An instance that cannot tell a key it was given from one it made itself will happily mint a " +
                "replacement over the operator's key, and every secret written afterwards becomes unreadable " +
                "the moment the supplied key wins again on the next start.");
        }

        [TestCaseSource(nameof(SupplyRouteNames))]
        public void AKeySupplied_IsReadThroughTheConfigurationProviderThatActuallyServesThatRoute(string routeName)
        {
            var route = Array.Find(SupplyRoutes, candidate => candidate.Name == routeName)!;
            var serving = RingResolvedFrom(route).ProvidersServingTheSetting;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(serving, Has.Count.EqualTo(1),
                    $"Exactly one provider should carry {route.SettingKey} here, so there is no doubt about " +
                    "which one this route actually exercised.");
                Assert.That(serving, Does.Contain(route.ProviderTypeName),
                    "This route was exercised by pre-loading the setting rather than by letting the provider " +
                    "that serves it parse it. A key that only ever arrives through a dictionary never has to " +
                    "meet that provider's own rules for reading one.");
            }
        }

        [Test]
        public void TheSameKeySuppliedByEveryRouteTheDocumentationDescribes_ResolvesToOneKeyWithOneIdentity()
        {
            var rings = SupplyRoutes.Select(route => RingResolvedFrom(route).Ring).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rings.Select(ring => ring.ActiveKey.Id).Distinct(StringComparer.Ordinal).ToList(),
                    Has.Count.EqualTo(1),
                    "One key answered to more than one name depending on how it arrived, so the same secret " +
                    "would be attributed to a different key on a host that was configured differently.");
                Assert.That(rings.Select(ring => Convert.ToBase64String(ring.ActiveKey.Material.Span)).Distinct(StringComparer.Ordinal).ToList(),
                    Has.Count.EqualTo(1),
                    "The routes did not all end up on the same key material.");
                Assert.That(rings[0].ActiveKey.Material.ToArray(), Is.EqualTo(TheDocumentedKey),
                    "The key every route agrees on is not the key that was supplied.");
            }
        }

        [Test]
        public void RestartingAnInstanceResolvesTheSameKeyAndStillReadsWhatWasSavedBeforeIt()
        {
            var keyStore = ADurableKeyStore();

            var beforeRestart = RingResolvedWithNothingSupplied(keyStore);
            var savedBeforeRestart = CryptoServiceHolding(beforeRestart).Encrypt(Credential);

            var afterRestart = RingResolvedWithNothingSupplied(keyStore);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterRestart.ActiveKey.Id, Is.EqualTo(beforeRestart.ActiveKey.Id),
                    "Starting again produced a different key, which means every restart silently rotates and " +
                    "abandons everything stored before it.");
                Assert.That(CryptoServiceHolding(afterRestart).Decrypt(savedBeforeRestart), Is.EqualTo(Credential),
                    "A credential saved before the restart could not be read after it.");
            }
        }

        [Test]
        public void AnInstanceCarryingOnlyTheSettingTheCodeUsedToRead_BehavesAsThoughNoKeyHadBeenSupplied()
        {
            Assert.That(
                RingResolvedFromTheSettingTheCodeUsedToRead().Custody,
                Is.EqualTo(KeyCustody.GeneratedForThisInstance),
                "The old setting name still decides something. It is deliberately not an alias: a second " +
                "accepted spelling would have to be honoured forever, and two names for one key is how an " +
                "operator came to believe they had overridden a key they had not.");
        }

        [Test]
        public void TheSettingTheCodeUsedToRead_NeverBecomesTheKeyTheInstanceWritesUnder()
        {
            var ring = RingResolvedFromTheSettingTheCodeUsedToRead();

            Assert.That(
                EveryKeyOn(ring),
                Does.Not.Contain(KeyMeantForTheSettingTheCodeUsedToRead),
                "A value under the old setting name reached the key ring, so an instance would encrypt under " +
                "a key nothing in the documentation ever told anyone to set.");
        }

        private static EncryptionKeyRing RingHeldBy(WebApplicationFactory<Backend.Program> factory)
        {
            return factory.Services.GetRequiredService<IEncryptionKeyRingHolder>().Current;
        }

        private static CryptoService CryptoServiceHolding(EncryptionKeyRing ring)
        {
            return new CryptoService(new EncryptionKeyRingHolder(ring), NullLogger<CryptoService>.Instance);
        }

        private static List<string> EveryKeyOn(EncryptionKeyRing ring)
        {
            return [.. ring.RetiredKeys
                .Prepend(ring.ActiveKey)
                .Select(key => Convert.ToBase64String(key.Material.Span))];
        }

        private EncryptionKeyRing RingResolvedFromTheSettingTheCodeUsedToRead()
        {
            return RingResolvedWith(
                configuration => configuration.AddCommandLine(
                    [$"--{SettingTheCodeUsedToRead}={KeyMeantForTheSettingTheCodeUsedToRead}"]),
                ADurableKeyStore());
        }

        private EncryptionKeyRing RingResolvedWithNothingSupplied(KeyStoreLocation keyStore)
        {
            return RingResolvedWith(_ => { }, keyStore);
        }

        private ResolvedRing RingResolvedFrom(SupplyRoute route)
        {
            var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

            SupplyThroughTheProviderServing(route.Name, builder.Configuration);

            Backend.Program.EnsureEncryptionKeyRing(builder, ADurableKeyStore());

            return new ResolvedRing(RingOf(builder), ProvidersServing(builder, route.SettingKey));
        }

        private EncryptionKeyRing RingResolvedWith(
            Action<IConfigurationBuilder> supply, KeyStoreLocation keyStore)
        {
            var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

            supply(builder.Configuration);

            Backend.Program.EnsureEncryptionKeyRing(builder, keyStore);

            return RingOf(builder);
        }

        private void SupplyThroughTheProviderServing(string route, IConfigurationBuilder configuration)
        {
            switch (route)
            {
                case CommandLineRoute:
                    configuration.AddCommandLine(
                        [$"--{IntegrationTestEncryptionKey.ConfigurationKey}={IntegrationTestEncryptionKey.Value}"]);
                    break;

                // The variable itself is set for the whole test process, so this reads the real environment.
                case EnvironmentRoute:
                    configuration.AddEnvironmentVariables();
                    break;

                case SettingsFileRoute:
                    configuration.AddJsonFile(ASettingsFileSupplyingTheKey(), optional: false);
                    break;

                default:
                    configuration.AddCommandLine(
                        [$"--{MountedFileKeyRingSource.PathSettingKey}={AFileHoldingTheKey()}"]);
                    break;
            }
        }

        private string ASettingsFileSupplyingTheKey()
        {
            var path = Path.Combine(ATemporaryDirectory(), "appsettings.json");

            File.WriteAllText(
                path, $$"""{ "Encryption": { "Key": "{{IntegrationTestEncryptionKey.Value}}" } }""");

            return path;
        }

        private string AFileHoldingTheKey()
        {
            var path = Path.Combine(ATemporaryDirectory(), "encryption-keys");

            File.WriteAllText(path, IntegrationTestEncryptionKey.Value);

            return path;
        }

        private KeyStoreLocation ADurableKeyStore()
        {
            return new KeyStoreLocation(ATemporaryDirectory(), KeyStoreCase.ExplicitKeyStorePath);
        }

        private string ATemporaryDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), $"EncryptionBootstrap_{Path.GetRandomFileName()}");

            Directory.CreateDirectory(path);
            temporaryDirectories.Add(path);

            return path;
        }

        private static List<string> ProvidersServing(WebApplicationBuilder builder, string settingKey)
        {
            return [.. ((IConfigurationRoot)builder.Configuration).Providers
                .Where(provider => provider.TryGet(settingKey, out _))
                .Select(provider => provider.GetType().Name)];
        }

        private static EncryptionKeyRing RingOf(WebApplicationBuilder builder)
        {
            var holder = (IEncryptionKeyRingHolder)builder.Services
                .Single(descriptor => descriptor.ServiceType == typeof(IEncryptionKeyRingHolder))
                .ImplementationInstance!;

            return holder.Current;
        }

        private sealed record ResolvedRing(EncryptionKeyRing Ring, List<string> ProvidersServingTheSetting);

        private sealed record SupplyRoute(string Name, string SettingKey, string ProviderTypeName);

        private static string? EncryptionKeySeenBy(WebApplicationFactory<Backend.Program> factory)
        {
            return factory.Services.GetRequiredService<IConfiguration>()[IntegrationTestEncryptionKey.ConfigurationKey];
        }

        // The published key is compiled in with no accessor of its own, so the only way to hold it is the way
        // production does: take it off the end of a ring it has been appended to.
        private static List<string> WrittenUnderThePublishedKeyOn(EncryptionKeyRing upgraded)
        {
            var published = new EncryptionKeyRing(KeyCustody.NoDurableStore, upgraded.RetiredKeys[^1]);

            Assert.That(published.ActiveKey.Id, Is.EqualTo(LegacyDefaultEncryptionKey.Id),
                "The last key on the resolved ring is not the published one, so these fixtures were not " +
                "written under the key an upgrading instance would have used.");

            return [.. CredentialsStoredBeforeTheUpgrade.Select(CryptoServiceHolding(published).Encrypt)];
        }

        private static List<string> SettingsWhoseValueDecodesToAKey()
        {
            return [.. EveryShippedSetting()
                .Where(setting => DecodesToTheLengthOfAKey(setting.Value))
                .Select(setting => setting.Key)];
        }

        private static List<string> EveryValueInTheShippedSettings()
        {
            return [.. EveryShippedSetting().Select(setting => setting.Value!)];
        }

        private static List<KeyValuePair<string, string?>> EveryShippedSetting()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(BackendProjectDirectory(), "appsettings.json"))
                .Build();

            return [.. configuration.AsEnumerable().Where(setting => setting.Value is not null)];
        }

        private static bool DecodesToTheLengthOfAKey(string? value)
        {
            return value is not null
                && Convert.TryFromBase64String(value, new byte[value.Length], out var decodedLength)
                && decodedLength == EncryptionKey.MaterialLength;
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
