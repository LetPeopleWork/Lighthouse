using Lighthouse.Backend.Models.Encryption;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    public static class LegacyDefaultEncryptionKey
    {
        public const string Id = "k-legacy-default";

        // This value is not a secret and never was. Every Lighthouse build before this release shipped it in
        // appsettings.json, so it is in every copy of the product and in the public source repository, and any
        // secret those builds stored is encrypted under it. It is compiled in here for one reason: so those
        // secrets stay readable after the upgrade. It is only ever added to a key ring behind whatever key is
        // already active, so nothing is ever encrypted under it again.
#pragma warning disable S6418
        private const string PublishedMaterial = "uH2VbF5hOW0/huLOH1Q2L0g+P3J9dG43cknQK7t9R5M=";
#pragma warning restore S6418

        private static readonly byte[] Material = Convert.FromBase64String(PublishedMaterial);

        // A ring holding this key and nothing else, so the question "can this one key read that value?"
        // can be asked of the same classifier everything else uses. Nothing is ever written through it.
        private static readonly SecretStateClassifier ReadingWithNothingElse =
            new(new EncryptionKeyRingHolder(new EncryptionKeyRing(new EncryptionKey(Id, Material))));

        public static EncryptionKeyRing AppendedTo(EncryptionKeyRing ring)
        {
            ArgumentNullException.ThrowIfNull(ring);

            return ring.WithRetired(new EncryptionKey(Id, Material));
        }

        // Whether some other key is this one, asked of every key an operator supplies. A key's name is
        // derived from its own bytes and so says nothing about whose bytes they are, which is how this
        // key walked past every check that looked at names.
        public static bool Matches(ReadOnlySpan<byte> material)
        {
            return CryptographicOperations.FixedTimeEquals(material, Material);
        }

        // Whether this key can read a stored value, which is the only honest way to answer whether a
        // credential is protected by something everybody already has. The shape of the value cannot
        // answer it: an install that set a key of its own before this release stores values that look
        // exactly like the ones written under this one. Answered by trying to read, and without the
        // material leaving here.
        public static bool CanRead(string? storedValue)
        {
            if (SecretEnvelope.TryParse(storedValue, out var envelope))
            {
                return envelope.TryUnprotect(new EncryptionKey(envelope.KeyId, Material), out _);
            }

            return ReadingWithNothingElse.Classify(storedValue).State == SecretState.LegacyCbc;
        }
    }
}
