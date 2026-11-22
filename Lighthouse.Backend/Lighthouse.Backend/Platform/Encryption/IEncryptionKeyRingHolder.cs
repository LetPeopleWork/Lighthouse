using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Platform.Encryption
{
    public interface IEncryptionKeyRingHolder
    {
        EncryptionKeyRing Current { get; }

        void Replace(EncryptionKeyRing ring);
    }
}
