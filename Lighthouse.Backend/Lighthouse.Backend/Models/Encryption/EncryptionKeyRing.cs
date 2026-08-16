using Lighthouse.Backend.Services.Implementation.Encryption;
using System.Diagnostics.CodeAnalysis;

namespace Lighthouse.Backend.Models.Encryption
{
    // Position is state: the first entry is the key new secrets are written under, every later entry only
    // ever reads older ones. A ring with two keys to write under, or with none, cannot be spelled at all.
    public sealed class EncryptionKeyRing : IEquatable<EncryptionKeyRing>
    {
        private readonly EncryptionKey[] keys;

        public EncryptionKeyRing(params EncryptionKey[] keys)
            : this(KeyCustody.NoDurableStore, keys)
        {
        }

        public EncryptionKeyRing(KeyCustody custody, params EncryptionKey[] keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            if (keys.Length == 0)
            {
                throw new ArgumentException("An encryption key ring must hold at least one key, because its first entry is the key secrets are written under.", nameof(keys));
            }

            var repeatedId = RepeatedKeyIdDefect(keys);

            if (repeatedId is not null)
            {
                throw new ArgumentException(repeatedId, nameof(keys));
            }

            Custody = custody;
            this.keys = [.. keys];
        }

        public KeyCustody Custody { get; }

        public bool CanMint => Custody == KeyCustody.GeneratedForThisInstance;

        public EncryptionKey ActiveKey => keys[0];

        public IReadOnlyList<EncryptionKey> RetiredKeys => keys[1..];

        // Appending is the only way this type grows, and it appends to the end, so nothing added after a ring
        // exists can become the key secrets are written under.
        public EncryptionKeyRing WithRetired(EncryptionKey key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (Array.Exists(keys, present => present.Equals(key)))
            {
                return this;
            }

            return new EncryptionKeyRing(Custody, [.. keys, key]);
        }

        // Removing a key is only ever safe once nothing stored is still under it, which is a question about
        // the data rather than about the ring - so the caller answers it and this only refuses the one case
        // that is never safe: taking away the key secrets are being written under.
        public EncryptionKeyRing Without(string keyId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

            if (string.Equals(ActiveKey.Id, keyId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The key secrets are being written under cannot be taken off the ring, because nothing written under it could be read afterwards.",
                    nameof(keyId));
            }

            var kept = Array.FindAll(keys, key => !string.Equals(key.Id, keyId, StringComparison.Ordinal));

            return kept.Length == keys.Length ? this : new EncryptionKeyRing(Custody, kept);
        }

        public EncryptionKeyRing WithLegacyDefault()
        {
            return LegacyDefaultEncryptionKey.AppendedTo(this);
        }

        public bool TryGet(string keyId, [NotNullWhen(true)] out EncryptionKey? key)
        {
            key = Array.Find(keys, candidate => string.Equals(candidate.Id, keyId, StringComparison.Ordinal));

            return key is not null;
        }

        // Both the rule and the sentence live here, because the two places that enforce it refuse in
        // different ways - one throws, the other hands the sentence back to be quoted - and spelling the
        // same refusal twice is how they come to disagree about what is wrong with a ring.
        internal static string? RepeatedKeyIdDefect(EncryptionKey[] keys)
        {
            var seen = new HashSet<string>(keys.Length, StringComparer.Ordinal);
            var repeated = Array.Find(keys, key => !seen.Add(key.Id))?.Id;

            return repeated is null
                ? null
                : $"The encryption key ring names '{repeated}' more than once, so a secret written under it could not be attributed to one key.";
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
