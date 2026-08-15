using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Interfaces.Encryption
{
    public interface IEncryptionKeyRingHolder
    {
        EncryptionKeyRing Current { get; }

        void Replace(EncryptionKeyRing ring);
    }
}
