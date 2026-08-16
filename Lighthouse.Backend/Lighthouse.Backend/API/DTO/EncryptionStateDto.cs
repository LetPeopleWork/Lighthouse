using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;

namespace Lighthouse.Backend.API.DTO
{
    public sealed class EncryptionStateDto
    {
        public EncryptionStateDto(
            EncryptionKeyRing keyRing,
            string keyStorePath,
            int secretsUnderPublishedKey,
            bool allowsStartWithUnreadableSecrets = false,
            IReadOnlyCollection<string>? keysSomethingWasWrittenUnder = null,
            string? keySuppliedThrough = null)
        {
            ArgumentNullException.ThrowIfNull(keyRing);

            Custody = keyRing.Custody;
            CanMint = keyRing.CanMint;
            ActiveKeyId = keyRing.ActiveKey.Id;
            KeyIds = [
                keyRing.ActiveKey.Id,
                .. keyRing.RetiredKeys
                    .Select(retired => retired.Id)
                    .Where(id => keysSomethingWasWrittenUnder?.Contains(id) ?? true)];
            KeyStorePath = keyStorePath;
            LegacyDefaultPresent = keyRing.TryGet(LegacyDefaultEncryptionKey.Id, out _);
            SecretsUnderPublishedKey = secretsUnderPublishedKey;
            AllowsStartWithUnreadableSecrets = allowsStartWithUnreadableSecrets;
            KeySuppliedThrough = keySuppliedThrough;
        }

        public KeyCustody Custody { get; }

        // Whether Lighthouse could create a replacement key that would still be there after a restart. It
        // is read off where the key in force came from, never from a setting, so nothing an operator can
        // write down is able to contradict it.
        public bool CanMint { get; }

        public string ActiveKeyId { get; }

        // The key in force, and the earlier keys something is still stored under. A key nothing was ever
        // written under stays on the ring and is still read with - it is only kept off this list, because
        // every ring carries the key published with the product and a first install would otherwise
        // appear to be holding a key named after a legacy it never had. Nothing is removed, so a restore
        // that brings back older values makes its key appear here again on its own.
        public IReadOnlyList<string> KeyIds { get; }

        public string KeyStorePath { get; }

        // Whether the key published with the product is still one of the keys this instance can read with.
        // A ring that holds it is not a problem by itself - it is how an upgraded instance keeps reading
        // what it already stored.
        public bool LegacyDefaultPresent { get; }

        // How many stored credentials are still readable with that key, which is the number that decides
        // whether anything needs doing. It is a count of secrets, not of keys, and the two are separate
        // properties because an operator who confused them would either panic or relax for the wrong
        // reason.
        public int SecretsUnderPublishedKey { get; }

        // Whether this instance was told to start even though it cannot read a single credential it holds.
        // It is on the settings page and not only in a log because the operator who set it is usually not
        // the one who finds it still set months later, and a standalone install has no console to read.
        public bool AllowsStartWithUnreadableSecrets { get; }

        // The setting the key arrived in, or nothing where Lighthouse keeps the key itself. It decides
        // whether the panel may point at the key store as the thing to back up: under a supplied key that
        // directory exists and is full of key-shaped files, and none of them is the encryption key.
        public string? KeySuppliedThrough { get; }
    }
}
