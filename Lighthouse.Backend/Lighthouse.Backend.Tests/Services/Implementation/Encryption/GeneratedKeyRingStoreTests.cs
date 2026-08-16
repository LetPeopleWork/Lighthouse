using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// Making a key an instance will still have tomorrow. The store is handed the filesystem it works on
    /// because the failure that matters - a write that is accepted, reported as successful and handed back
    /// as something else - is not something a real filesystem can be asked to do on demand.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class GeneratedKeyRingStoreTests
    {
        private static readonly EncryptionKey InForce = new("k-2025-11-02-01", Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg="));

        private static readonly EncryptionKey AlreadyRetired = new("k-2024-04-01-01", Convert.FromBase64String("Zm9vYmFyYmF6cXV4MTIzNDU2Nzg5MGFiY2RlZmdoaWo="));

        private readonly List<ServiceProvider> dataProtectionHosts = [];

        private string keyStoreDirectory = null!;

        private FakeTimeProvider clock = null!;

        [SetUp]
        public void SetUp()
        {
            keyStoreDirectory = Directory.CreateTempSubdirectory("GeneratedKeyRingStore_").FullName;
            clock = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var host in dataProtectionHosts)
            {
                host.Dispose();
            }

            dataProtectionHosts.Clear();
            Directory.Delete(keyStoreDirectory, recursive: true);
        }

        [Test]
        public void MintingOntoARing_PutsTheNewKeyFirst_AndEverythingElseBehindItInOrder()
        {
            var existing = new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, InForce, AlreadyRetired);

            var minted = StoreOver(new AKeyStoreThatKeepsWhatItIsGiven()).MintOnto(existing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(minted.ActiveKey.Id, Is.EqualTo("k-2026-08-16-01"));
                Assert.That(minted.ActiveKey.Material.ToArray(), Is.Not.EqualTo(InForce.Material.ToArray()));
                Assert.That(minted.RetiredKeys.Select(key => key.Id), Is.EqualTo(new[] { InForce.Id, AlreadyRetired.Id }).AsCollection);
                Assert.That(minted.Custody, Is.EqualTo(KeyCustody.GeneratedForThisInstance));
            }
        }

        [Test]
        public void MintingTwiceInOneDay_GivesTheTwoKeysDifferentNames()
        {
            var store = StoreOver(new AKeyStoreThatKeepsWhatItIsGiven());

            var first = store.MintOnto(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, InForce));
            var second = store.MintOnto(first);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.ActiveKey.Id, Is.EqualTo("k-2026-08-16-01"));
                Assert.That(second.ActiveKey.Id, Is.EqualTo("k-2026-08-16-02"),
                    "containing an exposure is exactly the reason somebody rotates twice in one afternoon, and a ring naming one key twice cannot be spelled at all");
            }
        }

        [Test]
        public void TheMintedRing_IsWrittenAsideAndMovedIntoPlace_ThenReadStraightBack()
        {
            var fileSystem = new AKeyStoreThatKeepsWhatItIsGiven();

            var minted = StoreOver(fileSystem).MintOnto(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, InForce));

            var ringFile = Path.Combine(keyStoreDirectory, GeneratedKeyRingStore.RingFileName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fileSystem.Operations, Has.One.StartsWith($"write {ringFile}.").And.Some.EndsWith(GeneratedKeyRingStore.TemporaryFileSuffix));
                Assert.That(fileSystem.Operations, Has.One.StartsWith($"move {ringFile}.").And.Some.EndsWith($"-> {ringFile}"));
                Assert.That(fileSystem.Operations, Does.Contain($"read {ringFile}"));
                Assert.That(minted.ActiveKey.Material.Length, Is.EqualTo(EncryptionKey.MaterialLength));
            }
        }

        [Test]
        public void AStoreThatHandsBackSomethingElse_RaisesRatherThanReturningTheKey()
        {
            var fileSystem = new AKeyStoreThatKeepsWhatItIsGiven
            {
                WhatItHandsBack = (_, stored) => [.. stored.Reverse()],
            };

            Assert.That(
                () => StoreOver(fileSystem).MintOnto(new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, InForce)),
                Throws.InvalidOperationException,
                "a key this machine may not keep would take every secret moved onto it with it");
        }

        [Test]
        public void ARing_LetsGoOfAKeyItIsAskedFor_AndKeepsTheRestInOrder()
        {
            var ring = new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, InForce, AlreadyRetired).WithLegacyDefault();

            var without = ring.Without(AlreadyRetired.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(without.ActiveKey.Id, Is.EqualTo(InForce.Id));
                Assert.That(without.TryGet(AlreadyRetired.Id, out _), Is.False);
                Assert.That(without.TryGet(LegacyDefaultEncryptionKey.Id, out _), Is.True);
            }
        }

        [Test]
        public void ARing_AskedToLetGoOfAKeyItDoesNotHold_IsUnchanged()
        {
            var ring = new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, InForce);

            Assert.That(ring.Without("k-never-existed"), Is.SameAs(ring));
        }

        [Test]
        public void ARing_RefusesToLetGoOfTheKeyItIsWritingUnder()
        {
            var ring = new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, InForce, AlreadyRetired);

            Assert.That(() => ring.Without(InForce.Id), Throws.ArgumentException,
                "nothing written under it could be read afterwards");
        }

        private GeneratedKeyRingStore StoreOver(IKeyStoreFileSystem fileSystem)
        {
            var dataProtectionHost = GeneratedKeyRingStore.ProtectionKeptBesideTheKeyStore(keyStoreDirectory);
            dataProtectionHosts.Add(dataProtectionHost);

            return new GeneratedKeyRingStore(
                keyStoreDirectory,
                dataProtectionHost.GetRequiredService<IDataProtectionProvider>(),
                fileSystem,
                clock);
        }

        private sealed class AKeyStoreThatKeepsWhatItIsGiven : IKeyStoreFileSystem
        {
            private readonly Dictionary<string, byte[]> files = new(StringComparer.Ordinal);

            public List<string> Operations { get; } = [];

            public Func<string, byte[], byte[]>? WhatItHandsBack { get; set; }

            public bool FileExists(string path)
            {
                return files.ContainsKey(path);
            }

            public byte[] ReadAllBytes(string path)
            {
                Operations.Add($"read {path}");
                var stored = files[path];

                return WhatItHandsBack?.Invoke(path, stored) ?? stored;
            }

            public void WriteAllBytes(string path, byte[] contents)
            {
                Operations.Add($"write {path}");
                files[path] = contents;
            }

            public void Move(string sourcePath, string destinationPath)
            {
                Operations.Add($"move {sourcePath} -> {destinationPath}");
                files[destinationPath] = files[sourcePath];
                files.Remove(sourcePath);
            }
        }
    }
}
