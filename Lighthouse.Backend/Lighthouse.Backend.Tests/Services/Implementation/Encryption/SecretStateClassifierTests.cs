using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class SecretStateClassifierTests
    {
        private const string Credential = "pat-7f3c9a2e-not-a-real-token";

        private const string ActiveKeyId = "key-active";

        private const string RetiredKeyId = "key-retired";

        private const string StrangerKeyId = "key-stranger";

        private const int CiphertextField = 3;

        private const int IvLength = 16;

        private static readonly byte[] ActiveKeyMaterial = Convert.FromBase64String("jcZatOnLrOP2HUMH4s43VB5Ci7uiCipa3odpR0edbKg=");

        private static readonly byte[] RetiredKeyMaterial = Convert.FromBase64String("BdurmHjAsvICR2wy2rjw3ao+2NW/s0TOIf85FOdjx+c=");

        private static readonly byte[] StrangerKeyMaterial = Convert.FromBase64String("2gMy+eBfMpbvIUlN9fyFHwpBNlNBpw+SVuAOmMVXsaE=");

        private static readonly byte[] FixedIv = Convert.FromBase64String("AAECAwQFBgcICQoLDA0ODw==");

        private static readonly SecretState[] LegacyPlaintextOnly = [SecretState.LegacyPlaintext];

        private static readonly string?[] EveryShapeOfInput =
        [
            null,
            "",
            "   ",
            "LH1",
            "LH1.",
            "LH1.key-active",
            "LH1.key-active.AAAAAAAAAAAAAAAA",
            "LH1.key-active.AAAAAAAAAAAAAAAA.AAAAAAAAAAAAAAAAAAAAAA",
            "LH1.key-active.....",
            "....",
            "=",
            "AAAA",
            "not base64 at all",
            "token_aValueThatWasNeverEncrypted",
            "🔑🔑🔑",
            new string('A', 100_000),
        ];

        [Test]
        public void Classify_EnvelopeUnderTheNamedRingKey_ReturnsEnvelopeAndTheOriginalPlainText()
        {
            var stored = SecretEnvelope.Protect(Credential, ActiveKeyId, ActiveKeyMaterial).Format();

            var result = CreateClassifier().Classify(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.Envelope));
                Assert.That(result.PlainText, Is.EqualTo(Credential));
                Assert.That(result.KeyId, Is.EqualTo(ActiveKeyId));
            }
        }

        [Test]
        public void Classify_EnvelopeUnderARetiredRingKey_ReturnsEnvelopeAndTheOriginalPlainText()
        {
            var stored = SecretEnvelope.Protect(Credential, RetiredKeyId, RetiredKeyMaterial).Format();

            var result = CreateClassifier().Classify(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.Envelope));
                Assert.That(result.PlainText, Is.EqualTo(Credential));
                Assert.That(result.KeyId, Is.EqualTo(RetiredKeyId));
            }
        }

        [Test]
        public void Classify_EnvelopeNamingAKeyTheRingDoesNotHold_ReturnsUnreadableAndTheClaimedKeyId()
        {
            var stored = SecretEnvelope.Protect(Credential, StrangerKeyId, StrangerKeyMaterial).Format();

            var result = CreateClassifier().Classify(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.Unreadable));
                Assert.That(result.PlainText, Is.Null);
                Assert.That(result.KeyId, Is.EqualTo(StrangerKeyId));
            }
        }

        [Test]
        public void Classify_EnvelopeWhoseTagFails_ReturnsUnreadable()
        {
            var stored = SecretEnvelope.Protect(Credential, ActiveKeyId, ActiveKeyMaterial).Format();
            var altered = WithDecodedByteFlipped(stored, CiphertextField);

            var result = CreateClassifier().Classify(altered);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.Unreadable));
                Assert.That(result.PlainText, Is.Null);
                Assert.That(result.KeyId, Is.EqualTo(ActiveKeyId));
            }
        }

        [Test]
        public void Classify_LegacyCbcBlobWrittenByThePreviousImplementation_ReturnsLegacyCbcAndItsPlainText()
        {
            var stored = LegacyCbcBlob(Credential, ActiveKeyMaterial);

            var result = CreateClassifier().Classify(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.LegacyCbc));
                Assert.That(result.PlainText, Is.EqualTo(Credential));
                Assert.That(result.KeyId, Is.EqualTo(ActiveKeyId));
            }
        }

        [Test]
        public void Classify_LegacyCbcBlobWrittenByARetiredKey_ReturnsLegacyCbcAndItsPlainText()
        {
            var stored = LegacyCbcBlob(Credential, RetiredKeyMaterial);

            var result = CreateClassifier().Classify(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.LegacyCbc));
                Assert.That(result.PlainText, Is.EqualTo(Credential));
                Assert.That(result.KeyId, Is.EqualTo(RetiredKeyId));
            }
        }

        // The single most important case in this class. A value shaped like a legacy blob that no key on the
        // ring can read must be reported as unreadable, because calling it plaintext and handing it back is
        // exactly the silent failure this epic exists to delete.
        [Test]
        public void Classify_CbcShapedValueNoRingKeyCanRead_ReturnsUnreadableAndNeverLegacyPlaintext()
        {
            var stored = LegacyCbcBlob(Credential, StrangerKeyMaterial, FixedIv);

            var result = CreateClassifier().Classify(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.Unreadable));
                Assert.That(result.State, Is.Not.EqualTo(SecretState.LegacyPlaintext));
                Assert.That(result.PlainText, Is.Null);
            }
        }

        [TestCase("", TestName = "Classify_LegacyPlaintext_EmptyValue")]
        [TestCase("token_aValueThatWasNeverEncrypted", TestName = "Classify_LegacyPlaintext_TokenOutsideTheBase64Alphabet")]
        [TestCase("pat-7f3c9a2e-not-a-real-token", TestName = "Classify_LegacyPlaintext_ValueContainingTheBase64Delimiter")]
        [TestCase("AAAA", TestName = "Classify_LegacyPlaintext_Base64ButFarTooShortToCarryAnIv")]
        [TestCase("AAAAAAAAAAAAAAAAAAAAAAAA", TestName = "Classify_LegacyPlaintext_Base64OfEighteenBytesIsNotAWholeNumberOfBlocks")]
        [TestCase("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", TestName = "Classify_LegacyPlaintext_Base64OfThirtyBytesIsShorterThanAnIvAndABlock")]
        public void Classify_ValueThatWasNeverEncrypted_ReturnsLegacyPlaintextByAPositiveShapeCheck(string stored)
        {
            var result = CreateClassifier().Classify(stored);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.LegacyPlaintext));
                Assert.That(result.PlainText, Is.EqualTo(stored));
                Assert.That(result.KeyId, Is.Null);
            }
        }

        // '.' is outside the standard base64 alphabet, so no value the previous implementation could write
        // can begin with the envelope prefix. This is a property of the two alphabets, not a probability,
        // and the sample size is here to make a regression in either format loud.
        [Test]
        public void Classify_TenThousandRandomLegacyCbcBlobs_NeverStartWithThePrefixAndNeverClassifyAsEnvelope()
        {
            const int blobCount = 10_000;

            var classifier = CreateClassifier();
            var prefixedBlobs = 0;
            var envelopeClassifications = 0;

            for (var blob = 0; blob < blobCount; blob++)
            {
                var stored = LegacyCbcBlob(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength));

                if (stored.StartsWith(SecretEnvelope.Prefix, StringComparison.Ordinal))
                {
                    prefixedBlobs++;
                }

                if (classifier.Classify(stored).State == SecretState.Envelope)
                {
                    envelopeClassifications++;
                }
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(prefixedBlobs, Is.Zero);
                Assert.That(envelopeClassifications, Is.Zero);
            }
        }

        [Test]
        public void Classify_AnyInputWhatsoever_ReturnsAStateAndNeverThrows()
        {
            var classifier = CreateClassifier();

            Assert.That(() => Array.ConvertAll(EveryShapeOfInput, classifier.Classify), Throws.Nothing);
        }

        [Test]
        public void Classify_ValueOutsideTheBase64Alphabet_IsNeverReportedAsUnreadable()
        {
            var classifier = CreateClassifier();

            var states = Array.ConvertAll(EveryShapeOfInput, input => classifier.Classify(input).State);

            Assert.That(states.Where(state => state != SecretState.Envelope && state != SecretState.Unreadable).Distinct(), Is.EquivalentTo(LegacyPlaintextOnly));
        }

        [Test]
        public void UnreadableSecretException_CarriesTheStateAndTheClaimedKeyIdAndNoMaterial()
        {
            var exception = new UnreadableSecretException(SecretState.Unreadable, StrangerKeyId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.State, Is.EqualTo(SecretState.Unreadable));
                Assert.That(exception.ClaimedKeyId, Is.EqualTo(StrangerKeyId));
                Assert.That(exception.Message, Does.Contain(StrangerKeyId));
                Assert.That(exception.Message, Does.Not.Contain(Credential));
                Assert.That(exception.Message, Does.Not.Contain(Convert.ToBase64String(StrangerKeyMaterial)));
            }
        }

        // PKCS7 pads up to the next block boundary, so a credential one character short of a block ends in a
        // single 0x01 and one that fills a block exactly gains a whole extra block of 0x10. An empty one is
        // nothing but padding. All three are ordinary secrets somebody typed, and all three sit on an edge of
        // the range this classifier accepts, where an off-by-one would report a readable secret unreadable.
        [TestCase("123456789012345", TestName = "Classify_LegacyCbc_CredentialOneCharacterShortOfABlock")]
        [TestCase("1234567890123456", TestName = "Classify_LegacyCbc_CredentialFillingAWholeBlock")]
        [TestCase("", TestName = "Classify_LegacyCbc_EmptyCredentialIsNothingButPadding")]
        public void Classify_LegacyCbcBlobOnAPaddingBoundary_StillReadsBackItsPlainText(string credential)
        {
            var result = CreateClassifier().Classify(LegacyCbcBlob(credential, ActiveKeyMaterial));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.State, Is.EqualTo(SecretState.LegacyCbc));
                Assert.That(result.PlainText, Is.EqualTo(credential));
            }
        }

        [Test]
        public void Classify_CbcShapedValueClaimingMorePaddingThanABlockHolds_ReturnsUnreadable()
        {
            var result = CreateClassifier().Classify(CbcBlobOfRawBytes(PaddedWith(20, 20), ActiveKeyMaterial));

            Assert.That(result.State, Is.EqualTo(SecretState.Unreadable));
        }

        [Test]
        public void Classify_CbcShapedValueWhosePaddingBytesDisagree_ReturnsUnreadable()
        {
            var result = CreateClassifier().Classify(CbcBlobOfRawBytes(PaddedWith(8, 1), ActiveKeyMaterial));

            Assert.That(result.State, Is.EqualTo(SecretState.Unreadable));
        }

        [Test]
        public void Classify_CbcShapedValueThatDecryptsToBytesThatAreNotUtf8_ReturnsUnreadable()
        {
            var raw = PaddedWith(16, 16);
            Array.Fill(raw, byte.MaxValue, 0, 16);

            var result = CreateClassifier().Classify(CbcBlobOfRawBytes(raw, ActiveKeyMaterial));

            Assert.That(result.State, Is.EqualTo(SecretState.Unreadable));
        }

        [Test]
        public void Classify_CbcShapedValueThatDecryptsToTextCarryingAControlCharacter_ReturnsUnreadable()
        {
            var raw = PaddedWith(16, 16);
            raw[7] = 7;

            var result = CreateClassifier().Classify(CbcBlobOfRawBytes(raw, ActiveKeyMaterial));

            Assert.That(result.State, Is.EqualTo(SecretState.Unreadable));
        }

        [Test]
        public void Classifier_BuiltWithoutAKeyRing_IsRefused()
        {
            Assert.That(() => new SecretStateClassifier(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void UnreadableSecretException_WithNoClaimedKeyId_SaysSoRatherThanNamingAnEmptyKey()
        {
            var exception = new UnreadableSecretException(SecretState.LegacyCbc, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.ClaimedKeyId, Is.Null);
                Assert.That(exception.Message, Does.Contain("names no encryption key"));
                Assert.That(exception.Message, Does.Not.Contain("''"));
            }
        }

        private static SecretStateClassifier CreateClassifier()
        {
            var ring = new EncryptionKeyRing(
                new EncryptionKey(ActiveKeyId, ActiveKeyMaterial),
                new EncryptionKey(RetiredKeyId, RetiredKeyMaterial));

            return new SecretStateClassifier(new EncryptionKeyRingHolder(ring));
        }

        // Reproduces byte for byte what the previous CryptoService wrote: base64 of the IV followed by
        // AES-CBC ciphertext with PKCS7 padding, carrying no version, no key id and no authentication tag.
        private static string LegacyCbcBlob(string plainText, byte[] keyMaterial, byte[]? iv = null)
        {
            using var aes = Aes.Create();
            aes.Key = keyMaterial;

            var initialisationVector = iv ?? RandomNumberGenerator.GetBytes(IvLength);
            var cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(plainText), initialisationVector);

            return Convert.ToBase64String([.. initialisationVector, .. cipher]);
        }

        // A blob written with the padding left off, so a test can choose the exact bytes the classifier sees
        // after it decrypts. This is what a wrong key looks like from the inside: the decrypt itself always
        // succeeds and hands back well-formed rubbish, and everything the classifier checks afterwards exists
        // to tell that rubbish from a secret somebody typed.
        private static string CbcBlobOfRawBytes(byte[] rawBytes, byte[] keyMaterial)
        {
            using var aes = Aes.Create();
            aes.Key = keyMaterial;

            var cipher = aes.EncryptCbc(rawBytes, FixedIv, PaddingMode.None);

            return Convert.ToBase64String([.. FixedIv, .. cipher]);
        }

        private static byte[] PaddedWith(byte paddingValue, int paddingByteCount)
        {
            var raw = new byte[2 * IvLength];

            Array.Fill(raw, (byte)'a', 0, raw.Length - paddingByteCount);
            Array.Fill(raw, paddingValue, raw.Length - paddingByteCount, paddingByteCount);

            return raw;
        }

        private static string WithDecodedByteFlipped(string stored, int fieldIndex)
        {
            var fields = stored.Split('.');
            var decoded = Base64Url.DecodeFromChars(fields[fieldIndex]);
            decoded[0] ^= 1;
            fields[fieldIndex] = Base64Url.EncodeToString(decoded);

            return string.Join('.', fields);
        }
    }
}
