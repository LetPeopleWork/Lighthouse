using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class EncryptionKeyRingTests
    {
        private const string ActiveKeyId = "k-active";

        private const string RetiredKeyId = "k-retired";

        private const string LegacyKeyId = "k-legacy-default";

        // The exact string every build before this release shipped in appsettings.json. Pinned as a literal so
        // that quietly changing the compiled-in value fails here rather than in a customer's unreadable secret.
        private const string PublishedKeyMaterial = "uH2VbF5hOW0/huLOH1Q2L0g+P3J9dG43cknQK7t9R5M=";

        private static readonly string[] RetiredIdsAfterAppendingThePublishedKey = [RetiredKeyId, LegacyKeyId];

        private static readonly KeyCustody[] EveryCustody = Enum.GetValues<KeyCustody>();

        [Test]
        public void Ring_BuiltFromOneEntry_ReportsThatEntryActiveAndHoldsNoRetiredKeys()
        {
            var key = KeyWith(ActiveKeyId);

            var ring = new EncryptionKeyRing(key);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.ActiveKey, Is.SameAs(key));
                Assert.That(ring.RetiredKeys, Is.Empty);
            }
        }

        [Test]
        public void Ring_BuiltFromSeveralEntries_ReportsTheFirstActiveAndTheRestRetired()
        {
            var active = KeyWith(ActiveKeyId);
            var firstRetired = KeyWith(RetiredKeyId);
            var secondRetired = KeyWith(LegacyKeyId);

            var ring = new EncryptionKeyRing(active, firstRetired, secondRetired);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.ActiveKey, Is.SameAs(active));
                Assert.That(ring.RetiredKeys, Has.Count.EqualTo(2));
                Assert.That(ring.RetiredKeys[0], Is.SameAs(firstRetired));
                Assert.That(ring.RetiredKeys[1], Is.SameAs(secondRetired));
            }
        }

        [Test]
        public void Ring_BuiltWithNoEntries_IsRefused()
        {
            Assert.That(
                () => new EncryptionKeyRing(),
                Throws.ArgumentException.With.Message.Contains("must hold at least one key"));
        }

        [Test]
        public void Ring_BuiltWithADuplicateKeyId_IsRefusedAndTheFailureNamesTheId()
        {
            Assert.That(
                () => new EncryptionKeyRing(KeyWith(ActiveKeyId), KeyWith(RetiredKeyId), KeyWith(ActiveKeyId)),
                Throws.ArgumentException.With.Message.Contains(ActiveKeyId));
        }

        [TestCase(0)]
        [TestCase(16)]
        [TestCase(31)]
        [TestCase(33)]
        public void Key_MaterialThatIsNot32Bytes_IsRefusedAndTheFailureNamesTheEntry(int materialLength)
        {
            Assert.That(
                () => new EncryptionKey(ActiveKeyId, MaterialOfLength(materialLength)),
                Throws.ArgumentException.With.Message.Contains(ActiveKeyId));
        }

        [Test]
        public void Key_MaterialThatIsNot32Bytes_IsRefusedWithoutQuotingAnyOfTheMaterial()
        {
            var material = RandomNumberGenerator.GetBytes(31);

            Assert.That(
                () => new EncryptionKey(ActiveKeyId, material),
                Throws.ArgumentException
                    .With.Message.Not.Contains(Convert.ToBase64String(material))
                    .And.Message.Not.Contains(Convert.ToHexString(material))
                    .And.Message.Not.Contains(Convert.ToHexString(material, 0, 2)));
        }

        [Test]
        public void TryGet_RetiredKeyId_ResolvesAsReadilyAsTheActiveOne()
        {
            var active = KeyWith(ActiveKeyId);
            var retired = KeyWith(RetiredKeyId);
            var ring = new EncryptionKeyRing(active, retired);

            var activeFound = ring.TryGet(ActiveKeyId, out var resolvedActive);
            var retiredFound = ring.TryGet(RetiredKeyId, out var resolvedRetired);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(activeFound, Is.True);
                Assert.That(resolvedActive, Is.SameAs(active));
                Assert.That(retiredFound, Is.True);
                Assert.That(resolvedRetired, Is.SameAs(retired));
            }
        }

        [Test]
        public void TryGet_KeyIdTheRingDoesNotHold_ReportsFailure()
        {
            var ring = new EncryptionKeyRing(KeyWith(ActiveKeyId));

            var found = ring.TryGet(RetiredKeyId, out var resolved);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(found, Is.False);
                Assert.That(resolved, Is.Null);
            }
        }

        [Test]
        public void Ring_WithTheSameEntriesInTheSameOrder_IsEqual()
        {
            var one = new EncryptionKeyRing(KeyWith(ActiveKeyId), KeyWith(RetiredKeyId));
            var other = new EncryptionKeyRing(KeyWith(ActiveKeyId), KeyWith(RetiredKeyId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(one, Is.EqualTo(other));
                Assert.That(one.GetHashCode(), Is.EqualTo(other.GetHashCode()));
            }
        }

        [Test]
        public void Ring_WithTheSameEntriesInADifferentOrder_IsNotEqual()
        {
            var one = new EncryptionKeyRing(KeyWith(ActiveKeyId), KeyWith(RetiredKeyId));
            var other = new EncryptionKeyRing(KeyWith(RetiredKeyId), KeyWith(ActiveKeyId));

            Assert.That(one, Is.Not.EqualTo(other));
        }

        [Test]
        public void Key_MaterialHandedToTheConstructor_IsNotShared()
        {
            var material = RandomNumberGenerator.GetBytes(32);
            var key = new EncryptionKey(ActiveKeyId, material);
            var asStored = key.Material.ToArray();

            material[0] ^= 1;

            Assert.That(key.Material.ToArray(), Is.EqualTo(asStored));
        }

        // Two keys that share a name but not their material are the situation rotation exists to make
        // visible. Treating them as the same key would have a ring silently accept the wrong material under a
        // name secrets were already written under, and every one of those secrets would stop being readable
        // with nothing to say why.
        [Test]
        public void Key_WithTheSameIdButDifferentMaterial_IsNotEqual()
        {
            var key = new EncryptionKey(ActiveKeyId, MaterialOfLength(32));
            var namesake = new EncryptionKey(ActiveKeyId, MaterialOfLength(32));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(key, Is.Not.EqualTo(namesake));
                Assert.That(key.GetHashCode(), Is.Not.EqualTo(namesake.GetHashCode()));
            }
        }

        [Test]
        public void Key_WithTheSameMaterialButADifferentId_IsNotEqual()
        {
            var material = MaterialOfLength(32);
            var key = new EncryptionKey(ActiveKeyId, material);
            var renamed = new EncryptionKey(RetiredKeyId, material);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(key, Is.Not.EqualTo(renamed));
                Assert.That(key.GetHashCode(), Is.Not.EqualTo(renamed.GetHashCode()));
            }
        }

        [Test]
        public void Key_ComparedWithSomethingThatIsNotAKey_IsNotEqualAndDoesNotThrow()
        {
            var key = KeyWith(ActiveKeyId);

            // Both Equals overloads are called by hand on purpose: the typed one and the object one
            // can diverge, and a wrong answer from either is how a retired key silently stops matching
            // the value it was stored under. Routing these through Is.EqualTo would exercise NUnit's
            // comparer instead, and would collapse the two null cases into the same assertion.
#pragma warning disable NUnit2010
            using (Assert.EnterMultipleScope())
            {
                Assert.That(key.Equals(null), Is.False);
                Assert.That(key.Equals((object?)null), Is.False);
                Assert.That(key.Equals((object?)ActiveKeyId), Is.False);
                Assert.That(key.Equals((object?)KeyWith(ActiveKeyId)), Is.True);
            }
#pragma warning restore NUnit2010
        }

        [TestCase(null, TestName = "Key_BuiltWithNoIdAtAll_IsRefused")]
        [TestCase("   ", TestName = "Key_BuiltWithABlankId_IsRefused")]
        public void Key_BuiltWithoutAnId_IsRefused(string? keyId)
        {
            Assert.That(() => new EncryptionKey(keyId!, MaterialOfLength(32)), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void Ring_BuiltWithoutAnyEntriesAtAll_IsRefused()
        {
            Assert.That(() => new EncryptionKeyRing(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Ring_ComparedWithSomethingThatIsNotARing_IsNotEqualAndDoesNotThrow()
        {
            var ring = new EncryptionKeyRing(KeyWith(ActiveKeyId));

            // Called by hand for the same reason as the key comparison above — the object overload is
            // the one a dictionary or a set would reach for, so it is the one worth pinning directly.
#pragma warning disable NUnit2010
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Equals((object?)null), Is.False);
                Assert.That(ring.Equals((object?)ActiveKeyId), Is.False);
                Assert.That(ring.Equals((object?)new EncryptionKeyRing(KeyWith(ActiveKeyId))), Is.True);
            }
#pragma warning restore NUnit2010
        }

        [Test]
        public void Ring_HoldingDifferentKeys_DoesNotShareAHashCode()
        {
            var ring = new EncryptionKeyRing(KeyWith(ActiveKeyId));
            var other = new EncryptionKeyRing(KeyWith(RetiredKeyId));

            Assert.That(ring.GetHashCode(), Is.Not.EqualTo(other.GetHashCode()));
        }

        [TestCase(KeyCustody.GeneratedForThisInstance)]
        [TestCase(KeyCustody.SuppliedByConfiguration)]
        [TestCase(KeyCustody.SuppliedByExternalSecret)]
        [TestCase(KeyCustody.NoDurableStore)]
        public void Ring_ReportsTheCustodyItWasBuiltWith(KeyCustody custody)
        {
            var ring = new EncryptionKeyRing(custody, KeyWith(ActiveKeyId));

            Assert.That(ring.Custody, Is.EqualTo(custody));
        }

        // Minting is offered only where Lighthouse wrote the key itself. Everywhere else the value the
        // operator supplied wins again on the next restart, so a key minted over it would leave every secret
        // written under it unreadable.
        [TestCase(KeyCustody.GeneratedForThisInstance, true)]
        [TestCase(KeyCustody.SuppliedByConfiguration, false)]
        [TestCase(KeyCustody.SuppliedByExternalSecret, false)]
        [TestCase(KeyCustody.NoDurableStore, false)]
        public void Ring_OffersToMintOnlyWhereThisInstanceOwnsTheKeyStore(KeyCustody custody, bool canMint)
        {
            var ring = new EncryptionKeyRing(custody, KeyWith(ActiveKeyId));

            Assert.That(ring.CanMint, Is.EqualTo(canMint));
        }

        [Test]
        public void Ring_BuiltWithoutNamingItsCustody_ClaimsNoDurableStoreAndCannotMint()
        {
            var ring = new EncryptionKeyRing(KeyWith(ActiveKeyId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Custody, Is.EqualTo(KeyCustody.NoDurableStore));
                Assert.That(ring.CanMint, Is.False);
            }
        }

        [Test]
        public void Ring_WithThePublishedKeyAppended_KeepsItsOwnActiveKeyAndItsCustody()
        {
            var active = KeyWith(ActiveKeyId);
            var ring = new EncryptionKeyRing(KeyCustody.GeneratedForThisInstance, active, KeyWith(RetiredKeyId));

            var extended = ring.WithLegacyDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(extended.ActiveKey, Is.SameAs(active));
                Assert.That(extended.Custody, Is.EqualTo(KeyCustody.GeneratedForThisInstance));
                Assert.That(extended.RetiredKeys.Select(key => key.Id), Is.EqualTo(RetiredIdsAfterAppendingThePublishedKey));
            }
        }

        [Test]
        public void Ring_WithThePublishedKeyAppendedTwice_HoldsItOnce()
        {
            var ring = new EncryptionKeyRing(KeyWith(ActiveKeyId)).WithLegacyDefault().WithLegacyDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.RetiredKeys, Has.Count.EqualTo(1));
                Assert.That(ring.RetiredKeys[0].Id, Is.EqualTo(LegacyKeyId));
            }
        }

        [Test]
        public void Ring_HasNoPathAtAllThatMovesTheActiveKey()
        {
            var producers = typeof(EncryptionKeyRing)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.ReturnType == typeof(EncryptionKeyRing))
                .ToList();

            var activeKeyIds = (from custody in EveryCustody
                                from producer in producers
                                select ActiveKeyIdAfterInvoking(producer, custody)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(producers, Is.Not.Empty);
                Assert.That(activeKeyIds, Has.All.EqualTo(ActiveKeyId));
            }
        }

        // The published key stays out of the active position because nothing hands it out as a value: there is
        // no member anywhere that returns one, so there is no argument to give a constructor in any order. The
        // only way it enters a ring is by being appended, and appending is the only shape the ring offers.
        [Test]
        public void PublishedKey_IsNeverHandedOutAsAValueSomethingCouldPutFirst()
        {
            var typesHandedOut = typeof(LegacyDefaultEncryptionKey)
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .Select(TypeCarriedBy)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(typesHandedOut, Has.None.EqualTo(typeof(EncryptionKey)));
                Assert.That(typesHandedOut, Has.None.EqualTo(typeof(EncryptionKey[])));
            }
        }

        [Test]
        public void PublishedKey_IsNamedAndKeyedExactlyAsEarlierBuildsWroteIt()
        {
            var published = new EncryptionKeyRing(KeyWith(ActiveKeyId)).WithLegacyDefault().RetiredKeys[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(published.Id, Is.EqualTo(LegacyKeyId));
                Assert.That(published.Material.ToArray(), Is.EqualTo(Convert.FromBase64String(PublishedKeyMaterial)));
            }
        }

        private static string ActiveKeyIdAfterInvoking(MethodInfo producer, KeyCustody custody)
        {
            var ring = new EncryptionKeyRing(custody, KeyWith(ActiveKeyId), KeyWith(RetiredKeyId));
            object[] arguments = producer.GetParameters().Length == 0 ? [] : [KeyWith(LegacyKeyId)];

            var produced = (EncryptionKeyRing)producer.Invoke(ring, arguments)!;

            return produced.ActiveKey.Id;
        }

        private static Type? TypeCarriedBy(MemberInfo member)
        {
            return member switch
            {
                MethodInfo method => method.ReturnType,
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null,
            };
        }

        private static EncryptionKey KeyWith(string keyId)
        {
            return new EncryptionKey(keyId, SHA256.HashData(Encoding.UTF8.GetBytes(keyId)));
        }

        private static byte[] MaterialOfLength(int length)
        {
            return RandomNumberGenerator.GetBytes(length);
        }
    }
}
