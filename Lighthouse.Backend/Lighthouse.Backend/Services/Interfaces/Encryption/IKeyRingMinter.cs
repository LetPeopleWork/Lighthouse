using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Interfaces.Encryption
{
    public interface IKeyRingMinter
    {
        // Returns the ring the new key is in front of, and only once that key has been written and read
        // back, because a key this machine cannot keep would take every secret moved onto it with it.
        EncryptionKeyRing MintOnto(EncryptionKeyRing existing);
    }
}
