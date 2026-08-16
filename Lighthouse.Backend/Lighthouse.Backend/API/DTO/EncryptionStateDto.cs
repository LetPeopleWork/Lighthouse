using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;

namespace Lighthouse.Backend.API.DTO
{
    public sealed class EncryptionStateDto
    {
        public EncryptionStateDto(EncryptionKeyRing keyRing, string keyStorePath, int secretsUnderPublishedKey)
        {
            ArgumentNullException.ThrowIfNull(keyRing);

            Custody = keyRing.Custody;
            CanMint = keyRing.CanMint;
            ActiveKeyId = keyRing.ActiveKey.Id;
            KeyIds = [keyRing.ActiveKey.Id, .. keyRing.RetiredKeys.Select(retired => retired.Id)];
            KeyStorePath = keyStorePath;
            LegacyDefaultPresent = keyRing.TryGet(LegacyDefaultEncryptionKey.Id, out _);
            SecretsUnderPublishedKey = secretsUnderPublishedKey;
        }

        public KeyCustody Custody { get; }

        // Whether Lighthouse could create a replacement key that would still be there after a restart. It
        // is read off where the key in force came from, never from a setting, so nothing an operator can
        // write down is able to contradict it.
        public bool CanMint { get; }

        public string ActiveKeyId { get; }

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
    }
}
