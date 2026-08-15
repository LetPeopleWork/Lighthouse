using Lighthouse.Backend.Services.Implementation.Encryption;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class KeyStoreResolverTests
    {
        private const string SqliteProvider = "sqlite";

        private const string PostgresProvider = "postgres";

        private const string ContentRoot = "/srv/lighthouse";

        // The connection string appsettings.json actually ships. It names no directory, so it is the input
        // that must land in the case where Lighthouse will not create a key it cannot promise to keep.
        private const string ShippedConnectionString = "Data Source=LighthouseAppContext.db";

        private const string MountedVolumeDatabase = "/app/Data/LighthouseAppContext.db";

        private const string MountedVolumeDirectory = "/app/Data";

        private const string OperatorNamedPath = "/mnt/secrets/lighthouse-keys";

        // What StandaloneInitializer writes into configuration today, beside the database it also places.
        private const string StandaloneKeyStorePath = "/home/someone/.config/Lighthouse/data-protection-keys";

        private const string KeysDirectoryName = "keys";

        private static readonly string DefaultLocation = Path.Combine(ContentRoot, "data-protection-keys");

        [Test]
        public void Resolve_ExplicitKeyStorePathConfigured_ResolvesToThePathTheOperatorNamed()
        {
            var location = Resolve(encryptionKeyStorePath: OperatorNamedPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(OperatorNamedPath));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.ExplicitKeyStorePath));
                Assert.That(location.MintingIsPermitted, Is.True);
            }
        }

        [Test]
        public void Resolve_OnlyADataProtectionKeyStorePathConfigured_ResolvesToThatDirectory()
        {
            var location = Resolve(dataProtectionKeyStorePath: StandaloneKeyStorePath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(StandaloneKeyStorePath));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.ConfiguredDataProtectionPath));
                Assert.That(location.MintingIsPermitted, Is.True);
            }
        }

        [Test]
        public void Resolve_SqliteDatabaseAtAnAbsolutePath_ResolvesBesideThatDatabaseFile()
        {
            var location = Resolve(databaseConnectionString: $"Data Source={MountedVolumeDatabase}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(Path.Combine(MountedVolumeDirectory, KeysDirectoryName)));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.BesideTheDatabaseFile));
                Assert.That(location.MintingIsPermitted, Is.True);
            }
        }

        [Test]
        public void Resolve_SqliteDatabaseNamedByABareFileName_ResolvesToTheDefaultLocationAndRefusesMinting()
        {
            var location = Resolve(databaseConnectionString: ShippedConnectionString);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(DefaultLocation));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.DefaultLocationNoDurableStore));
                Assert.That(location.MintingIsPermitted, Is.False);
            }
        }

        [Test]
        public void Resolve_PostgresDeployment_ResolvesToTheDefaultLocationAndRefusesMinting()
        {
            var location = Resolve(
                databaseProvider: PostgresProvider,
                databaseConnectionString: "Host=db;Database=lighthouse;Username=lighthouse;Password=secret");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(DefaultLocation));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.DefaultLocationNoDurableStore));
                Assert.That(location.MintingIsPermitted, Is.False);
            }
        }

        [Test]
        public void Resolve_SqliteDatabaseHeldInMemory_ResolvesToTheDefaultLocationAndRefusesMinting()
        {
            var location = Resolve(databaseConnectionString: "Data Source=:memory:");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(DefaultLocation));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.DefaultLocationNoDurableStore));
                Assert.That(location.MintingIsPermitted, Is.False);
            }
        }

        [TestCase(MountedVolumeDatabase, MountedVolumeDirectory)]
        [TestCase(@"C:\Lighthouse\Data\LighthouseAppContext.db", @"C:\Lighthouse\Data")]
        [TestCase(@"\\fileserver\lighthouse\LighthouseAppContext.db", @"\\fileserver\lighthouse")]
        public void Resolve_AnAbsoluteDatabasePathInEitherConvention_ResolvesBesideThatDatabaseFile(
            string databasePath, string expectedDatabaseDirectory)
        {
            var location = Resolve(databaseConnectionString: $"Data Source={databasePath}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(Path.Combine(expectedDatabaseDirectory, KeysDirectoryName)));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.BesideTheDatabaseFile));
            }
        }

        [Test]
        public void Resolve_DataSourceKeywordWrittenWithAndWithoutASpace_ParsesIdentically()
        {
            var spaced = Resolve(databaseConnectionString: $"Data Source={MountedVolumeDatabase}");
            var unspaced = Resolve(databaseConnectionString: $"DataSource={MountedVolumeDatabase}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(spaced.Case, Is.EqualTo(KeyStoreCase.BesideTheDatabaseFile));
                Assert.That(unspaced, Is.EqualTo(spaced));
            }
        }

        [Test]
        public void Resolve_BothKeyStorePathsConfigured_TheExplicitEncryptionOneWins()
        {
            var location = Resolve(
                encryptionKeyStorePath: OperatorNamedPath,
                dataProtectionKeyStorePath: StandaloneKeyStorePath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(OperatorNamedPath));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.ExplicitKeyStorePath));
            }
        }

        [Test]
        public void Resolve_ADataProtectionPathBesideADatabaseFile_TheConfiguredPathWins()
        {
            var location = Resolve(
                dataProtectionKeyStorePath: StandaloneKeyStorePath,
                databaseConnectionString: $"Data Source={MountedVolumeDatabase}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(StandaloneKeyStorePath));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.ConfiguredDataProtectionPath));
            }
        }

        // The same directory means two different things depending on how it was arrived at: named by an
        // operator it is somewhere they promised is durable, fallen back to it is somewhere nobody vouched
        // for. Only the case tells them apart, which is why the answer carries it.
        [Test]
        public void Resolve_AnOperatorNamingTheDefaultLocationItself_IsStillPermittedToMint()
        {
            var location = Resolve(encryptionKeyStorePath: DefaultLocation);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location.Directory, Is.EqualTo(DefaultLocation));
                Assert.That(location.Case, Is.EqualTo(KeyStoreCase.ExplicitKeyStorePath));
                Assert.That(location.MintingIsPermitted, Is.True);
            }
        }

        private static KeyStoreLocation Resolve(
            string? encryptionKeyStorePath = null,
            string? dataProtectionKeyStorePath = null,
            string databaseProvider = SqliteProvider,
            string databaseConnectionString = ShippedConnectionString,
            string contentRootPath = ContentRoot)
        {
            return KeyStoreResolver.Resolve(
                encryptionKeyStorePath,
                dataProtectionKeyStorePath,
                databaseProvider,
                databaseConnectionString,
                contentRootPath);
        }
    }
}
