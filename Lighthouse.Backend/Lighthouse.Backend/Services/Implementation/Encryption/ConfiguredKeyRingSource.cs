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
                && !string.IsNullOrWhiteSpace(suppliedUnderTheRetiredName)
                && !ThePublishedKeyUnderTheRetiredName(suppliedUnderTheRetiredName);
        }

        // Whether the value under the retired name is nothing but the key published with the product. That
        // is not a key anybody chose: every Lighthouse before this release shipped exactly this value in
        // appsettings.json, and the in-app updater keeps an operator settings file across an upgrade on
        // purpose, so an instance carrying it made no decision at all. It is therefore read as no key
        // rather than refused over, which sends the resolution on to the key this instance keeps for
        // itself - and the published key is appended behind whatever that turns out to be, so nothing
        // already stored becomes unreadable and nothing new is written under it.
        //
        // Only a value that is this key on its own. Anything longer is a ring somebody built by hand, and
        // putting the published key at the front of one is a choice worth stopping for.
        public static bool ThePublishedKeyUnderTheRetiredName(string? suppliedUnderTheRetiredName)
        {
            return KeyRingSerializer.TryParse(suppliedUnderTheRetiredName, out var parsed, out _)
                && parsed.RetiredKeys.Count == 0
                && LegacyDefaultEncryptionKey.Matches(parsed.ActiveKey.Material.Span);
        }

        // Which setting an operator would have to go and edit. Where Lighthouse keeps the key itself the
        // answer is nothing, and saying so is the point: the panel used to name the key store directory
        // in every custody, and under a supplied key that directory exists, is full of key-shaped files,
        // and does not contain the key. An operator who backed it up alongside the database had every
        // reason to believe they had taken their encryption key with them.
        public static string? SettingThatAnswered(string? suppliedRing, string? suppliedKey, string? suppliedUnderTheRetiredName)
        {
            if (!string.IsNullOrWhiteSpace(suppliedRing))
            {
                return AsAnOperatorWouldWriteIt(RingSettingKey);
            }

            if (!string.IsNullOrWhiteSpace(suppliedKey))
            {
                return AsAnOperatorWouldWriteIt(SingleKeySettingKey);
            }

            return string.IsNullOrWhiteSpace(suppliedUnderTheRetiredName)
                ? null
                : AsAnOperatorWouldWriteIt(RetiredSingleKeySettingKey);
        }

        // Settings are spelled with a colon inside the application and with two underscores everywhere an
        // operator types them, and it is the second spelling they have to recognise. It matters most in a
        // refusal: an instance that will not start names the setting it read the key from, and an operator
        // greps their compose file or manifest for that string. The colon spelling appears in neither.
        public static string AsAnOperatorWouldWriteIt(string settingKey)
        {
            ArgumentNullException.ThrowIfNull(settingKey);

            return settingKey.Replace(":", "__", StringComparison.Ordinal);
        }

        public EncryptionKeyRing? Resolve()
        {
            if (!string.IsNullOrWhiteSpace(suppliedRing))
            {
                return SuppliedKeyRing.ParsedFrom(
                    suppliedRing, KeyCustody.SuppliedByConfiguration, AsAnOperatorWouldWriteIt(RingSettingKey));
            }

            if (!string.IsNullOrWhiteSpace(suppliedKey))
            {
                return SuppliedKeyRing.ParsedFrom(
                    suppliedKey, KeyCustody.SuppliedByConfiguration, AsAnOperatorWouldWriteIt(SingleKeySettingKey));
            }

            if (!string.IsNullOrWhiteSpace(suppliedUnderTheRetiredName))
            {
                if (ThePublishedKeyUnderTheRetiredName(suppliedUnderTheRetiredName))
                {
                    return null;
                }

                return SuppliedKeyRing.ParsedFrom(
                    suppliedUnderTheRetiredName,
                    KeyCustody.SuppliedByConfiguration,
                    AsAnOperatorWouldWriteIt(RetiredSingleKeySettingKey));
            }

            return null;
        }
    }
}
