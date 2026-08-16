using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Makes a key where the application keeps its own, and nowhere else. Anywhere the key was handed to
    // this instance, a minted key would be written to a place the supplied one wins over again on the next
    // start, and everything moved onto it would be out of reach - so nothing constructs this at all in
    // those deployments and the refusal is decided from where the key in force came from.
    public sealed class GeneratedKeyRingMinter : IKeyRingMinter, IDisposable
    {
        private readonly ServiceProvider keyStoreProtection;

        private readonly GeneratedKeyRingStore store;

        public GeneratedKeyRingMinter(string keyStoreDirectory, IKeyStoreFileSystem fileSystem, TimeProvider timeProvider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyStoreDirectory);

            keyStoreProtection = GeneratedKeyRingStore.ProtectionKeptBesideTheKeyStore(keyStoreDirectory);

            store = new GeneratedKeyRingStore(
                keyStoreDirectory,
                keyStoreProtection.GetRequiredService<IDataProtectionProvider>(),
                fileSystem,
                timeProvider);
        }

        // The key published with the product is compiled in and put on the end of every ring at startup, so
        // it is taken off before the ring is written down. Writing it into the file would put a key that is
        // in every copy of Lighthouse, and in the public source, into this instance's own key store.
        public EncryptionKeyRing MintOnto(EncryptionKeyRing existing)
        {
            ArgumentNullException.ThrowIfNull(existing);

            return store.MintOnto(existing.Without(LegacyDefaultEncryptionKey.Id));
        }

        public void Dispose()
        {
            keyStoreProtection.Dispose();
        }
    }
}
