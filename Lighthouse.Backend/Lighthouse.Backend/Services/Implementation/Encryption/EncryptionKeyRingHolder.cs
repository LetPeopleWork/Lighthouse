using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    public sealed class EncryptionKeyRingHolder : IEncryptionKeyRingHolder
    {
        private EncryptionKeyRing current;

        public EncryptionKeyRingHolder(EncryptionKeyRing ring)
        {
            ArgumentNullException.ThrowIfNull(ring);

            current = ring;
        }

        // The ring is immutable, so whoever reads this once at the top of an operation keeps a whole,
        // consistent set of keys even if the keys in use change while that operation is still running.
        public EncryptionKeyRing Current => Volatile.Read(ref current);

        public void Replace(EncryptionKeyRing ring)
        {
            ArgumentNullException.ThrowIfNull(ring);

            Volatile.Write(ref current, ring);
        }
    }
}
