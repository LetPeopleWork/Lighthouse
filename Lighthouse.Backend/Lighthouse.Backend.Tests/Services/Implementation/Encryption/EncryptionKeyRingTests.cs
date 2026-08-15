using Lighthouse.Backend.Models.Encryption;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class EncryptionKeyRingTests
    {
        private const string ActiveKeyId = "k-active";

        private const string RetiredKeyId = "k-retired";

        private const string LegacyKeyId = "k-legacy-default";

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
            Assert.That(() => new EncryptionKeyRing(), Throws.ArgumentException);
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
