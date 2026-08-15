using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using System.Buffers;
using System.Security.Cryptography;
using System.Text.Unicode;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Every question this class asks about a stored value is answered by looking at the value, never by
    // running something and seeing whether it blew up. That is the whole point: a decrypt that failed and
    // was quietly caught is what let a wrong key look like an expired token for years, so there is no
    // catch and no exception filter anywhere below, and any change that adds one has undone the fix. The
    // sole exception lives in SecretEnvelope, which has no other way to learn that a tag failed and is
    // careful to catch that one failure and nothing else.
    public sealed class SecretStateClassifier
    {
        private const int CbcBlockLength = 16;

        private const int CbcIvLength = 16;

        private const int SmallestPossibleCbcBlob = CbcIvLength + CbcBlockLength;

        private readonly IEncryptionKeyRingHolder keyRingHolder;

        public SecretStateClassifier(IEncryptionKeyRingHolder keyRingHolder)
        {
            ArgumentNullException.ThrowIfNull(keyRingHolder);

            this.keyRingHolder = keyRingHolder;
        }

        public SecretReadResult Classify(string? storedValue)
        {
            var ring = keyRingHolder.Current;

            if (SecretEnvelope.TryParse(storedValue, out var envelope))
            {
                return ClassifyEnvelope(envelope, ring);
            }

            if (TryDecodeCbcShaped(storedValue, out var blob))
            {
                return ClassifyCbcShaped(blob, ring);
            }

            return new SecretReadResult(SecretState.LegacyPlaintext, storedValue, null);
        }

        private static SecretReadResult ClassifyEnvelope(SecretEnvelope envelope, EncryptionKeyRing ring)
        {
            if (ring.TryGet(envelope.KeyId, out var key) && envelope.TryUnprotect(key, out var plainText))
            {
                return new SecretReadResult(SecretState.Envelope, plainText, envelope.KeyId);
            }

            return new SecretReadResult(SecretState.Unreadable, null, envelope.KeyId);
        }

        // A value shaped like a legacy blob that no key on the ring can read is unreadable, never plaintext.
        // Handing it back as though it had never been encrypted is precisely the silent failure being
        // removed, and it would come back the moment this returned LegacyPlaintext instead.
        private static SecretReadResult ClassifyCbcShaped(byte[] blob, EncryptionKeyRing ring)
        {
            var read = ring.RetiredKeys
                .Prepend(ring.ActiveKey)
                .Select(key => (key.Id, PlainText: ReadLegacyCbc(blob, key.Material)))
                .FirstOrDefault(candidate => candidate.PlainText is not null);

            return read.PlainText is null
                ? new SecretReadResult(SecretState.Unreadable, null, null)
                : new SecretReadResult(SecretState.LegacyCbc, read.PlainText, read.Id);
        }

        // Only a value that is standard base64 over at least an IV and one whole block can be something the
        // previous implementation wrote. Everything else is rejected here by its shape, which is what keeps
        // the plaintext answer a deliberate positive finding rather than a leftover.
        private static bool TryDecodeCbcShaped(string? storedValue, out byte[] blob)
        {
            blob = [];

            if (string.IsNullOrEmpty(storedValue))
            {
                return false;
            }

            var buffer = new byte[storedValue.Length / 4 * 3];

            if (!Convert.TryFromBase64String(storedValue, buffer, out var decodedLength)
                || decodedLength < SmallestPossibleCbcBlob
                || decodedLength % CbcBlockLength != 0)
            {
                return false;
            }

            blob = buffer.AsSpan(0, decodedLength).ToArray();
            return true;
        }

        private static string? ReadLegacyCbc(byte[] blob, ReadOnlyMemory<byte> keyMaterial)
        {
            using var aes = Aes.Create();
            aes.Key = keyMaterial.ToArray();

            // PaddingMode.None turns the unpadding into arithmetic on the last byte. Letting the framework
            // strip the padding would express a wrong key as a thrown exception, which this class may not
            // observe.
            var decrypted = aes.DecryptCbc(blob.AsSpan(CbcIvLength), blob.AsSpan(0, CbcIvLength), PaddingMode.None);

            return TryStripPkcs7(decrypted, out var payload) && TryReadPrintableUtf8(payload, out var plainText)
                ? plainText
                : null;
        }

        private static bool TryStripPkcs7(byte[] decrypted, out ReadOnlySpan<byte> payload)
        {
            payload = default;

            int padding = decrypted[^1];

            if (padding < 1 || padding > CbcBlockLength || padding > decrypted.Length)
            {
                return false;
            }

            if (decrypted.AsSpan(decrypted.Length - padding).ContainsAnyExcept((byte)padding))
            {
                return false;
            }

            payload = decrypted.AsSpan(0, decrypted.Length - padding);
            return true;
        }

        // Legacy CBC carries no authentication tag, so a wrong key that survives the padding check can only
        // be caught by what it produces. A secret is text; bytes that are not well-formed UTF-8, or that
        // decode to control characters, are the wrong key showing through.
        private static bool TryReadPrintableUtf8(ReadOnlySpan<byte> payload, out string plainText)
        {
            plainText = string.Empty;

            var characters = new char[payload.Length];

            if (Utf8.ToUtf16(payload, characters, out _, out var charactersWritten, replaceInvalidSequences: false) != OperationStatus.Done)
            {
                return false;
            }

            var decoded = new string(characters, 0, charactersWritten);

            if (decoded.Any(char.IsControl))
            {
                return false;
            }

            plainText = decoded;
            return true;
        }
    }
}
