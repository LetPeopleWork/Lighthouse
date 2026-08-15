using System.Diagnostics.CodeAnalysis;

namespace Lighthouse.Backend.Models.Encryption
{
    // Position is state: the first entry is the key new secrets are written under, every later entry only
    // ever reads older ones. A ring with two keys to write under, or with none, cannot be spelled at all.
    public sealed class EncryptionKeyRing : IEquatable<EncryptionKeyRing>
    {
        private readonly EncryptionKey[] keys;

        public EncryptionKeyRing(params EncryptionKey[] keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            if (keys.Length == 0)
            {
                throw new ArgumentException("An encryption key ring must hold at least one key, because its first entry is the key secrets are written under.", nameof(keys));
            }

            var duplicateId = keys
                .GroupBy(key => key.Id, StringComparer.Ordinal)
                .FirstOrDefault(namesake => namesake.Count() > 1)?.Key;

            if (duplicateId is not null)
            {
                throw new ArgumentException($"The encryption key ring names '{duplicateId}' more than once, so a secret written under it could not be attributed to one key.", nameof(keys));
            }

            this.keys = [.. keys];
        }

        public EncryptionKey ActiveKey => keys[0];

        public IReadOnlyList<EncryptionKey> RetiredKeys => keys[1..];

        public bool TryGet(string keyId, [NotNullWhen(true)] out EncryptionKey? key)
        {
            key = Array.Find(keys, candidate => string.Equals(candidate.Id, keyId, StringComparison.Ordinal));

            return key is not null;
        }

        public bool Equals(EncryptionKeyRing? other)
        {
            return other is not null && keys.AsSpan().SequenceEqual(other.keys);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EncryptionKeyRing);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var key in keys)
            {
                hash.Add(key);
            }

            return hash.ToHashCode();
        }
    }
}
