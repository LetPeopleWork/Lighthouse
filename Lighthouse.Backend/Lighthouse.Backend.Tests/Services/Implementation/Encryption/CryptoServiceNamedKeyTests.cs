using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// Encrypting under a key that was named rather than under whichever key happens to be in force. A
    /// re-encryption pass has to write every credential under the key it started on, and until now the
    /// only thing it could ask for was "the active one" - which it re-read on every single row.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class CryptoServiceNamedKeyTests
    {
        private static readonly EncryptionKey ActiveKey = new("k-2026-08-17-01", Convert.FromBase64String("Zm9vYmFyYmF6cXV4MTIzNDU2Nzg5MGFiY2RlZmdoaWo="));

        private static readonly EncryptionKey RetiredKey = new("k-2025-11-02-01", Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg="));

        [Test]
        public void ASecretEncryptedUnderANamedKey_NamesThatKeyAndNotTheActiveOne()
        {
            var (crypto, _) = ACryptoServiceHolding(ActiveKey, RetiredKey);

            var stored = crypto.Encrypt("a-stored-credential", RetiredKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(SecretEnvelope.TryParse(stored, out var envelope), Is.True);
                Assert.That(envelope!.KeyId, Is.EqualTo(RetiredKey.Id));
            }
        }

        [Test]
        public void ASecretEncryptedUnderANamedKey_ReadsBackWhileThatKeyIsOnTheRing()
        {
            var (crypto, _) = ACryptoServiceHolding(ActiveKey, RetiredKey);

            var secret = crypto.Read(crypto.Encrypt("a-stored-credential", RetiredKey));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secret.State, Is.EqualTo(SecretState.Envelope));
                Assert.That(secret.KeyId, Is.EqualTo(RetiredKey.Id));
                Assert.That(secret.PlainText, Is.EqualTo("a-stored-credential"));
            }
        }

        [Test]
        public void ASecretEncryptedUnderANamedKey_StopsBeingReadableOnceThatKeyIsOffTheRing()
        {
            var (crypto, holder) = ACryptoServiceHolding(ActiveKey, RetiredKey);
            var stored = crypto.Encrypt("a-stored-credential", RetiredKey);

            holder.Replace(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, ActiveKey));

            var secret = crypto.Read(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secret.State, Is.EqualTo(SecretState.Unreadable));
                Assert.That(secret.PlainText, Is.Null);
            }
        }

        [Test]
        public void ASecretEncryptedWithoutNamingAKey_IsStillWrittenUnderTheActiveOne()
        {
            var (crypto, _) = ACryptoServiceHolding(ActiveKey, RetiredKey);

            var secret = crypto.Read(crypto.Encrypt("a-stored-credential"));

            Assert.That(secret.KeyId, Is.EqualTo(ActiveKey.Id));
        }

        [Test]
        public void EncryptingUnderNoKeyAtAll_IsRefused()
        {
            var (crypto, _) = ACryptoServiceHolding(ActiveKey, RetiredKey);

            Assert.That(() => crypto.Encrypt("a-stored-credential", null!), Throws.ArgumentNullException);
        }

        private static (CryptoService Crypto, EncryptionKeyRingHolder Holder) ACryptoServiceHolding(params EncryptionKey[] keys)
        {
            var holder = new EncryptionKeyRingHolder(new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, keys));

            return (new CryptoService(holder, NullLogger<CryptoService>.Instance), holder);
        }
    }
}
