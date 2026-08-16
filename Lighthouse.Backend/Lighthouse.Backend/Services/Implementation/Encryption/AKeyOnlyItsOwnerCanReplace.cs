using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // What an instance is given instead of something that makes keys, when the key it runs on was handed to
    // it. Refusing here rather than at the call site means the answer is decided once, where the key was
    // resolved, by the same fact the settings screen shows - so what the screen offers and what the request
    // will actually do cannot come apart.
    public sealed class AKeyOnlyItsOwnerCanReplace : IKeyRingMinter
    {
        private readonly KeyCustody custody;

        public AKeyOnlyItsOwnerCanReplace(KeyCustody custody)
        {
            this.custody = custody;
        }

        public EncryptionKeyRing MintOnto(EncryptionKeyRing existing)
        {
            throw new MintingNotPermittedException(custody);
        }
    }
}
