using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class SecretEnvelopeTests
    {
        private const string Credential = "pat-7f3c9a2e-not-a-real-token";

        private const string KeyOneId = "key-one";

        private const string KeyTwoId = "key-two";

        private const int VersionField = 0;

        private const int KeyIdField = 1;

        private const int NonceField = 2;

        private const int CiphertextField = 3;

        private const int NonceLength = 12;

        private const int TagLength = 16;

        private static readonly byte[] KeyOneMaterial = Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg=");

        private static readonly byte[] KeyTwoMaterial = Convert.FromBase64String("BdurmHjAsvICR2wy2rjw3ao+2NW/s0TOIf85FOdjx+c=");

        private static readonly EncryptionKey KeyOne = new(KeyOneId, KeyOneMaterial);

        private static readonly EncryptionKey KeyOneIdWithKeyTwosMaterial = new(KeyOneId, KeyTwoMaterial);

        private static readonly string[] InvalidKeyIds =
        [
            "",
            "KeyOne",
            "key_one",
            "key one",
            "key.one",
            "key+one",
            "kéy",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        ];

        private static readonly string?[] UnreadableValues =
        [
            null,
            "",
            "   ",
            "LH1",
            "LH1.",
            "LH1.key-one",
            "LH1.key-one.AAAAAAAAAAAAAAAA",
            "LH1.key-one.AAAAAAAAAAAAAAAA.AAAAAAAAAAAAAAAAAAAAAA.AAAA",
            "LH1.KEY.AAAAAAAAAAAAAAAA.AAAAAAAAAAAAAAAAAAAAAA",
            "LH1.key-one.AAAA.AAAAAAAAAAAAAAAAAAAAAA",
            "LH1.key-one.AAAAAAAAAAAAAAAA.AAAA",
            "LH1.key-one.AAAAAAAAAAAAAAAA.AA+A/AAAAAAAAAAAAAAAAAA==",
            "LH2.key-one.AAAAAAAAAAAAAAAA.AAAAAAAAAAAAAAAAAAAAAA",
            "U2FsdGVkX19hbGVnYWN5Q0JDYmxvYg==",
        ];

        [Test]
        public void Format_ThenTryParse_ReturnsTheSameFields()
        {
            var envelope = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial);
            var stored = envelope.Format();

            var parsed = SecretEnvelope.TryParse(stored, out var roundTripped);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.True);
                Assert.That(roundTripped?.KeyId, Is.EqualTo(KeyOneId));
                Assert.That(roundTripped?.Nonce.ToArray(), Is.EqualTo(envelope.Nonce.ToArray()));
                Assert.That(roundTripped?.CiphertextAndTag.ToArray(), Is.EqualTo(envelope.CiphertextAndTag.ToArray()));
                Assert.That(roundTripped?.Format(), Is.EqualTo(stored));
                Assert.That(roundTripped?.Unprotect(KeyOneMaterial), Is.EqualTo(Credential));
            }
        }

        [Test]
        public void Format_RendersTheVersionKeyIdAndUnpaddedBase64UrlFields()
        {
            var stored = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial).Format();

            var fields = stored.Split('.');

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fields, Has.Length.EqualTo(4));
                Assert.That(fields[VersionField], Is.EqualTo("LH1"));
                Assert.That(fields[KeyIdField], Is.EqualTo(KeyOneId));
                Assert.That(DecodeField(fields, NonceField), Has.Length.EqualTo(NonceLength));
                Assert.That(DecodeField(fields, CiphertextField), Has.Length.EqualTo(Encoding.UTF8.GetByteCount(Credential) + TagLength));
                Assert.That(stored, Does.Not.Contain("+"));
                Assert.That(stored, Does.Not.Contain("/"));
                Assert.That(stored, Does.Not.Contain("="));
            }
        }

        [TestCaseSource(nameof(InvalidKeyIds))]
        public void Protect_KeyIdOutsideTheAllowedCharset_IsRefused(string keyId)
        {
            Assert.That(() => SecretEnvelope.Protect(Credential, keyId, KeyOneMaterial), Throws.ArgumentException);
        }

        [TestCaseSource(nameof(UnreadableValues))]
        public void TryParse_ValueThatIsNotAnEnvelope_ReportsFailureWithoutThrowing(string? value)
        {
            var parsed = SecretEnvelope.TryParse(value, out var envelope);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.False);
                Assert.That(envelope, Is.Null);
            }
        }

        [Test]
        public void Unprotect_VersionFieldAltered_Fails()
        {
            var stored = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial).Format();
            var altered = WithCharacterFlipped(stored, VersionField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadStrict(stored, KeyOneMaterial), Is.EqualTo(Credential));
                Assert.That(() => ReadStrict(altered, KeyOneMaterial), Throws.InstanceOf<CryptographicException>());
            }
        }

        [Test]
        public void Unprotect_KeyIdFieldAltered_Fails()
        {
            var stored = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial).Format();
            var altered = WithCharacterFlipped(stored, KeyIdField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadStrict(stored, KeyOneMaterial), Is.EqualTo(Credential));
                Assert.That(() => ReadStrict(altered, KeyOneMaterial), Throws.InstanceOf<CryptographicException>());
            }
        }

        [Test]
        public void Unprotect_NonceFieldAltered_Fails()
        {
            var stored = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial).Format();
            var altered = WithDecodedByteFlipped(stored, NonceField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadStrict(stored, KeyOneMaterial), Is.EqualTo(Credential));
                Assert.That(() => ReadStrict(altered, KeyOneMaterial), Throws.InstanceOf<CryptographicException>());
            }
        }

        [Test]
        public void Unprotect_CiphertextFieldAltered_Fails()
        {
            var stored = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial).Format();
            var altered = WithDecodedByteFlipped(stored, CiphertextField);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadStrict(stored, KeyOneMaterial), Is.EqualTo(Credential));
                Assert.That(() => ReadStrict(altered, KeyOneMaterial), Throws.InstanceOf<CryptographicException>());
            }
        }

        [Test]
        public void Unprotect_RelabelledWithAnotherKeysId_FailsUnderThatKey()
        {
            var stored = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial).Format();
            var relabelled = WithKeyIdRewritten(stored, KeyTwoId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadStrict(stored, KeyOneMaterial), Is.EqualTo(Credential));
                Assert.That(() => ReadStrict(relabelled, KeyTwoMaterial), Throws.InstanceOf<CryptographicException>());
            }
        }

        // Decrypting the relabelled value under the key that actually wrote it isolates the header binding:
        // nothing but the key id has changed, and nothing but the associated data can notice.
        [Test]
        public void Unprotect_RelabelledWithAnotherKeysId_FailsUnderTheKeyThatWroteIt()
        {
            var stored = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial).Format();
            var relabelled = WithKeyIdRewritten(stored, KeyTwoId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadStrict(stored, KeyOneMaterial), Is.EqualTo(Credential));
                Assert.That(() => ReadStrict(relabelled, KeyOneMaterial), Throws.InstanceOf<CryptographicException>());
            }
        }

        [Test]
        public void Protect_SameCredentialUnderOneKey_NeverRepeatsANonce()
        {
            const int encryptions = 100_000;
            var nonces = new HashSet<string>(encryptions, StringComparer.Ordinal);

            for (var encryption = 0; encryption < encryptions; encryption++)
            {
                var envelope = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial);
                nonces.Add(Convert.ToBase64String(envelope.Nonce.ToArray()));
            }

            Assert.That(nonces, Has.Count.EqualTo(encryptions));
        }

        [Test]
        public void TryUnprotect_EnvelopeUnderTheKeyThatWroteIt_ReportsSuccessAndReturnsThePlainText()
        {
            var envelope = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial);

            var read = envelope.TryUnprotect(KeyOne, out var plainText);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.True);
                Assert.That(plainText, Is.EqualTo(Credential));
            }
        }

        // Every way a stored envelope can fail to authenticate, answered without an exception escaping and
        // without a plaintext coming back. The pristine value is read alongside them so that a change which
        // made everything unreadable could not pass this test.
        [Test]
        public void TryUnprotect_EnvelopeThatDoesNotAuthenticate_ReportsFailureAndReturnsNoPlainText()
        {
            var stored = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial).Format();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TryRead(stored, KeyOne), Is.EqualTo(Credential));
                Assert.That(TryRead(WithDecodedByteFlipped(stored, CiphertextField), KeyOne), Is.Null);
                Assert.That(TryRead(WithDecodedByteFlipped(stored, NonceField), KeyOne), Is.Null);
                Assert.That(TryRead(WithKeyIdRewritten(stored, KeyTwoId), KeyOne), Is.Null);
                Assert.That(TryRead(stored, KeyOneIdWithKeyTwosMaterial), Is.Null);
            }
        }

        [Test]
        public void TryUnprotect_NoKey_IsRefused()
        {
            var envelope = SecretEnvelope.Protect(Credential, KeyOneId, KeyOneMaterial);

            Assert.That(() => envelope.TryUnprotect(null!, out _), Throws.ArgumentNullException);
        }

        // The longest key id the format admits, and the one letter at the far end of the allowed range. Both
        // sit one character away from being refused, and a key id refused at write time is a secret that
        // cannot be stored at all.
        [TestCase("abcdefghijklmnopqrstuvwxyz-01234", TestName = "Protect_KeyIdOfTheGreatestAllowedLength")]
        [TestCase("zzz", TestName = "Protect_KeyIdAtTheEndOfTheAllowedLetterRange")]
        [TestCase("key-9", TestName = "Protect_KeyIdAtTheEndOfTheAllowedDigitRange")]
        public void Protect_KeyIdOnTheEdgeOfTheAllowedShape_IsAcceptedAndReadsBack(string keyId)
        {
            var stored = SecretEnvelope.Protect(Credential, keyId, KeyOneMaterial).Format();

            Assert.That(ReadStrict(stored, KeyOneMaterial), Is.EqualTo(Credential));
        }

        // An empty secret carries no ciphertext at all, only the authentication tag, so the envelope is at its
        // shortest possible length here. Refusing to parse it would send an empty stored value down the legacy
        // path and have it reported as something it is not.
        [Test]
        public void Protect_EmptyCredential_RoundTripsAsAnEnvelope()
        {
            var stored = SecretEnvelope.Protect(string.Empty, KeyOneId, KeyOneMaterial).Format();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(SecretEnvelope.TryParse(stored, out _), Is.True);
                Assert.That(ReadStrict(stored, KeyOneMaterial), Is.Empty);
            }
        }

        [Test]
        public void Protect_NoPlainText_IsRefused()
        {
            Assert.That(() => SecretEnvelope.Protect(null!, KeyOneId, KeyOneMaterial), Throws.ArgumentNullException);
        }

        private static string? TryRead(string stored, EncryptionKey key)
        {
            return SecretEnvelope.TryParse(stored, out var envelope) && envelope.TryUnprotect(key, out var plainText)
                ? plainText
                : null;
        }

        private static string ReadStrict(string stored, byte[] key)
        {
            if (!SecretEnvelope.TryParse(stored, out var envelope))
            {
                throw new CryptographicException("The stored value is not a readable secret envelope.");
            }

            return envelope.Unprotect(key);
        }

        private static byte[] DecodeField(string[] fields, int fieldIndex)
        {
            return Base64Url.DecodeFromChars(fields[fieldIndex]);
        }

        private static string WithCharacterFlipped(string stored, int fieldIndex)
        {
            var fields = stored.Split('.');
            var characters = fields[fieldIndex].ToCharArray();
            characters[0] = (char)(characters[0] ^ 1);
            fields[fieldIndex] = new string(characters);

            return string.Join('.', fields);
        }

        private static string WithDecodedByteFlipped(string stored, int fieldIndex)
        {
            var fields = stored.Split('.');
            var decoded = Base64Url.DecodeFromChars(fields[fieldIndex]);
            decoded[0] ^= 1;
            fields[fieldIndex] = Base64Url.EncodeToString(decoded);

            return string.Join('.', fields);
        }

        private static string WithKeyIdRewritten(string stored, string keyId)
        {
            var fields = stored.Split('.');
            fields[KeyIdField] = keyId;

            return string.Join('.', fields);
        }
    }
}
