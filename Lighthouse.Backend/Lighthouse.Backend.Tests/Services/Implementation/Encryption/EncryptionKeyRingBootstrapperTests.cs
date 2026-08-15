using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
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
        ];

        private const string MountedSecretPath = "/etc/lighthouse/encryption/keyring";

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
                Assert.That(ring.RetiredKeys, Is.Empty);
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
            var temporaryPath = store.RingFilePath + GeneratedKeyRingStore.TemporaryFileSuffix;

            var expectedOperations = new List<string>
            {
                $"write {temporaryPath}",
                $"move {temporaryPath} -> {store.RingFilePath}",
                $"read {store.RingFilePath}",
            };

            _ = store.Mint();

            Assert.That(staged.Operations, Is.EqualTo(expectedOperations));
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

            Assert.That(refusal.Message, Does.Contain(ringFilePath));
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
                Assert.That(staged.Contents(ringFilePath), Is.EqualTo(unreadable));
                Assert.That(staged.FileCount, Is.EqualTo(1));
                Assert.That(staged.Operations, Has.No.Member($"write {ringFilePath}"));
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(configuredDefect.Message, Does.Not.Contain(encoded));
                Assert.That(mountedDefect.Message, Does.Not.Contain(encoded));
                Assert.That(configuredDefect.Message, Does.Not.Contain(Convert.ToHexStringLower(material)));
                Assert.That(mountedDefect.Message, Does.Not.Contain(Convert.ToHexStringLower(material)));
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
            string? keysFilePath = null)
        {
            return new EncryptionKeyRingBootstrapper(
                new ConfiguredKeyRingSource(suppliedRing, suppliedKey),
                new MountedFileKeyRingSource(keysFilePath, fileSystem),
                StoreFor(fileSystem));
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
