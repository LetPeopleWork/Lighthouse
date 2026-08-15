using Lighthouse.Backend.Models.Encryption;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // A whole key ring is one line of text, and its order is the only thing that says which key is used to
    // write: the first entry is it, every later entry only ever reads what was written before. There is
    // nowhere in this spelling to say "this one is active", so a ring with two of them, or with none,
    // cannot be written down at all.
    public static class KeyRingSerializer
    {
        private const string ConfiguredKeyIdPrefix = "k-cfg-";

        private const int ConfiguredKeyIdFingerprintBytes = 4;

        private const char EntrySeparator = ',';

        private const char KeyIdSeparator = ':';

        private const int MaxKeyIdLength = 32;

        public static bool TryParse(string? value, [NotNullWhen(true)] out EncryptionKeyRing? ring, [NotNullWhen(false)] out string? defect)
        {
            ring = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                defect = "No encryption key was supplied.";
                return false;
            }

            var entries = value.Split(EntrySeparator);
            var keys = new EncryptionKey[entries.Length];

            for (var index = 0; index < entries.Length; index++)
            {
                if (!TryParseEntry(entries[index].Trim(), index + 1, out var key, out defect))
                {
                    return false;
                }

                keys[index] = key;
            }

            var repeatedId = FirstRepeatedId(keys);

            if (repeatedId is not null)
            {
                defect = $"The encryption key ring names '{repeatedId}' more than once, so a secret written under it could not be attributed to one key.";
                return false;
            }

            ring = new EncryptionKeyRing(keys);
            defect = null;

            return true;
        }

        public static string Format(EncryptionKeyRing ring)
        {
            ArgumentNullException.ThrowIfNull(ring);

            var entries = new List<string>(ring.RetiredKeys.Count + 1) { FormatEntry(ring.ActiveKey) };

            foreach (var retired in ring.RetiredKeys)
            {
                entries.Add(FormatEntry(retired));
            }

            return string.Join(EntrySeparator, entries);
        }

        // The name is a fingerprint of the key itself rather than something fresh, so two instances sharing
        // one supplied key, and the same instance after a restart, label their stored secrets identically.
        // A random name would leave everything written before a restart unattributable after it.
        public static string DeriveConfiguredKeyId(ReadOnlySpan<byte> material)
        {
            Span<byte> fingerprint = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(material, fingerprint);

            return string.Concat(ConfiguredKeyIdPrefix, Convert.ToHexStringLower(fingerprint[..ConfiguredKeyIdFingerprintBytes]));
        }

        // Every refusal names the entry at fault and says what is wrong with it, and none of them repeats a
        // character of what was supplied: a startup failure is read from a console or a log that keeps it.
        private static bool TryParseEntry(string entry, int position, [NotNullWhen(true)] out EncryptionKey? key, [NotNullWhen(false)] out string? defect)
        {
            key = null;

            if (entry.Length == 0)
            {
                defect = $"Entry {position} of the encryption key ring is empty.";
                return false;
            }

            var separator = entry.IndexOf(KeyIdSeparator);
            var keyId = separator < 0 ? null : entry[..separator].Trim();
            var encoded = separator < 0 ? entry : entry[(separator + 1)..].Trim();

            if (keyId is not null && !IsUsableKeyId(keyId))
            {
                defect = $"Entry {position} of the encryption key ring is named '{keyId}', which is not allowed: a key name may only use lowercase letters, digits and hyphens, and may be at most {MaxKeyIdLength} characters long.";
                return false;
            }

            var named = keyId is null ? string.Empty : $" ('{keyId}')";

            if (encoded.Length == 0)
            {
                defect = $"Entry {position}{named} of the encryption key ring supplies no key material under a name that was set.";
                return false;
            }

            var decoded = new byte[(encoded.Length / 4 * 3) + 3];

            if (!Convert.TryFromBase64String(encoded, decoded, out var decodedLength))
            {
                defect = $"Entry {position}{named} of the encryption key ring could not be decoded as base64 key material.";
                return false;
            }

            if (decodedLength != EncryptionKey.MaterialLength)
            {
                defect = $"Entry {position}{named} of the encryption key ring carries {decodedLength} bytes of key material, but must carry exactly {EncryptionKey.MaterialLength}.";
                return false;
            }

            var material = decoded.AsSpan(0, decodedLength);

            key = new EncryptionKey(keyId ?? DeriveConfiguredKeyId(material), material);
            defect = null;

            return true;
        }

        private static bool IsUsableKeyId(string keyId)
        {
            return keyId.Length is > 0 and <= MaxKeyIdLength && keyId.All(IsUsableKeyIdCharacter);
        }

        private static bool IsUsableKeyIdCharacter(char character)
        {
            return character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-';
        }

        private static string? FirstRepeatedId(EncryptionKey[] keys)
        {
            var seen = new HashSet<string>(keys.Length, StringComparer.Ordinal);

            return Array.Find(keys, key => !seen.Add(key.Id))?.Id;
        }

        private static string FormatEntry(EncryptionKey key)
        {
            return $"{key.Id}{KeyIdSeparator}{Convert.ToBase64String(key.Material.Span)}";
        }
    }
}
