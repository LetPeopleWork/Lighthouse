using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using NUnit.Framework;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// The two questions everything else asks about the key published with the product: is this key that
    /// key, and can that key read this stored value. Both are asked because the name a key wears is
    /// derived from its own bytes and so says nothing about whose bytes they are.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class LegacyDefaultEncryptionKeyTests
    {
        private const string ACredential = "a personal access token";

        private static readonly EncryptionKey OwnKey =
            new("k-2026-08-16-01", RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

        [Test]
        public void Matches_ThePublishedKeysOwnMaterial_SaysYes()
        {
            Assert.That(LegacyDefaultEncryptionKey.Matches(ThePublishedKey().Material.Span), Is.True);
        }

        [Test]
        public void Matches_AnyOtherKeyOfTheRightLength_SaysNo()
        {
            Assert.That(LegacyDefaultEncryptionKey.Matches(OwnKey.Material.Span), Is.False);
        }

        [Test]
        public void Matches_MaterialOfTheWrongLength_SaysNoRatherThanRefusingToAnswer()
        {
            Assert.That(
                LegacyDefaultEncryptionKey.Matches(ThePublishedKey().Material.Span[..16]),
                Is.False,
                "a key of the wrong length is refused elsewhere, and this question is asked before that happens");
        }

        [Test]
        public void CanRead_AValueInTheFormatThisVersionReplacedWrittenUnderIt_SaysYes()
        {
            Assert.That(LegacyDefaultEncryptionKey.CanRead(InTheFormatThisVersionReplaced(ThePublishedKey())), Is.True);
        }

        [Test]
        public void CanRead_AValueInTheFormatThisVersionReplacedWrittenUnderAnotherKey_SaysNo()
        {
            Assert.That(
                LegacyDefaultEncryptionKey.CanRead(InTheFormatThisVersionReplaced(OwnKey)),
                Is.False,
                "an install that set a key of its own before this release stores values that look exactly the same");
        }

        [Test]
        public void CanRead_AnEnvelopeNamingIt_SaysYes()
        {
            Assert.That(LegacyDefaultEncryptionKey.CanRead(Under(ThePublishedKey())), Is.True);
        }

        [Test]
        public void CanRead_AnEnvelopeWearingAnotherNameOverThisKeysMaterial_SaysYes()
        {
            var wearingAnotherName = new EncryptionKey("k-cfg-27d69a05", ThePublishedKey().Material.Span);

            Assert.That(
                LegacyDefaultEncryptionKey.CanRead(Under(wearingAnotherName)),
                Is.True,
                "the question is which key wrote the value, and the name on it is whatever supplied it was called");
        }

        [Test]
        public void CanRead_AnEnvelopeOnAKeyOfTheInstancesOwn_SaysNo()
        {
            Assert.That(LegacyDefaultEncryptionKey.CanRead(Under(OwnKey)), Is.False);
        }

        [Test]
        public void CanRead_AValueNothingEverEncrypted_SaysNo()
        {
            Assert.That(
                LegacyDefaultEncryptionKey.CanRead("not encrypted at all"),
                Is.False,
                "that is a different problem, and the check pass reports it in its own state");
        }

        [TestCase(null)]
        [TestCase("")]
        public void CanRead_NothingStored_SaysNo(string? storedValue)
        {
            Assert.That(LegacyDefaultEncryptionKey.CanRead(storedValue), Is.False);
        }

        // The published key's material is compiled in with no accessor of its own, so the only way to hold
        // it is the way production does: append it to a ring and take it back off the end.
        private static EncryptionKey ThePublishedKey()
        {
            var scaffold = new EncryptionKeyRing(
                new EncryptionKey("k-not-the-published-one", new byte[EncryptionKey.MaterialLength]));

            return scaffold.WithLegacyDefault().RetiredKeys[0];
        }

        private static string Under(EncryptionKey key)
        {
            return SecretEnvelope.Protect(ACredential, key.Id, key.Material.Span).Format();
        }

        // What every install written before this release holds: AES-CBC, an initialisation vector in
        // front, and no key id anywhere on the value.
        private static string InTheFormatThisVersionReplaced(EncryptionKey key)
        {
            using var aes = Aes.Create();
            aes.Key = key.Material.ToArray();

            var iv = RandomNumberGenerator.GetBytes(16);

            return Convert.ToBase64String([.. iv, .. aes.EncryptCbc(Encoding.UTF8.GetBytes(ACredential), iv)]);
        }
    }
}
