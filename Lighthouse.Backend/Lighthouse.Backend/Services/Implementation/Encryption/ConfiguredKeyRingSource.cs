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

        // The name every Lighthouse before this release read its key from. It is read last and is no longer
        // documented, because an instance that upgrades while relying on it would otherwise mint a key of its
        // own and leave every secret the operator stored under their key unreadable - and nothing about that
        // start would say so. Honouring the old name keeps those instances working; the banner asks them to
        // move to Encryption__Key, which is what makes it safe to eventually stop reading this.
        public const string RetiredSingleKeySettingKey = "EncryptionSettings:EncryptionKey";

        private readonly string? suppliedRing;

        private readonly string? suppliedKey;

        private readonly string? suppliedUnderTheRetiredName;

        public ConfiguredKeyRingSource(string? suppliedRing, string? suppliedKey, string? suppliedUnderTheRetiredName)
        {
            this.suppliedRing = suppliedRing;
            this.suppliedKey = suppliedKey;
            this.suppliedUnderTheRetiredName = suppliedUnderTheRetiredName;
        }

        // Whether the key in force came from the name this release retired, so the one place that says so out
        // loud does not have to re-read configuration to find out.
        public static bool AnsweredByTheRetiredName(string? suppliedRing, string? suppliedKey, string? suppliedUnderTheRetiredName)
        {
            return string.IsNullOrWhiteSpace(suppliedRing)
                && string.IsNullOrWhiteSpace(suppliedKey)
                && !string.IsNullOrWhiteSpace(suppliedUnderTheRetiredName);
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

            if (!string.IsNullOrWhiteSpace(suppliedUnderTheRetiredName))
            {
                return SuppliedKeyRing.ParsedFrom(
                    suppliedUnderTheRetiredName, KeyCustody.SuppliedByConfiguration, RetiredSingleKeySettingKey);
            }

            return null;
        }
    }
}
