using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // A key an operator put into a setting. Both spellings mean the same thing - a single key is a ring of
    // one - and the ring setting is read first, so an operator part-way through adding a second key is never
    // quietly put back on the one key they were leaving behind.
    public sealed class ConfiguredKeyRingSource
    {
        public const string RingSettingKey = "Encryption:Keys";

        public const string SingleKeySettingKey = "Encryption:Key";

        private readonly string? suppliedRing;

        private readonly string? suppliedKey;

        public ConfiguredKeyRingSource(string? suppliedRing, string? suppliedKey)
        {
            this.suppliedRing = suppliedRing;
            this.suppliedKey = suppliedKey;
        }

        public EncryptionKeyRing? Resolve()
        {
            if (!string.IsNullOrWhiteSpace(suppliedRing))
            {
                return SuppliedKeyRing.ParsedFrom(suppliedRing, KeyCustody.SuppliedByConfiguration, RingSettingKey);
            }

            if (!string.IsNullOrWhiteSpace(suppliedKey))
            {
                return SuppliedKeyRing.ParsedFrom(suppliedKey, KeyCustody.SuppliedByConfiguration, SingleKeySettingKey);
            }

            return null;
        }
    }
}
