using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Interfaces.Encryption
{
    public interface IKeyRingMinter
    {
        // Returns the ring the new key is in front of, and only once that key has been written and read
        // back, because a key this machine cannot keep would take every secret moved onto it with it.
        // The returned ring never holds the key published with the product: that one is compiled in and put
        // on the end again at every start, so writing it down here would copy a key that is in every copy of
        // Lighthouse into this instance's own key store.
        // An instance whose key was handed to it is given something that refuses instead of something that
        // works, so the refusal is decided where the key was resolved rather than at the call site.
        EncryptionKeyRing MintOnto(EncryptionKeyRing existing);
    }
}
