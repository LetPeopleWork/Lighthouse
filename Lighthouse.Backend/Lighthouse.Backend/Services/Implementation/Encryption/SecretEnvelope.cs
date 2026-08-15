using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    public sealed class SecretEnvelope
    {
        public const string VersionToken = "LH1";

        public const string Prefix = VersionToken + ".";

        private const char FieldSeparator = '.';

        private const int FieldCount = 4;

        private const int KeyIdFieldIndex = 1;

        private const int NonceFieldIndex = 2;

        private const int CiphertextFieldIndex = 3;

        private const int NonceLength = 12;

        private const int TagLength = 16;

        private const int MaxKeyIdLength = 32;

        private readonly byte[] nonce;

        private readonly byte[] ciphertextAndTag;

        private SecretEnvelope(string keyId, byte[] nonce, byte[] ciphertextAndTag)
        {
            KeyId = keyId;
            this.nonce = nonce;
            this.ciphertextAndTag = ciphertextAndTag;
        }

        public string KeyId { get; }

        public ReadOnlyMemory<byte> Nonce => nonce;

        public ReadOnlyMemory<byte> CiphertextAndTag => ciphertextAndTag;

        public static SecretEnvelope Protect(string plainText, string keyId, ReadOnlySpan<byte> key)
        {
            ArgumentNullException.ThrowIfNull(plainText);

            if (!IsValidKeyId(keyId))
            {
                throw new ArgumentException($"A key id must be 1 to {MaxKeyIdLength} characters drawn from a-z, 0-9 and '-'.", nameof(keyId));
            }

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var nonce = RandomNumberGenerator.GetBytes(NonceLength);
            var ciphertextAndTag = new byte[plainBytes.Length + TagLength];

            using var aes = new AesGcm(key, TagLength);
            aes.Encrypt(
                nonce,
                plainBytes,
                ciphertextAndTag.AsSpan(0, plainBytes.Length),
                ciphertextAndTag.AsSpan(plainBytes.Length, TagLength),
                Header(keyId));

            return new SecretEnvelope(keyId, nonce, ciphertextAndTag);
        }

        public static bool TryParse(string? value, [NotNullWhen(true)] out SecretEnvelope? envelope)
        {
            envelope = null;

            if (value is null || !value.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var fields = value.Split(FieldSeparator);

            if (fields.Length != FieldCount || !IsValidKeyId(fields[KeyIdFieldIndex]))
            {
                return false;
            }

            if (!TryDecode(fields[NonceFieldIndex], out var nonce) || nonce.Length != NonceLength)
            {
                return false;
            }

            if (!TryDecode(fields[CiphertextFieldIndex], out var ciphertextAndTag) || ciphertextAndTag.Length < TagLength)
            {
                return false;
            }

            envelope = new SecretEnvelope(fields[KeyIdFieldIndex], nonce, ciphertextAndTag);
            return true;
        }

        public string Format()
        {
            return string.Join(
                FieldSeparator,
                VersionToken,
                KeyId,
                Base64Url.EncodeToString(nonce),
                Base64Url.EncodeToString(ciphertextAndTag));
        }

        public string Unprotect(ReadOnlySpan<byte> key)
        {
            var ciphertextLength = ciphertextAndTag.Length - TagLength;
            var plainBytes = new byte[ciphertextLength];

            using var aes = new AesGcm(key, TagLength);
            aes.Decrypt(
                nonce,
                ciphertextAndTag.AsSpan(0, ciphertextLength),
                ciphertextAndTag.AsSpan(ciphertextLength, TagLength),
                plainBytes,
                Header(KeyId));

            return Encoding.UTF8.GetString(plainBytes);
        }

        // The version and the key id authenticate the ciphertext without being encrypted by it, so a stored
        // value relabelled with another key's name fails its tag instead of being read under that key.
        private static byte[] Header(string keyId)
        {
            return Encoding.UTF8.GetBytes(string.Concat(Prefix, keyId));
        }

        private static bool TryDecode(string field, out byte[] decoded)
        {
            decoded = [];

            if (field.Length == 0 || !IsBase64Url(field))
            {
                return false;
            }

            var buffer = new byte[Base64Url.GetMaxDecodedLength(field.Length)];

            if (!Base64Url.TryDecodeFromChars(field, buffer, out var decodedLength))
            {
                return false;
            }

            decoded = buffer.AsSpan(0, decodedLength).ToArray();
            return true;
        }

        private static bool IsValidKeyId(string keyId)
        {
            return !string.IsNullOrEmpty(keyId)
                && keyId.Length <= MaxKeyIdLength
                && keyId.All(character => character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-');
        }

        private static bool IsBase64Url(string field)
        {
            return field.All(character => character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_');
        }
    }
}
