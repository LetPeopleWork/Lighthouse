using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Startup;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using System.Reflection;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class EncryptionKeyRingBootstrapperTests
    {
        private static readonly Type[] ResolutionTypes =
        [
            typeof(EncryptionKeyRingBootstrapper),
            typeof(ConfiguredKeyRingSource),
            typeof(MountedFileKeyRingSource),
            typeof(GeneratedKeyRingStore),
            typeof(DatabaseSecretPresenceProbe),
        ];

        private const string MountedSecretPath = "/etc/lighthouse/encryption/keyring";

        private static readonly string[] CredentialsStoredBeforeTheUpgrade =
        [
            "a personal access token",
            "another instance's client secret",
            "one more, long enough that padding is not what makes this work: " + new string('x', 400),
        ];

        private const string ConnectionOptionTable =
            """CREATE TABLE "WorkTrackingSystemConnectionOption" ("Id" INTEGER PRIMARY KEY, "Key" TEXT, "Value" TEXT, "IsSecret" INTEGER, "IsOptional" INTEGER, "WorkTrackingSystemConnectionId" INTEGER)""";

        private const string OAuthCredentialTable =
            """CREATE TABLE "OAuthCredentials" ("Id" INTEGER PRIMARY KEY, "AccessToken" TEXT, "RefreshToken" TEXT, "WorkTrackingSystemConnectionId" INTEGER)""";

        private readonly List<ServiceProvider> dataProtectionHosts = [];

        private string keyStoreDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            keyStoreDirectory = Path.Combine(
                Path.GetTempPath(), "lighthouse-key-ring-tests", Guid.NewGuid().ToString("n"));

            Directory.CreateDirectory(keyStoreDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            dataProtectionHosts.ForEach(host => host.Dispose());
            dataProtectionHosts.Clear();

            SqliteConnection.ClearAllPools();

            if (Directory.Exists(keyStoreDirectory))
            {
                Directory.Delete(keyStoreDirectory, recursive: true);
            }
        }

        [Test]
        public void Resolve_NothingSuppliedAndADurableStore_MintsAKeyOfItsOwnAndSaysSo()
        {
            var ring = BootstrapperFor(new PhysicalKeyStoreFileSystem()).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.GeneratedForThisInstance));
                Assert.That(ring.CanMint, Is.True);
                Assert.That(ring.ActiveKey.Material.Length, Is.EqualTo(EncryptionKey.MaterialLength));
                Assert.That(ring.RetiredKeys, Is.EqualTo(OnlyThePublishedKey()));
                Assert.That(File.Exists(Path.Combine(keyStoreDirectory, GeneratedKeyRingStore.RingFileName)), Is.True);
            }
        }

        [Test]
        public void Resolve_TwoInstancesEachMintingTheirOwnKey_DoNotEndUpOnTheSameMaterial()
        {
            var firstInstance = BootstrapperFor(new PhysicalKeyStoreFileSystem()).Resolve();

            SetUpASecondKeyStoreDirectory();

            var secondInstance = BootstrapperFor(new PhysicalKeyStoreFileSystem()).Resolve();

            Assert.That(
                secondInstance.ActiveKey.Material.ToArray(),
                Is.Not.EqualTo(firstInstance.ActiveKey.Material.ToArray()));
        }

        [Test]
        public void Resolve_RunAgainAgainstTheSameDirectory_ResolvesTheSameKeyRatherThanASecondOne()
        {
            var firstStart = BootstrapperFor(new PhysicalKeyStoreFileSystem()).Resolve();

            var restart = BootstrapperFor(new PhysicalKeyStoreFileSystem()).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(restart.ActiveKey.Id, Is.EqualTo(firstStart.ActiveKey.Id));
                Assert.That(restart.ActiveKey.Material.ToArray(), Is.EqualTo(firstStart.ActiveKey.Material.ToArray()));
                Assert.That(restart.Custody, Is.EqualTo(KeyCustody.GeneratedForThisInstance));
            }
        }

        [Test]
        public void Resolve_AKeySuppliedByConfiguration_WinsAndNothingIsWritten()
        {
            var material = MaterialOf(7);
            var staged = new StagedKeyStoreFileSystem();

            var ring = BootstrapperFor(staged, suppliedKey: Convert.ToBase64String(material)).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.SuppliedByConfiguration));
                Assert.That(ring.CanMint, Is.False);
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(material));
                Assert.That(staged.Operations, Is.Empty);
                Assert.That(staged.FileCount, Is.Zero);
            }
        }

        [Test]
        public void Resolve_AKeySuppliedByAMountedFile_BeatsGeneratingOne()
        {
            var material = MaterialOf(11);
            var staged = new StagedKeyStoreFileSystem();
            staged.Place(MountedSecretPath, RingTextFor("k-mounted-01", material));

            var ring = BootstrapperFor(staged, keysFilePath: MountedSecretPath).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.SuppliedByExternalSecret));
                Assert.That(ring.ActiveKey.Id, Is.EqualTo("k-mounted-01"));
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(material));
                Assert.That(staged.FileCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Resolve_KeysFileNamedAndNothingMountedThere_RefusesRatherThanFallingBackToAKeyOfItsOwn()
        {
            var staged = new StagedKeyStoreFileSystem();
            var bootstrapper = BootstrapperFor(staged, keysFilePath: MountedSecretPath);

            var refusal = Assert.Throws<InvalidOperationException>(() => bootstrapper.Resolve());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Contain(MountedSecretPath));
                Assert.That(refusal.Message, Does.Contain("Encryption__KeysFile"));
                Assert.That(refusal.Message, Does.Contain("will not fall back to a key of its own"));
                Assert.That(refusal.Message, Does.Contain("everything written in the meantime would be unreadable"));
                Assert.That(refusal.Message, Does.Contain("Mount the file, or remove the setting"));
                Assert.That(staged.FileCount, Is.Zero);
            }
        }

        [Test]
        public void Resolve_AMountedFileThatCannotBeParsed_NamesTheFileItCameFrom()
        {
            var staged = new StagedKeyStoreFileSystem();
            staged.Place(MountedSecretPath, Encoding.UTF8.GetBytes($"K-SHOUTING:{Convert.ToBase64String(MaterialOf(29))}"));

            var refusal = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(staged, keysFilePath: MountedSecretPath).Resolve());

            Assert.That(refusal.Message, Does.Contain($"the file '{MountedSecretPath}'"));
        }

        [Test]
        public void Resolve_OnlyTheRetiredSettingNamesAKey_UsesItRatherThanMintingOneAndLeavingSecretsUnreadable()
        {
            var material = MaterialOf(41);
            var staged = new StagedKeyStoreFileSystem();

            var ring = BootstrapperFor(
                staged, suppliedUnderTheRetiredName: Convert.ToBase64String(material)).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.SuppliedByConfiguration));
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(material));
                Assert.That(ring.CanMint, Is.False);
                Assert.That(staged.FileCount, Is.Zero);
            }
        }

        [Test]
        public void Resolve_TheCurrentAndTheRetiredSettingBothNameAKey_TheCurrentOneWins()
        {
            var current = MaterialOf(43);
            var staged = new StagedKeyStoreFileSystem();

            var ring = BootstrapperFor(
                staged,
                suppliedKey: Convert.ToBase64String(current),
                suppliedUnderTheRetiredName: Convert.ToBase64String(MaterialOf(47))).Resolve();

            Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(current));
        }

        [Test]
        public void Resolve_TheRingSettingAndTheRetiredSettingBothNameAKey_TheRingWins()
        {
            var fromTheRing = MaterialOf(53);
            var staged = new StagedKeyStoreFileSystem();

            var ring = BootstrapperFor(
                staged,
                suppliedRing: RingStringFor("k-configured-01", fromTheRing),
                suppliedUnderTheRetiredName: Convert.ToBase64String(MaterialOf(59))).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.ActiveKey.Id, Is.EqualTo("k-configured-01"));
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(fromTheRing));
            }
        }

        [Test]
        public void Resolve_StoredSecretsAndNotOneOfThemCanBeRead_RefusesAndNamesBothWaysBack()
        {
            var staged = new StagedKeyStoreFileSystem();
            var bootstrapper = BootstrapperFor(
                staged, readability: new StagedReadabilityProbe(StoredSecretReadability.NothingReadable));

            var refusal = Assert.Throws<InvalidOperationException>(() => bootstrapper.Resolve());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Contain("not one of them can be read"));
                Assert.That(refusal.Message, Does.Contain("Encryption__Key"));
                Assert.That(refusal.Message, Does.Contain("Encryption__KeyStorePath"));
            }
        }

        [TestCase(StoredSecretReadability.SomethingReadable)]
        [TestCase(StoredSecretReadability.NothingStored)]
        [TestCase(StoredSecretReadability.CannotTell)]
        public void Resolve_AnythingOtherThanNotOneSecretReadable_Starts(StoredSecretReadability answer)
        {
            var staged = new StagedKeyStoreFileSystem();
            var bootstrapper = BootstrapperFor(staged, readability: new StagedReadabilityProbe(answer));

            Assert.That(() => bootstrapper.Resolve(), Throws.Nothing);
        }

        // The question costs a read of every stored secret, and the answer cannot change what key was
        // resolved - so it is asked once, about the ring that was actually resolved, and never before.
        [Test]
        public void Resolve_TheKeyThatWasResolved_IsTheOneTheSecretsAreTriedAgainst()
        {
            var staged = new StagedKeyStoreFileSystem();
            var readability = new StagedReadabilityProbe(StoredSecretReadability.SomethingReadable);
            var material = MaterialOf(61);

            var ring = BootstrapperFor(
                staged, suppliedKey: Convert.ToBase64String(material), readability: readability).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readability.TimesAsked, Is.EqualTo(1));
                Assert.That(readability.WasAskedAbout, Is.EqualTo(ring));
            }
        }

        [Test]
        public void Resolve_ConfigurationAndAMountedFileBothSupplyAKey_ConfigurationWins()
        {
            var configuredMaterial = MaterialOf(13);
            var staged = new StagedKeyStoreFileSystem();
            staged.Place(MountedSecretPath, RingTextFor("k-mounted-01", MaterialOf(17)));

            var ring = BootstrapperFor(
                staged,
                suppliedRing: RingStringFor("k-configured-01", configuredMaterial),
                keysFilePath: MountedSecretPath).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.SuppliedByConfiguration));
                Assert.That(ring.ActiveKey.Id, Is.EqualTo("k-configured-01"));
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(configuredMaterial));
            }
        }

        [Test]
        public void Resolve_MintingAKey_WritesItAsideThenMovesItIntoPlaceThenReadsItBack()
        {
            var staged = new StagedKeyStoreFileSystem();
            var store = StoreFor(staged);

            _ = store.Mint();

            // The staging file is named apart per write, so the order and the shape are what is asserted
            // rather than the name: written somewhere else first, moved into place, then read straight back.
            var stagedAt = staged.Operations[0]["write ".Length..];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stagedAt, Does.StartWith(store.RingFilePath).And.EndsWith(GeneratedKeyRingStore.TemporaryFileSuffix));
                Assert.That(stagedAt, Is.Not.EqualTo(store.RingFilePath), "a ring written straight over the old one is lost if the process dies mid-write");
                Assert.That(staged.Operations, Is.EqualTo(new List<string>
                {
                    $"write {stagedAt}",
                    $"move {stagedAt} -> {store.RingFilePath}",
                    $"read {store.RingFilePath}",
                }));
            }
        }

        [Test]
        public void Mint_InterruptedBeforeTheKeyIsMovedIntoPlace_LeavesThePreviousRingIntact()
        {
            var staged = new StagedKeyStoreFileSystem();
            var store = StoreFor(staged);
            var previousRing = Encoding.UTF8.GetBytes("the ring that was already there");
            staged.Place(store.RingFilePath, previousRing);
            staged.WhatHappensOnMove = (_, _) => throw new IOException("the process went away");

            Assert.Throws<IOException>(() => store.Mint());

            Assert.That(staged.Contents(store.RingFilePath), Is.EqualTo(previousRing));
        }

        [Test]
        public void Resolve_AFilesystemThatAcceptsTheWriteAndHandsBackSomethingElse_StopsStartup()
        {
            var staged = new StagedKeyStoreFileSystem();
            var bootstrapper = BootstrapperFor(staged);
            var ringFilePath = Path.Combine(keyStoreDirectory, GeneratedKeyRingStore.RingFileName);
            staged.WhatTheFilesystemHandsBack = (_, _) => Encoding.UTF8.GetBytes("not what was written");

            var refusal = Assert.Throws<InvalidOperationException>(() => bootstrapper.Resolve());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Contain(ringFilePath));
                Assert.That(refusal.Message, Does.Contain("did not read back as what was written"));
                Assert.That(refusal.Message, Does.Contain("cannot be trusted to keep it"));
                Assert.That(refusal.Message, Does.Contain("every secret written under it would become unreadable"));
                Assert.That(refusal.Message, Does.Contain("storage that keeps what it is given"));
                Assert.That(refusal.Message, Does.Contain("supply the key through Encryption__Key"));
            }
        }

        [Test]
        public void Resolve_AKeyStoreThatExistsAndCannotBeRead_StopsStartupNamesItAndWritesNoReplacement()
        {
            var staged = new StagedKeyStoreFileSystem();
            var bootstrapper = BootstrapperFor(staged);
            var ringFilePath = Path.Combine(keyStoreDirectory, GeneratedKeyRingStore.RingFileName);
            var unreadable = Encoding.UTF8.GetBytes("this was never wrapped by this instance");
            staged.Place(ringFilePath, unreadable);

            var refusal = Assert.Throws<InvalidOperationException>(() => bootstrapper.Resolve());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Contain(ringFilePath));
                Assert.That(refusal.Message, Does.Contain("is there and could not be read"));
                Assert.That(refusal.Message, Does.Contain("will not write a replacement"));
                Assert.That(refusal.Message, Does.Contain("leave every secret already stored unreadable"));
                Assert.That(refusal.Message, Does.Contain("supply the key through Encryption__Key"));
                Assert.That(staged.Contents(ringFilePath), Is.EqualTo(unreadable));
                Assert.That(staged.FileCount, Is.EqualTo(1));
                Assert.That(staged.Operations, Has.No.Member($"write {ringFilePath}"));
            }
        }

        [Test]
        public void Resolve_AKeyStoreThisInstanceCanReadButCannotParse_NamesTheFileItCameFrom()
        {
            var staged = new StagedKeyStoreFileSystem();
            var ringFilePath = Path.Combine(keyStoreDirectory, GeneratedKeyRingStore.RingFileName);
            staged.Place(
                ringFilePath,
                AsThisInstanceWouldHaveWrittenIt($"K-SHOUTING:{Convert.ToBase64String(MaterialOf(31))}"));

            var refusal = Assert.Throws<InvalidOperationException>(() => BootstrapperFor(staged).Resolve());

            Assert.That(refusal.Message, Does.Contain($"the file '{ringFilePath}'"));
        }

        [Test]
        public void Move_ADestinationThatIsAlreadyThere_IsReplacedRatherThanRefused()
        {
            var fileSystem = new PhysicalKeyStoreFileSystem();
            var writtenAside = Path.Combine(keyStoreDirectory, "ring.writing");
            var inPlace = Path.Combine(keyStoreDirectory, "ring");
            var replacement = new byte[] { 1, 2, 3 };
            fileSystem.WriteAllBytes(writtenAside, replacement);
            fileSystem.WriteAllBytes(inPlace, [9]);

            fileSystem.Move(writtenAside, inPlace);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fileSystem.ReadAllBytes(inPlace), Is.EqualTo(replacement));
                Assert.That(fileSystem.FileExists(writtenAside), Is.False);
            }
        }

        [Test]
        public void Resolve_EveryWayThisCanRefuse_NeverRepeatsACharacterOfTheKeyMaterial()
        {
            var material = MaterialOf(23);
            var encoded = Convert.ToBase64String(material);
            var staged = new StagedKeyStoreFileSystem();
            staged.Place(MountedSecretPath, Encoding.UTF8.GetBytes($"K-SHOUTING:{encoded}"));

            var configuredDefect = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(staged, suppliedRing: $"k-one:{encoded},k-one:{encoded}").Resolve());
            var mountedDefect = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(staged, keysFilePath: MountedSecretPath).Resolve());
            var published = ThePublishedKeyEncoded();
            var publishedRefusal = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(staged, suppliedKey: published).Resolve());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(configuredDefect.Message, Does.Not.Contain(encoded));
                Assert.That(mountedDefect.Message, Does.Not.Contain(encoded));
                Assert.That(configuredDefect.Message, Does.Not.Contain(Convert.ToHexStringLower(material)));
                Assert.That(mountedDefect.Message, Does.Not.Contain(Convert.ToHexStringLower(material)));
                Assert.That(publishedRefusal.Message, Does.Not.Contain(published));
                Assert.That(
                    publishedRefusal.Message,
                    Does.Not.Contain(Convert.ToHexStringLower(ThePublishedKey().Material.Span)),
                    "the key being refused is public, but a refusal that quotes one key teaches the next one to quote another");
            }
        }

        [Test]
        public void Resolution_HasNoWayToPutAnythingItResolvedBackIntoConfiguration()
        {
            var membersReachingConfiguration = ResolutionTypes
                .SelectMany(type => type.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(TouchesConfiguration)
                .Select(member => $"{member.DeclaringType?.Name}.{member.Name}")
                .ToList();

            Assert.That(membersReachingConfiguration, Is.Empty);
        }

        // Two rings holding the same keys in the same order are the same ring, whoever supplied them, so
        // custody is deliberately left out of equality. This is here so that a later reader does not read the
        // omission as an oversight and "fix" it - and so the corollary stays visible: a ring comparison can
        // never be used to detect that custody changed.
        [Test]
        public void Equality_TwoRingsHoldingTheSameKeys_AreEqualEvenWhenTheirCustodyDiffers()
        {
            var key = new EncryptionKey("k-same-01", MaterialOf(29));
            var generated = new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, key);
            var supplied = new EncryptionKeyRing(KeyCustody.SuppliedByConfiguration, key);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(generated, Is.EqualTo(supplied));
                Assert.That(generated.GetHashCode(), Is.EqualTo(supplied.GetHashCode()));
            }
        }

        [Test]
        public void Resolve_NowhereDurableAndTheDatabaseAlreadyHoldsASecret_StartsOnThePublishedKeyRatherThanMinting()
        {
            var staged = new StagedKeyStoreFileSystem();

            var ring = BootstrapperFor(
                staged,
                keyStoreCase: KeyStoreCase.DefaultLocationNoDurableStore,
                storedSecrets: new StagedSecretPresenceProbe(StoredSecretPresence.HoldsAtLeastOne)).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.NoDurableStore));
                Assert.That(ring.CanMint, Is.False);
                Assert.That(ring.ActiveKey.Id, Is.EqualTo(LegacyDefaultEncryptionKey.Id));
                Assert.That(ring.TryGet(LegacyDefaultEncryptionKey.Id, out _), Is.True);
                Assert.That(staged.Operations, Is.Empty);
                Assert.That(staged.FileCount, Is.Zero);
            }
        }

        [Test]
        public void NoDurableKeyStore_WhatAnInstanceInThatPositionIsTold_NamesThePublishedKeyAndBothWaysOut()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NoDurableKeyStore.Warning, Does.Contain("running on the key published with the product"));
                Assert.That(NoDurableKeyStore.Warning, Does.Contain("Set Encryption__Key"));
                Assert.That(NoDurableKeyStore.Warning, Does.Contain("set Encryption__KeyStorePath"));
                Assert.That(NoDurableKeyStore.Refusal, Does.Contain("will not start on the key published with the product"));
                Assert.That(NoDurableKeyStore.Refusal, Does.Contain("Set Encryption__Key"));
                Assert.That(NoDurableKeyStore.Refusal, Does.Contain("set Encryption__KeyStorePath"));
            }
        }

        [Test]
        public void Resolve_NowhereDurableAndNothingStoredYet_RefusesToStartAndBeginsOnNoKeyAtAll()
        {
            var staged = new StagedKeyStoreFileSystem();
            var bootstrapper = BootstrapperFor(
                staged,
                keyStoreCase: KeyStoreCase.DefaultLocationNoDurableStore,
                storedSecrets: new StagedSecretPresenceProbe(StoredSecretPresence.HoldsNone));

            var refusal = Assert.Throws<InvalidOperationException>(() => bootstrapper.Resolve());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Is.EqualTo(NoDurableKeyStore.Refusal));
                Assert.That(staged.Operations, Is.Empty);
                Assert.That(staged.FileCount, Is.Zero);
            }
        }

        [Test]
        public void Resolve_NowhereDurableAndTheDatabaseCannotBeAsked_StartsWithTheWarningRatherThanRefusing()
        {
            var staged = new StagedKeyStoreFileSystem();

            var ring = BootstrapperFor(
                staged,
                keyStoreCase: KeyStoreCase.DefaultLocationNoDurableStore,
                storedSecrets: new StagedSecretPresenceProbe(StoredSecretPresence.CannotTell)).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.NoDurableStore));
                Assert.That(ring.ActiveKey.Id, Is.EqualTo(LegacyDefaultEncryptionKey.Id));
            }
        }

        [TestCase(KeyStoreCase.ExplicitKeyStorePath)]
        [TestCase(KeyStoreCase.ConfiguredDataProtectionPath)]
        [TestCase(KeyStoreCase.BesideTheDatabaseFile)]
        public void Resolve_AKeyStoreSomebodyVouchedFor_MintsWithoutAskingTheDatabaseAnything(KeyStoreCase keyStoreCase)
        {
            var storedSecrets = new StagedSecretPresenceProbe(StoredSecretPresence.HoldsNone);

            var ring = BootstrapperFor(
                new PhysicalKeyStoreFileSystem(),
                keyStoreCase: keyStoreCase,
                storedSecrets: storedSecrets).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.GeneratedForThisInstance));
                Assert.That(storedSecrets.TimesAsked, Is.Zero);
            }
        }

        [Test]
        public void Look_ADatabaseThatCannotBeOpenedAtAll_CannotTellRatherThanClaimingItIsEmpty()
        {
            var unreachable = $"Data Source={Path.Combine(keyStoreDirectory, "no-such-directory", "lighthouse.db")}";

            Assert.That(ProbeAgainst(unreachable).Look(), Is.EqualTo(StoredSecretPresence.CannotTell));
        }

        [Test]
        public void Look_ADatabaseWithNoSchemaAtAll_CannotTellRatherThanClaimingItIsEmpty()
        {
            var beforeAnyMigrationRan = ADatabaseAt("before-migrations.db");

            Assert.That(ProbeAgainst(beforeAnyMigrationRan).Look(), Is.EqualTo(StoredSecretPresence.CannotTell));
        }

        [Test]
        public void Look_ADatabaseWhoseSecretCarryingTableIsAbsent_CannotTellRatherThanClaimingItIsEmpty()
        {
            var halfASchema = ADatabaseAt("half-a-schema.db", OAuthCredentialTable);

            Assert.That(ProbeAgainst(halfASchema).Look(), Is.EqualTo(StoredSecretPresence.CannotTell));
        }

        [Test]
        public void Look_ADatabaseHoldingASecretConnectionOption_SaysItHoldsAtLeastOne()
        {
            var withAStoredCredential = ADatabaseAt(
                "with-a-credential.db",
                ConnectionOptionTable,
                OAuthCredentialTable,
                """INSERT INTO "WorkTrackingSystemConnectionOption" ("Value", "IsSecret") VALUES ('an-envelope', 1)""");

            Assert.That(ProbeAgainst(withAStoredCredential).Look(), Is.EqualTo(StoredSecretPresence.HoldsAtLeastOne));
        }

        [Test]
        public void Look_ADatabaseHoldingAnOAuthToken_SaysItHoldsAtLeastOne()
        {
            var withAStoredToken = ADatabaseAt(
                "with-a-token.db",
                ConnectionOptionTable,
                OAuthCredentialTable,
                """INSERT INTO "OAuthCredentials" ("AccessToken") VALUES ('an-envelope')""");

            Assert.That(ProbeAgainst(withAStoredToken).Look(), Is.EqualTo(StoredSecretPresence.HoldsAtLeastOne));
        }

        [Test]
        public void Look_ADatabaseWhoseOnlyStoredValuesAreNotSecrets_SaysItHoldsNone()
        {
            var freshlyMigrated = ADatabaseAt(
                "freshly-migrated.db",
                ConnectionOptionTable,
                OAuthCredentialTable,
                """INSERT INTO "WorkTrackingSystemConnectionOption" ("Value", "IsSecret") VALUES ('https://example.com', 0)""");

            Assert.That(ProbeAgainst(freshlyMigrated).Look(), Is.EqualTo(StoredSecretPresence.HoldsNone));
        }

        [Test]
        public void Look_WhateverItFinds_OpensOneConnectionOfItsOwnAndLeavesNothingOpen()
        {
            var connectionString = ADatabaseAt("lifecycle.db", ConnectionOptionTable, OAuthCredentialTable);
            var opened = new List<SqliteConnection>();

            var presence = new DatabaseSecretPresenceProbe(() =>
            {
                var connection = new SqliteConnection(connectionString);
                opened.Add(connection);

                return connection;
            }).Look();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(presence, Is.EqualTo(StoredSecretPresence.HoldsNone));
                Assert.That(opened, Has.Count.EqualTo(1));
                Assert.That(opened.TrueForAll(connection => connection.State == ConnectionState.Closed), Is.True);
            }
        }

        [TestCase(KeyCustody.GeneratedForThisInstance)]
        [TestCase(KeyCustody.SuppliedByConfiguration)]
        [TestCase(KeyCustody.SuppliedByExternalSecret)]
        public void Resolve_AnInstanceThatHasAKeyOfItsOwn_KeepsThePublishedKeyLastAndNeverWritesUnderIt(KeyCustody custody)
        {
            var ring = RingResolvedInCustody(custody);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(custody));
                Assert.That(ring.ActiveKey.Id, Is.Not.EqualTo(LegacyDefaultEncryptionKey.Id));
                Assert.That(ring.RetiredKeys[^1], Is.EqualTo(ThePublishedKey()));
                Assert.That(ring.TryGet(LegacyDefaultEncryptionKey.Id, out _), Is.True);
            }
        }

        // The one position in which the published key is the key that writes, because it is the only key the
        // instance has: nothing here can keep one of its own, so this is the state it was already in before
        // the upgrade. It cannot mint a replacement, which is why the warning tells the operator to supply one.
        [Test]
        public void Resolve_NowhereDurableToKeepAKey_HoldsThePublishedKeyAndNothingElse()
        {
            var ring = RingResolvedInCustody(KeyCustody.NoDurableStore);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.ActiveKey, Is.EqualTo(ThePublishedKey()));
                Assert.That(ring.RetiredKeys, Is.Empty);
                Assert.That(ring.CanMint, Is.False);
            }
        }

        [TestCase(KeyCustody.GeneratedForThisInstance)]
        [TestCase(KeyCustody.SuppliedByConfiguration)]
        [TestCase(KeyCustody.SuppliedByExternalSecret)]
        [TestCase(KeyCustody.NoDurableStore)]
        public void ThePublishedKey_AppendedAgainToAnAlreadyResolvedRing_ChangesNothingAboutIt(KeyCustody custody)
        {
            var ring = RingResolvedInCustody(custody);

            Assert.That(ring.WithLegacyDefault(), Is.EqualTo(ring));
        }

        [Test]
        public void Resolve_AnInstanceUpgradingOntoAKeyOfItsOwn_StillReadsEverythingWrittenUnderThePublishedKey()
        {
            var writtenBeforeTheUpgrade = EncryptedUnderThePublishedKey();

            var upgraded = CryptoServiceHolding(BootstrapperFor(new PhysicalKeyStoreFileSystem()).Resolve());

            Assert.That(
                writtenBeforeTheUpgrade.ConvertAll(upgraded.Decrypt),
                Is.EqualTo(CredentialsStoredBeforeTheUpgrade));
        }

        [Test]
        public void Resolve_ACredentialSavedAfterTheUpgrade_NamesThisInstancesOwnKeyRatherThanThePublishedOne()
        {
            var ring = BootstrapperFor(new PhysicalKeyStoreFileSystem()).Resolve();

            var savedAfterTheUpgrade = CryptoServiceHolding(ring).Encrypt(CredentialsStoredBeforeTheUpgrade[0]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(savedAfterTheUpgrade, Does.Contain(ring.ActiveKey.Id));
                Assert.That(savedAfterTheUpgrade, Does.Not.Contain(LegacyDefaultEncryptionKey.Id));
            }
        }

        [Test]
        public void Resolve_ADatabaseOfCredentialsWrittenUnderThePublishedKey_IsLeftExactlyAsItWas()
        {
            var storedBeforeTheUpgrade = EncryptedUnderThePublishedKey();
            var connectionString = ADatabaseHolding("upgraded.db", storedBeforeTheUpgrade);

            var ring = BootstrapperFor(
                new StagedKeyStoreFileSystem(),
                keyStoreCase: KeyStoreCase.DefaultLocationNoDurableStore,
                storedSecrets: null,
                probe: ProbeAgainst(connectionString)).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(SecretValuesIn(connectionString), Is.EqualTo(storedBeforeTheUpgrade));
                Assert.That(
                    storedBeforeTheUpgrade.TrueForAll(
                        stored => stored.Contains(LegacyDefaultEncryptionKey.Id, StringComparison.Ordinal)),
                    Is.True);
                Assert.That(
                    storedBeforeTheUpgrade.ConvertAll(stored => CryptoServiceHolding(ring).Read(stored).KeyId),
                    Is.EqualTo(EveryOneNamingThePublishedKey));
            }
        }

        // What "nothing walked the stored secrets during startup" is checked against rather than assumed: the
        // only statement anything in key resolution can put to a database asks whether a secret is there, and
        // reads back a literal 1 rather than anything a secret column holds.
        [Test]
        public void Resolution_TheOnlyThingItCanAskADatabase_IsWhetherASecretIsThereAndNeverWhatOneSays()
        {
            var everyStatementItCanIssue = ResolutionTypes
                .SelectMany(type => type.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(field => field.FieldType == typeof(string[]))
                .SelectMany(field => (string[])field.GetValue(null)!)
                .Where(value => value.Contains("SELECT", StringComparison.OrdinalIgnoreCase))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(everyStatementItCanIssue, Is.Not.Empty);
                Assert.That(
                    everyStatementItCanIssue.TrueForAll(
                        query => query.StartsWith("SELECT 1 FROM", StringComparison.Ordinal)
                            && query.EndsWith("LIMIT 1", StringComparison.Ordinal)),
                    Is.True);
            }
        }

        [Test]
        public void Resolve_ThePublishedKeySuppliedAsTheKeyToWriteWith_RefusesToStart()
        {
            var refusal = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(new StagedKeyStoreFileSystem(), suppliedKey: ThePublishedKeyEncoded()).Resolve());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    refusal.Message,
                    Does.Contain(ConfiguredKeyRingSource.AsAnOperatorWouldWriteIt(ConfiguredKeyRingSource.SingleKeySettingKey)),
                    "an operator cannot act on a refusal that does not say which setting carried the key");
                Assert.That(
                    refusal.Message,
                    Does.Not.Contain(ConfiguredKeyRingSource.SingleKeySettingKey),
                    "the colon spelling appears in no compose file, manifest or environment an operator can search");
                Assert.That(refusal.Message, Does.Contain("is the key published with Lighthouse"));
                Assert.That(refusal.Message, Does.Contain("ships inside every copy of the product"),
                    "an operator who is not told why the key is no good has been given an order rather than a reason");
                Assert.That(refusal.Message, Does.Contain("would not be protected at all"));
                Assert.That(refusal.Message, Does.Contain("Nothing has been changed and nothing is lost"));
                Assert.That(refusal.Message, Does.Contain("Set Encryption__Key to a key of your own"));
                Assert.That(refusal.Message, Does.Contain("start Lighthouse again"),
                    "every other refusal on this path ends by saying to start again, and an operator reading two of them should not have to wonder why one does not");
            }
        }

        // Every Lighthouse before this release shipped the published key in appsettings.json under this
        // name, and the updater keeps an operator settings file across an upgrade on purpose. An instance
        // arriving here has therefore chosen nothing - it is carrying a value the product itself put there
        // and never took away. Refusing to start strands it on a machine whose only way out is editing
        // JSON by hand, and protects nothing: the key it would refuse over is the one it is already
        // reading every stored credential with.
        [Test]
        public void Resolve_ThePublishedKeyUnderTheNameThisReleaseRetired_MintsItsOwnKeyRatherThanRefusingToStart()
        {
            var ring = BootstrapperFor(
                new PhysicalKeyStoreFileSystem(),
                suppliedUnderTheRetiredName: ThePublishedKeyEncoded()).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.GeneratedForThisInstance));
                Assert.That(
                    LegacyDefaultEncryptionKey.Matches(ring.ActiveKey.Material.Span),
                    Is.False,
                    "anything written after this start would be protected by bytes that ship in every copy of the product");
                Assert.That(
                    ring.RetiredKeys.Any(key => LegacyDefaultEncryptionKey.Matches(key.Material.Span)),
                    Is.True,
                    "the published key has to stay on the ring for reading, or the upgrade takes every stored credential with it");
            }
        }

        // Reading the shipped value as no key sends an instance with nowhere to keep one down to the branch
        // that runs on the published key, where before it was stopped. That is not a step backwards: the
        // refusal told such an instance to remove the setting and let Lighthouse make a key, which is the
        // one thing it cannot do, and removing it landed here anyway. What it needs to hear is that it has
        // nowhere to keep a key - which is what it is told here, and is the only thing it can act on.
        [Test]
        public void Resolve_ThePublishedKeyUnderTheRetiredNameWithNowhereToKeepAKey_StartsAndIsToldWhatIsActuallyWrong()
        {
            var ring = BootstrapperFor(
                new StagedKeyStoreFileSystem(),
                suppliedUnderTheRetiredName: ThePublishedKeyEncoded(),
                keyStoreCase: KeyStoreCase.DefaultLocationNoDurableStore,
                storedSecrets: new StagedSecretPresenceProbe(StoredSecretPresence.HoldsAtLeastOne)).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.NoDurableStore));
                Assert.That(
                    StartupBanner.BuildEncryptionCustodyLines(
                        ring, new KeyStoreLocation("/app/keys", KeyStoreCase.DefaultLocationNoDurableStore),
                        keyCameFromTheRetiredSetting: false,
                        allowsStartWithUnreadableSecrets: false,
                        keySupply: null,
                        thePublishedKeyWasLeftInTheSettingsFile: true),
                    Has.None.Contains("a key of its own"),
                    "An instance running on the published key was told it uses a key of its own, directly " +
                    "under the warning saying it does not - so one of the two adjacent lines is lying.");
            }
        }

        [Test]
        public void Resolve_ThePublishedKeyUnderTheNameThisReleaseRetired_StillReadsWhatWasStoredBeforeTheUpgrade()
        {
            var writtenBeforeTheUpgrade = EncryptedUnderThePublishedKey();

            var upgraded = CryptoServiceHolding(
                BootstrapperFor(
                    new PhysicalKeyStoreFileSystem(),
                    suppliedUnderTheRetiredName: ThePublishedKeyEncoded()).Resolve());

            Assert.That(
                writtenBeforeTheUpgrade.ConvertAll(upgraded.Decrypt),
                Is.EqualTo(CredentialsStoredBeforeTheUpgrade));
        }

        [Test]
        public void Resolve_ARingWhoseFirstEntryIsThePublishedKey_RefusesAndNamesTheRingSetting()
        {
            var supplied = $"{RingStringFor("k-old", ThePublishedKey().Material.ToArray())},{RingStringFor("k-mine", MaterialOf(41))}";

            var refusal = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(new StagedKeyStoreFileSystem(), suppliedRing: supplied).Resolve());

            Assert.That(
                refusal.Message,
                Does.Contain(ConfiguredKeyRingSource.AsAnOperatorWouldWriteIt(ConfiguredKeyRingSource.RingSettingKey)));
        }

        [Test]
        public void Resolve_AMountedKeyFileHoldingThePublishedKey_RefusesAndNamesTheFile()
        {
            var staged = new StagedKeyStoreFileSystem();
            staged.Place(MountedSecretPath, RingTextFor("k-mounted-01", ThePublishedKey().Material.ToArray()));

            var refusal = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(staged, keysFilePath: MountedSecretPath).Resolve());

            Assert.That(refusal.Message, Does.Contain($"the file '{MountedSecretPath}'"));
        }

        [Test]
        public void Resolve_ARingWhoseSecondEntryIsThePublishedKey_StartsOnTheOperatorsOwnKey()
        {
            var supplied = $"{RingStringFor("k-mine", MaterialOf(41))},{RingStringFor("k-old", ThePublishedKey().Material.ToArray())}";

            var ring = BootstrapperFor(new StagedKeyStoreFileSystem(), suppliedRing: supplied).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.ActiveKey.Id, Is.EqualTo("k-mine"));
                Assert.That(
                    CryptoServiceHolding(ring).Decrypt(EncryptedUnderThePublishedKey()[0]),
                    Is.EqualTo(CredentialsStoredBeforeTheUpgrade[0]),
                    "behind an active key the same material has to stay welcome, or every upgrade stops being readable");
            }
        }

        // The refusal an operator reads at the moment everything stopped. Both remedies it used to offer
        // assumed they had lost a key; the reproduced cause is that they added one, and in that state the
        // key store is already correct and the key it asks them for was generated into a file they never
        // read.
        [Test]
        public void Resolve_AKeyThatReadsNothing_LeadsWithUndoingWhatWasJustSet()
        {
            var refusal = RefusalWhenNothingCanBeRead(WrittenUnder);

            var undo = refusal.Message.IndexOf("remove that setting", StringComparison.Ordinal);
            var supplyAnother = refusal.Message.IndexOf("Otherwise set Encryption__Key", StringComparison.Ordinal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(undo, Is.GreaterThan(-1),
                    "the only remedy an operator can carry out with nothing but what they already have");
                Assert.That(undo, Is.LessThan(supplyAnother),
                    "it is both the likeliest cause and the cheapest fix, so it is read first");
            }
        }

        [Test]
        public void Resolve_AKeyThatReadsNothing_NamesBothKeys()
        {
            var refusal = RefusalWhenNothingCanBeRead(WrittenUnder);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Contain(WrittenUnder),
                    "the stored credentials say which key wrote them, and an operator told two keys disagree is left to work out which two");
                Assert.That(refusal.Message, Does.Contain(StartedOn));
            }
        }

        [Test]
        public void Resolve_StoredValuesUnderTwoKeys_NamesThemBoth()
        {
            var refusal = RefusalWhenNothingCanBeRead(WrittenUnder, "k-and-this-one-02");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Contain(WrittenUnder));
                Assert.That(refusal.Message, Does.Contain("k-and-this-one-02"));
            }
        }

        [Test]
        public void Resolve_StoredValuesNamingNoKeyAtAll_SaysThatRatherThanInventingOne()
        {
            var refusal = RefusalWhenNothingCanBeRead();

            Assert.That(refusal.Message, Does.Contain("carry no key name at all"),
                "values written before this release carry none, and naming a key that never existed sends an operator hunting for it");
        }

        [Test]
        public void Resolve_AKeyThatReadsNothing_DoesNotPromiseThatNothingIsLost()
        {
            var refusal = RefusalWhenNothingCanBeRead(WrittenUnder);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Not.Contain("nothing is lost"),
                    "true of an operator who pointed at the wrong key, false of one whose key store was destroyed, and Lighthouse cannot tell them apart");
                Assert.That(refusal.Message, Does.Contain("Nothing has been changed by this start"),
                    "which is the thing that is true either way");
            }
        }

        [Test]
        public void Resolve_AKeyThatReadsNothing_NamesTheWayPastItAndWhatItCosts()
        {
            var refusal = RefusalWhenNothingCanBeRead(WrittenUnder);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Contain("Encryption__StartEvenIfNothingStoredCanBeRead"));
                Assert.That(refusal.Message, Does.Contain("every one of them has to be entered again"),
                    "an operator who reaches for this without reading what it costs has thrown away every credential they hold");
                Assert.That(refusal.Message, Does.Contain("Nothing is deleted"));
            }
        }

        [Test]
        public void Resolve_AKeyThatReadsNothing_StillRepeatsNoKeyMaterial()
        {
            var material = MaterialOf(73);

            var refusal = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(
                    new StagedKeyStoreFileSystem(),
                    suppliedRing: RingStringFor(StartedOn, material),
                    readability: new StagedReadabilityProbe(StoredSecretReadability.NothingReadable, WrittenUnder)).Resolve());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Not.Contain(Convert.ToBase64String(material)));
                Assert.That(refusal.Message, Does.Not.Contain(Convert.ToHexStringLower(material)));
            }
        }

        // The remedy that fits depends on whether there is anything to undo. An instance that minted its
        // own key was not handed one by anybody, so telling its operator to remove a setting sends them
        // looking for something that was never there.
        [Test]
        public void Resolve_AKeyThisInstanceMadeForItselfThatReadsNothing_DoesNotTellAnyoneToRemoveASetting()
        {
            var refusal = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(
                    new PhysicalKeyStoreFileSystem(),
                    readability: new StagedReadabilityProbe(StoredSecretReadability.NothingReadable, WrittenUnder)).Resolve());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal.Message, Does.Contain("using a key it made for itself"));
                Assert.That(refusal.Message, Does.Not.Contain("remove that setting"),
                    "there is no setting to remove, and an operator sent looking for one loses the time they have");
            }
        }

        [Test]
        public void Resolve_AKeyThatReadsNothing_ReadsAsSentencesRatherThanRunTogether()
        {
            var refusal = RefusalWhenNothingCanBeRead(WrittenUnder);

            Assert.That(
                refusal.Message,
                Does.Contain("exactly as it was. If you have just started supplying"),
                "the parts are assembled, and an operator reads the result rather than the parts");
        }

        [Test]
        public void Resolve_StoredValuesUnderTwoKeys_KeepsThemApartFromEachOther()
        {
            var refusal = RefusalWhenNothingCanBeRead(WrittenUnder, "k-and-this-one-02");

            Assert.That(
                refusal.Message,
                Does.Contain($"'{WrittenUnder}', 'k-and-this-one-02'"),
                "two key ids run together read as one name that matches no key at all");
        }

        [Test]
        public void TheRefusal_RefusesToBeBuiltWithoutTheThingsItQuotes()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => KeyThatReadsNothing.RefusalFor(null!, []), Throws.ArgumentNullException);
                Assert.That(
                    () => KeyThatReadsNothing.RefusalFor(new EncryptionKeyRing(new EncryptionKey(StartedOn, MaterialOf(83))), null!),
                    Throws.ArgumentNullException);
            }
        }

        private const string StartedOn = "k-started-on-01";

        private const string WrittenUnder = "k-written-under-01";

        private InvalidOperationException RefusalWhenNothingCanBeRead(params string[] keyIdsStoredValuesName)
        {
            return Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(
                    new StagedKeyStoreFileSystem(),
                    suppliedRing: RingStringFor(StartedOn, MaterialOf(79)),
                    readability: new StagedReadabilityProbe(StoredSecretReadability.NothingReadable, keyIdsStoredValuesName)).Resolve())!;
        }

        // The one refusal with no way out, and the switch that provides one. It is deliberately shaped like
        // the emergency administrator: set on purpose, off by default, and impossible to leave running
        // unnoticed.
        [Test]
        public void Resolve_NothingStoredCanBeReadAndTheSwitchIsSet_StartsAnyway()
        {
            var ring = BootstrapperFor(
                new PhysicalKeyStoreFileSystem(),
                readability: new StagedReadabilityProbe(StoredSecretReadability.NothingReadable),
                startAnyway: true).Resolve();

            Assert.That(ring.ActiveKey, Is.Not.Null,
                "an instance whose key is genuinely gone owns a database nothing can open, and every team, forecast and hour of history in it");
        }

        [Test]
        public void Resolve_NothingStoredCanBeReadAndTheSwitchIsNotSet_StillRefuses()
        {
            var bootstrapper = BootstrapperFor(
                new PhysicalKeyStoreFileSystem(),
                readability: new StagedReadabilityProbe(StoredSecretReadability.NothingReadable));

            Assert.That(
                () => bootstrapper.Resolve(),
                Throws.InvalidOperationException,
                "the refusal is right and stays; what it must not be is a dead end");
        }

        [Test]
        public void Resolve_TheSwitchIsSetOnAnInstanceThatIsFine_ChangesNothingAndAsksNothing()
        {
            var readability = new StagedReadabilityProbe(StoredSecretReadability.SomethingReadable);
            var material = MaterialOf(67);

            var ring = BootstrapperFor(
                new StagedKeyStoreFileSystem(),
                suppliedKey: Convert.ToBase64String(material),
                readability: readability,
                startAnyway: true).Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(material));
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.SuppliedByConfiguration));
                Assert.That(readability.TimesAsked, Is.Zero,
                    "the answer cannot change anything once the switch is set, and asking costs a read of every stored secret");
            }
        }

        [Test]
        public void Resolve_TheSwitchIsSet_DoesNotTouchTheKeyStore()
        {
            var staged = new StagedKeyStoreFileSystem();

            BootstrapperFor(
                staged,
                suppliedKey: Convert.ToBase64String(MaterialOf(71)),
                readability: new StagedReadabilityProbe(StoredSecretReadability.NothingReadable),
                startAnyway: true).Resolve();

            Assert.That(staged.Operations, Is.Empty,
                "the switch is a way in, not a repair - nothing is re-encrypted and nothing is discarded");
        }

        // Every other refusal is a situation the switch cannot help with, so letting it past would only
        // start an instance that is about to lose credentials rather than one that has already lost them.
        [Test]
        public void Resolve_TheSwitchIsSet_LetsPastThatOneRefusalAndNoOther()
        {
            var staged = new StagedKeyStoreFileSystem();
            var readable = new StagedReadabilityProbe(StoredSecretReadability.SomethingReadable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => BootstrapperFor(staged, suppliedKey: Convert.ToBase64String(MaterialOf(31)[..16]), readability: readable, startAnyway: true).Resolve(),
                    Throws.InvalidOperationException,
                    "key material of the wrong length");
                Assert.That(
                    () => BootstrapperFor(staged, keysFilePath: MountedSecretPath, readability: readable, startAnyway: true).Resolve(),
                    Throws.InvalidOperationException,
                    "a key file that is not there");
                Assert.That(
                    () => BootstrapperFor(staged, suppliedKey: ThePublishedKeyEncoded(), readability: readable, startAnyway: true).Resolve(),
                    Throws.InvalidOperationException,
                    "the key published with the product supplied as the key");
                Assert.That(
                    () => BootstrapperFor(
                        staged,
                        keyStoreCase: KeyStoreCase.DefaultLocationNoDurableStore,
                        storedSecrets: new StagedSecretPresenceProbe(StoredSecretPresence.HoldsNone),
                        readability: readable,
                        startAnyway: true).Resolve(),
                    Throws.InvalidOperationException,
                    "nowhere to keep a key, with nothing stored yet");
            }
        }

        [Test]
        public void Resolve_ARefusalTheSwitchCannotHelpWith_DoesNotMentionTheSwitch()
        {
            var refusal = Assert.Throws<InvalidOperationException>(
                () => BootstrapperFor(
                    new StagedKeyStoreFileSystem(),
                    keyStoreCase: KeyStoreCase.DefaultLocationNoDurableStore,
                    storedSecrets: new StagedSecretPresenceProbe(StoredSecretPresence.HoldsNone),
                    startAnyway: true).Resolve());

            Assert.That(
                refusal.Message,
                Does.Not.Contain("StartEvenIfNothingStoredCanBeRead"),
                "offering a setting that would not help is how an operator ends up setting all of them");
        }

        // A bootstrapper missing any one of these resolves a key from somewhere it was not told about, and
        // the first anyone would hear of it is a secret written under the wrong key.
        [Test]
        public void ABootstrapper_RefusesToBeBuiltWithoutAnyOfWhatItResolvesFrom()
        {
            var configuration = new ConfiguredKeyRingSource(null, null, null);
            var mountedFile = new MountedFileKeyRingSource(null, new StagedKeyStoreFileSystem());
            var generated = StoreFor(new StagedKeyStoreFileSystem());
            var keyStore = new KeyStoreLocation(keyStoreDirectory, KeyStoreCase.BesideTheDatabaseFile);
            var storedSecrets = new StagedSecretPresenceProbe(StoredSecretPresence.HoldsNone);
            var readability = new StagedReadabilityProbe(StoredSecretReadability.NothingStored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => new EncryptionKeyRingBootstrapper(null!, mountedFile, generated, keyStore, storedSecrets, readability),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => new EncryptionKeyRingBootstrapper(configuration, null!, generated, keyStore, storedSecrets, readability),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => new EncryptionKeyRingBootstrapper(configuration, mountedFile, null!, keyStore, storedSecrets, readability),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => new EncryptionKeyRingBootstrapper(configuration, mountedFile, generated, null!, storedSecrets, readability),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => new EncryptionKeyRingBootstrapper(configuration, mountedFile, generated, keyStore, null!, readability),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => new EncryptionKeyRingBootstrapper(configuration, mountedFile, generated, keyStore, storedSecrets, null!),
                    Throws.ArgumentNullException);
            }
        }

        private static string ThePublishedKeyEncoded()
        {
            return Convert.ToBase64String(ThePublishedKey().Material.Span);
        }

        private static readonly string[] EveryOneNamingThePublishedKey =
            [.. Enumerable.Repeat(LegacyDefaultEncryptionKey.Id, CredentialsStoredBeforeTheUpgrade.Length)];

        // The published key's material is compiled in with no accessor of its own, so the only way to hold it
        // is the way production does: append it to a ring and take it back off the end.
        private static EncryptionKey ThePublishedKey()
        {
            var scaffold = new EncryptionKeyRing(
                new EncryptionKey("k-not-the-published-one", new byte[EncryptionKey.MaterialLength]));

            return scaffold.WithLegacyDefault().RetiredKeys[0];
        }

        private static EncryptionKey[] OnlyThePublishedKey()
        {
            return [ThePublishedKey()];
        }

        private static CryptoService CryptoServiceHolding(EncryptionKeyRing ring)
        {
            return new CryptoService(new EncryptionKeyRingHolder(ring), NullLogger<CryptoService>.Instance);
        }

        private static List<string> EncryptedUnderThePublishedKey()
        {
            var published = CryptoServiceHolding(
                new EncryptionKeyRing(KeyCustody.NoDurableStore, ThePublishedKey()));

            return [.. CredentialsStoredBeforeTheUpgrade.Select(published.Encrypt)];
        }

        private EncryptionKeyRing RingResolvedInCustody(KeyCustody custody)
        {
            var staged = new StagedKeyStoreFileSystem();

            if (custody == KeyCustody.SuppliedByConfiguration)
            {
                return BootstrapperFor(staged, suppliedKey: Convert.ToBase64String(MaterialOf(31))).Resolve();
            }

            if (custody == KeyCustody.SuppliedByExternalSecret)
            {
                staged.Place(MountedSecretPath, RingTextFor("k-mounted-01", MaterialOf(37)));

                return BootstrapperFor(staged, keysFilePath: MountedSecretPath).Resolve();
            }

            if (custody == KeyCustody.NoDurableStore)
            {
                return BootstrapperFor(
                    staged,
                    keyStoreCase: KeyStoreCase.DefaultLocationNoDurableStore,
                    storedSecrets: new StagedSecretPresenceProbe(StoredSecretPresence.HoldsAtLeastOne)).Resolve();
            }

            return BootstrapperFor(new PhysicalKeyStoreFileSystem()).Resolve();
        }

        private string ADatabaseHolding(string fileName, List<string> secretValues)
        {
            var connectionString = ADatabaseAt(fileName, ConnectionOptionTable, OAuthCredentialTable);

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var secretValue in secretValues)
            {
                using var command = connection.CreateCommand();
                command.CommandText =
                    """INSERT INTO "WorkTrackingSystemConnectionOption" ("Value", "IsSecret") VALUES ($value, 1)""";
                command.Parameters.AddWithValue("$value", secretValue);
                command.ExecuteNonQuery();
            }

            return connectionString;
        }

        private static List<string> SecretValuesIn(string connectionString)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                """SELECT "Value" FROM "WorkTrackingSystemConnectionOption" WHERE "IsSecret" = 1 ORDER BY "Id" """;

            using var reader = command.ExecuteReader();
            var stored = new List<string>();

            while (reader.Read())
            {
                stored.Add(reader.GetString(0));
            }

            return stored;
        }

        private static DatabaseSecretPresenceProbe ProbeAgainst(string connectionString)
        {
            return new DatabaseSecretPresenceProbe(() => new SqliteConnection(connectionString));
        }

        private string ADatabaseAt(string fileName, params string[] statements)
        {
            var connectionString = $"Data Source={Path.Combine(keyStoreDirectory, fileName)}";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var statement in statements)
            {
                using var command = connection.CreateCommand();
                command.CommandText = statement;
                command.ExecuteNonQuery();
            }

            return connectionString;
        }

        private static bool TouchesConfiguration(MemberInfo member)
        {
            var involvedTypes = member switch
            {
                MethodBase callable => callable.GetParameters().Select(parameter => parameter.ParameterType),
                PropertyInfo property => [property.PropertyType],
                FieldInfo field => [field.FieldType],
                _ => Enumerable.Empty<Type>(),
            };

            if (member is MethodInfo method)
            {
                involvedTypes = involvedTypes.Append(method.ReturnType);
            }

            return involvedTypes.Any(type =>
                type.Namespace?.StartsWith("Microsoft.Extensions.Configuration", StringComparison.Ordinal) == true);
        }

        private static byte[] MaterialOf(byte seed)
        {
            return Enumerable.Repeat(seed, EncryptionKey.MaterialLength).ToArray();
        }

        private static string RingStringFor(string keyId, byte[] material)
        {
            return $"{keyId}:{Convert.ToBase64String(material)}";
        }

        private static byte[] RingTextFor(string keyId, byte[] material)
        {
            return Encoding.UTF8.GetBytes(RingStringFor(keyId, material));
        }

        private void SetUpASecondKeyStoreDirectory()
        {
            keyStoreDirectory = Path.Combine(
                Path.GetTempPath(), "lighthouse-key-ring-tests", Guid.NewGuid().ToString("n"));

            Directory.CreateDirectory(keyStoreDirectory);
        }

        private EncryptionKeyRingBootstrapper BootstrapperFor(
            IKeyStoreFileSystem fileSystem,
            string? suppliedRing = null,
            string? suppliedKey = null,
            string? suppliedUnderTheRetiredName = null,
            string? keysFilePath = null,
            KeyStoreCase keyStoreCase = KeyStoreCase.BesideTheDatabaseFile,
            StagedSecretPresenceProbe? storedSecrets = null,
            DatabaseSecretPresenceProbe? probe = null,
            IStoredSecretReadabilityProbe? readability = null,
            bool startAnyway = false)
        {
            return new EncryptionKeyRingBootstrapper(
                new ConfiguredKeyRingSource(suppliedRing, suppliedKey, suppliedUnderTheRetiredName),
                new MountedFileKeyRingSource(keysFilePath, fileSystem),
                StoreFor(fileSystem),
                new KeyStoreLocation(keyStoreDirectory, keyStoreCase),
                probe ?? storedSecrets ?? (IStoredSecretPresenceProbe)new StagedSecretPresenceProbe(StoredSecretPresence.HoldsNone),
                readability ?? new StagedReadabilityProbe(StoredSecretReadability.NothingStored),
                startAnyway);
        }

        // Named the same way the store names it, so a ring placed by a test is one this instance can unwrap
        // and then fail to parse - the only way to reach the refusal that comes after the unwrapping.
        private byte[] AsThisInstanceWouldHaveWrittenIt(string ringText)
        {
            var dataProtectionHost = new ServiceCollection()
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyStoreDirectory))
                .Services
                .BuildServiceProvider();

            dataProtectionHosts.Add(dataProtectionHost);

            return dataProtectionHost.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("Lighthouse.Encryption.KeyRing.v1")
                .Protect(Encoding.UTF8.GetBytes(ringText));
        }

        private GeneratedKeyRingStore StoreFor(IKeyStoreFileSystem fileSystem)
        {
            var dataProtectionHost = new ServiceCollection()
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyStoreDirectory))
                .Services
                .BuildServiceProvider();

            dataProtectionHosts.Add(dataProtectionHost);

            return new GeneratedKeyRingStore(
                keyStoreDirectory,
                dataProtectionHost.GetRequiredService<IDataProtectionProvider>(),
                fileSystem,
                TimeProvider.System);
        }

        // The bootstrapper is told what the database holds rather than asking it, so a test can put all three
        // answers in front of it - including the one a real database only gives when it is unreachable.
        private sealed class StagedSecretPresenceProbe : IStoredSecretPresenceProbe
        {
            private readonly StoredSecretPresence answer;

            public StagedSecretPresenceProbe(StoredSecretPresence answer)
            {
                this.answer = answer;
            }

            public int TimesAsked { get; private set; }

            public StoredSecretPresence Look()
            {
                TimesAsked++;

                return answer;
            }
        }

        // Whether anything stored can still be read is a question about the key that was resolved, so the
        // ring it was asked about is recorded rather than only the answer given back.
        private sealed class StagedReadabilityProbe : IStoredSecretReadabilityProbe
        {
            private readonly StoredSecretReadability answer;

            private readonly IReadOnlyList<string> keyIdsSeen;

            public StagedReadabilityProbe(StoredSecretReadability answer, params string[] keyIdsSeen)
            {
                this.answer = answer;
                this.keyIdsSeen = keyIdsSeen;
            }

            public int TimesAsked { get; private set; }

            public EncryptionKeyRing? WasAskedAbout { get; private set; }

            public StoredSecretFinding Look(EncryptionKeyRing ring)
            {
                TimesAsked++;
                WasAskedAbout = ring;

                return new StoredSecretFinding(answer, keyIdsSeen);
            }
        }

        // Docker overlayfs and WSL2 DrvFs both accept a write, report success and can hand back something
        // else afterwards, and neither is available to a test run. So the filesystem is a capability the
        // store is given, and this is the one that lies on demand.
        private sealed class StagedKeyStoreFileSystem : IKeyStoreFileSystem
        {
            private readonly Dictionary<string, byte[]> files = new(StringComparer.Ordinal);

            public List<string> Operations { get; } = [];

            public Func<string, byte[], byte[]>? WhatTheFilesystemHandsBack { get; set; }

            public Action<string, string>? WhatHappensOnMove { get; set; }

            public int FileCount => files.Count;

            public bool FileExists(string path)
            {
                return files.ContainsKey(path);
            }

            public byte[] ReadAllBytes(string path)
            {
                Operations.Add($"read {path}");
                var stored = files[path];

                return WhatTheFilesystemHandsBack?.Invoke(path, stored) ?? stored;
            }

            public void WriteAllBytes(string path, byte[] contents)
            {
                Operations.Add($"write {path}");
                files[path] = contents;
            }

            public void Move(string sourcePath, string destinationPath)
            {
                Operations.Add($"move {sourcePath} -> {destinationPath}");
                WhatHappensOnMove?.Invoke(sourcePath, destinationPath);

                files[destinationPath] = files[sourcePath];
                files.Remove(sourcePath);
            }

            public void Place(string path, byte[] contents)
            {
                files[path] = contents;
            }

            public byte[]? Contents(string path)
            {
                return files.TryGetValue(path, out var contents) ? contents : null;
            }
        }
    }
}
