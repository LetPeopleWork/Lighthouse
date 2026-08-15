namespace Lighthouse.Backend.Models.Encryption
{
    public sealed class EncryptionKey : IEquatable<EncryptionKey>
    {
        public const int MaterialLength = 32;

        private readonly byte[] material;

        public EncryptionKey(string id, ReadOnlySpan<byte> material)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            // The message names the entry and its length and stops there: anything more specific would put
            // the key itself into whatever log or console the startup failure is read from.
            if (material.Length != MaterialLength)
            {
                throw new ArgumentException($"The encryption key '{id}' must carry exactly {MaterialLength} bytes of key material, but carries {material.Length}.", nameof(material));
            }

            Id = id;
            this.material = material.ToArray();
        }

        public string Id { get; }

        public ReadOnlyMemory<byte> Material => material;

        public bool Equals(EncryptionKey? other)
        {
            return other is not null
                && string.Equals(Id, other.Id, StringComparison.Ordinal)
                && material.AsSpan().SequenceEqual(other.material);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EncryptionKey);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Id, StringComparer.Ordinal);
            hash.AddBytes(material);

            return hash.ToHashCode();
        }
    }
}
