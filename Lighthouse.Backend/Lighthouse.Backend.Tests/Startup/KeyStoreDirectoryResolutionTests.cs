using Lighthouse.Backend.Services.Implementation.Encryption;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Startup
{
    public class KeyStoreDirectoryResolutionTests
    {
        private const string DatabaseProviderConfigKey = "Database:Provider";

        private const string DatabaseConnectionStringConfigKey = "Database:ConnectionString";

        private const string DataProtectionKeyStorePathConfigKey = "Lighthouse:DataProtection:KeyStorePath";

        private const string EncryptionKeyStorePathConfigKey = "Encryption:KeyStorePath";

        private const string LegacyDirectoryName = "data-protection-keys";

        private const string OAuthStateSecretBlobFileName = "oauth-state-secret.protected";

        private const string RingFileName = GeneratedKeyRingStore.RingFileName;

        // What appsettings.json ships: a bare file name, resolved against the content root. It is the
        // input that tells a resolution that ran too early apart from one that ran after the standalone
        // profile wrote its own paths.
        private const string ShippedConnectionString = "Data Source=LighthouseAppContext.db";

        private string contentRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            contentRoot = Path.Combine(Path.GetTempPath(), $"lighthouse-keystore-{Guid.NewGuid():N}");
            Directory.CreateDirectory(contentRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(contentRoot))
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }

        [Test]
        public void ResolveKeyStoreDirectory_SqliteFileAtAnAbsolutePath_PutsTheKeyStoreBesideThatDatabase()
        {
            var databaseDirectory = Path.Combine(contentRoot, "Data");
            Directory.CreateDirectory(databaseDirectory);

            var builder = BuilderWith(new Dictionary<string, string?>
            {
                [DatabaseProviderConfigKey] = "sqlite",
                [DatabaseConnectionStringConfigKey] = $"Data Source={Path.Combine(databaseDirectory, "LighthouseAppContext.db")}",
            });

            var (location, _) = Backend.Program.ResolveKeyStoreDirectory(builder);

            var expected = Path.Combine(databaseDirectory, "keys");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(expected));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.BesideTheDatabaseFile));
                Assert.That(Directory.Exists(expected), Is.True);
            }
        }

        // Resolution reads two values the standalone profile writes. Run it first and it sees neither, so it
        // lands in the default location instead - which is what this test fails on if the two steps are ever
        // swapped.
        [Test]
        public void InitializeKeyStore_StandaloneProfile_ResolvesTheKeyStoreTheStandaloneStepJustWrote()
        {
            var appDataDirectory = Path.Combine(contentRoot, "AppData", "Lighthouse");
            var builder = BuilderWith(ShippedConfiguration());

            var (location, _) = Backend.Program.InitializeKeyStore(
                builder,
                isStandalone: true,
                StandalonePathInitializationWriting(appDataDirectory));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(Path.Combine(appDataDirectory, LegacyDirectoryName)));
                Assert.That(location.Directory, Is.Not.EqualTo(Path.Combine(contentRoot, LegacyDirectoryName)));
            }
        }

        [Test]
        public void InitializeKeyStore_StandaloneProfile_ResolvesToTheSameDirectoryItResolvedToBefore()
        {
            var appDataDirectory = Path.Combine(contentRoot, "AppData", "Lighthouse");
            var builder = BuilderWith(ShippedConfiguration());

            var (location, migration) = Backend.Program.InitializeKeyStore(
                builder,
                isStandalone: true,
                StandalonePathInitializationWriting(appDataDirectory));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(Path.Combine(appDataDirectory, LegacyDirectoryName)));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.ConfiguredDataProtectionPath));
                Assert.That(migration.ContentsWereCarriedOver, Is.False);
            }
        }

        [Test]
        public void ResolveKeyStoreDirectory_TheOAuthSecretAndTheDataProtectionRing_LandInTheOneResolvedDirectory()
        {
            var keyStoreDirectory = Path.Combine(contentRoot, "Data", "keys");
            var builder = BuilderWith(new Dictionary<string, string?>
            {
                [EncryptionKeyStorePathConfigKey] = keyStoreDirectory,
            });

            var (location, _) = Backend.Program.ResolveKeyStoreDirectory(builder);

            Backend.Program.EnsureOAuthStateSecret(builder, location);
            Backend.Program.ConfigureDataProtection(builder, location);

            using var services = builder.Services.BuildServiceProvider();
            services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("Lighthouse.Tests.KeyStoreDirectory")
                .Protect([1, 2, 3]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(keyStoreDirectory));
                Assert.That(File.Exists(Path.Combine(keyStoreDirectory, OAuthStateSecretBlobFileName)), Is.True);
                Assert.That(Directory.GetFiles(keyStoreDirectory, "key-*.xml"), Is.Not.Empty);
            }
        }

        [Test]
        public void CarryOverLegacyKeyStore_LegacyPopulatedAndResolvedEmpty_CopiesTheContentsAndNamesBothLocations()
        {
            var legacy = PopulatedDirectory("legacy", ("key-a.xml", "<key a>"), (OAuthStateSecretBlobFileName, "secret"));
            var resolved = EmptyDirectory("resolved");

            var outcome = KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.ContentsWereCarriedOver, Is.True);
                Assert.That(outcome.LegacyDirectory, Is.EqualTo(legacy));
                Assert.That(outcome.ResolvedDirectory, Is.EqualTo(resolved));
                Assert.That(ContentsOf(resolved), Is.EqualTo(ContentsOf(legacy)));
            }
        }

        [Test]
        public void CarryOverLegacyKeyStore_RunAgainWithBothHoldingTheSameContents_CopiesNothingASecondTime()
        {
            var legacy = PopulatedDirectory("legacy", ("key-a.xml", "<key a>"));
            var resolved = EmptyDirectory("resolved");

            KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);
            var writtenAt = File.GetLastWriteTimeUtc(Path.Combine(resolved, "key-a.xml"));

            var outcome = KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.ContentsWereCarriedOver, Is.False);
                Assert.That(File.GetLastWriteTimeUtc(Path.Combine(resolved, "key-a.xml")), Is.EqualTo(writtenAt));
                Assert.That(ContentsOf(resolved), Is.EqualTo(ContentsOf(legacy)));
            }
        }

        [Test]
        public void CarryOverLegacyKeyStore_RunAgainAfterTheInstanceMintedItsOwnKey_CarriesNothingRatherThanRefusing()
        {
            var legacy = PopulatedDirectory("legacy", ("key-a.xml", "<key a>"));
            var resolved = EmptyDirectory("resolved");
            KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);
            File.WriteAllText(Path.Combine(resolved, RingFileName), "<the key this instance minted>");

            var outcome = KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);

            Assert.That(outcome.ContentsWereCarriedOver, Is.False);
        }

        [Test]
        public void CarryOverLegacyKeyStore_TheLegacyDirectoryIsTheResolvedOne_CarriesNothingAndLeavesItAsItWas()
        {
            var directory = PopulatedDirectory("resolved", ("key-a.xml", "<the only key there is>"));
            var before = ContentsOf(directory);

            var outcome = KeyStoreMigration.CarryOverLegacyKeyStore(directory, directory);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.ContentsWereCarriedOver, Is.False);
                Assert.That(ContentsOf(directory), Is.EqualTo(before));
            }
        }

        [Test]
        public void CarryOverLegacyKeyStore_TheResolvedDirectoryDoesNotExistYet_IsCreatedAndFilled()
        {
            var legacy = PopulatedDirectory("legacy", ("key-a.xml", "<key a>"));
            var resolved = Path.Combine(contentRoot, "resolved-that-was-never-created");

            var outcome = KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.ContentsWereCarriedOver, Is.True);
                Assert.That(ContentsOf(resolved), Is.EqualTo(ContentsOf(legacy)));
            }
        }

        [Test]
        public void CarryOverLegacyKeyStore_ALegacyStoreHoldingASubdirectory_CarriesThatTreeAcrossToo()
        {
            var legacy = PopulatedDirectory(
                "legacy", ("key-a.xml", "<key a>"), (Path.Combine("nested", "key-b.xml"), "<key b>"));
            var resolved = EmptyDirectory("resolved");

            var outcome = KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.ContentsWereCarriedOver, Is.True);
                Assert.That(ContentsOf(resolved), Is.EqualTo(ContentsOf(legacy)));
                Assert.That(File.Exists(Path.Combine(resolved, "nested", "key-b.xml")), Is.True);
            }
        }

        [Test]
        public void CarryOverLegacyKeyStore_EachDirectoryHoldsADifferentKeyRing_StopsStartupAndModifiesNeither()
        {
            var legacy = PopulatedDirectory("legacy", (RingFileName, "<the ring one instance minted>"));
            var resolved = PopulatedDirectory("resolved", (RingFileName, "<the ring another instance minted>"));

            var legacyBefore = ContentsOf(legacy);
            var resolvedBefore = ContentsOf(resolved);

            var refusal = Assert.Throws<InvalidOperationException>(
                () => KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Contain(legacy));
                Assert.That(refusal.Message, Does.Contain(resolved));
                Assert.That(refusal.Message, Does.Contain("will not choose between them"));
                Assert.That(refusal.Message, Does.Contain("move the other one elsewhere"));
                Assert.That(ContentsOf(legacy), Is.EqualTo(legacyBefore));
                Assert.That(ContentsOf(resolved), Is.EqualTo(resolvedBefore));
            }
        }

        [Test]
        public void CarryOverLegacyKeyStore_BothDirectoriesHoldTheSameKeyRing_CarriesNothingAndDoesNotRefuse()
        {
            var legacy = PopulatedDirectory("legacy", (RingFileName, "<the one ring this instance has>"));
            var resolved = PopulatedDirectory("resolved", (RingFileName, "<the one ring this instance has>"));

            var outcome = KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);

            Assert.That(outcome.ContentsWereCarriedOver, Is.False);
        }

        // The old location is still where an instance with no database file to sit beside keeps its keys,
        // so a deployment that has run on both providers has a live store in each directory. Neither is a
        // rival: only a key ring names a store, and neither of these holds one.
        [Test]
        public void CarryOverLegacyKeyStore_TwoLiveStoresNeitherHoldingAKeyRing_CarriesTheMissingKeysAcross()
        {
            var legacy = PopulatedDirectory(
                "legacy",
                ("key-from-the-default-location.xml", "<a data protection key>"),
                ("oauth-state-secret.protected", "<the secret written there>"));
            var resolved = PopulatedDirectory(
                "resolved",
                ("key-from-beside-the-database.xml", "<another data protection key>"),
                ("oauth-state-secret.protected", "<the secret written here>"));

            var outcome = KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.ContentsWereCarriedOver, Is.True);
                Assert.That(
                    File.ReadAllText(Path.Combine(resolved, "key-from-the-default-location.xml")),
                    Is.EqualTo("<a data protection key>"));
                Assert.That(
                    File.ReadAllText(Path.Combine(resolved, "oauth-state-secret.protected")),
                    Is.EqualTo("<the secret written here>"));
            }
        }

        [Test]
        public void CarryOverLegacyKeyStore_ResolvedAlreadyHoldsAKeyStore_LeavesItExactlyAsItWas()
        {
            var resolved = PopulatedDirectory("resolved", ("key-a.xml", "<the only key that can read this instance>"));
            var legacy = Path.Combine(contentRoot, "legacy-that-was-never-created");

            var resolvedBefore = ContentsOf(resolved);

            var outcome = KeyStoreMigration.CarryOverLegacyKeyStore(resolved, legacy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.ContentsWereCarriedOver, Is.False);
                Assert.That(ContentsOf(resolved), Is.EqualTo(resolvedBefore));
                Assert.That(Directory.Exists(legacy), Is.False);
            }
        }

        private static Dictionary<string, string?> ShippedConfiguration()
        {
            return new Dictionary<string, string?>
            {
                [DatabaseProviderConfigKey] = "sqlite",
                [DatabaseConnectionStringConfigKey] = ShippedConnectionString,
            };
        }

        // The three values StandaloneInitializer.InitializePaths writes into configuration, without its
        // machine-wide side effects: it changes the process working directory and writes under the real
        // user profile, neither of which a test in a parallel suite may do.
        private static Action<WebApplicationBuilder> StandalonePathInitializationWriting(string appDataDirectory)
        {
            return builder => builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:WriteTo:0:Args:path"] = Path.Combine(appDataDirectory, "logs", "log-.txt"),
                [DatabaseConnectionStringConfigKey] = $"Data Source={Path.Combine(appDataDirectory, "LighthouseAppContext.db")}",
                [DataProtectionKeyStorePathConfigKey] = Path.Combine(appDataDirectory, LegacyDirectoryName),
            });
        }

        private WebApplicationBuilder BuilderWith(Dictionary<string, string?> settings)
        {
            var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentRoot,
            });

            builder.Configuration.AddInMemoryCollection(settings);

            return builder;
        }

        private string EmptyDirectory(string name)
        {
            var directory = Path.Combine(contentRoot, name);
            Directory.CreateDirectory(directory);

            return directory;
        }

        private string PopulatedDirectory(string name, params (string FileName, string Contents)[] files)
        {
            var directory = EmptyDirectory(name);

            foreach (var (fileName, contents) in files)
            {
                var file = Path.Combine(directory, fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, contents);
            }

            return directory;
        }

        private static Dictionary<string, string> ContentsOf(string directory)
        {
            return Directory
                .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .ToDictionary(file => Path.GetRelativePath(directory, file), File.ReadAllText);
        }
    }
}
