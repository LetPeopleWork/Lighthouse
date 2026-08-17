using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Where an operator put a key, and which of those places the instance is actually running on. The two
    // travel together because neither is worth having alone: a list of settings says nothing without the
    // one that won, and the one that won says nothing about which others are sitting there doing nothing.
    public sealed record KeySupply(IReadOnlyList<string> Settings, string? TheOneInForce)
    {
        public bool InMoreThanOnePlace => Settings.Count > 1 && !string.IsNullOrWhiteSpace(TheOneInForce);
    }

    // Which of the places an operator can put a key is the one the instance is actually running on, and
    // which others are carrying a key that is being ignored.
    //
    // Both questions are answered from the resolved ring first and from configuration second. Asking
    // configuration on its own would name a setting on an instance running on a key it made for itself -
    // a value can sit in a setting without having won - and that sends an operator to edit something
    // that is not in force.
    public static class WhereTheKeyCameFrom
    {
        public static KeySupply Resolve(
            KeyCustody custody, string? suppliedRing, string? suppliedKey, string? suppliedUnderTheRetiredName, string? keysFilePath)
        {
            return new KeySupply(
                EverySettingCarryingAKey(suppliedRing, suppliedKey, suppliedUnderTheRetiredName, keysFilePath),
                SettingThatAnswered(custody, suppliedRing, suppliedKey, suppliedUnderTheRetiredName, keysFilePath));
        }

        // The settings are asked in the order the resolution asks them, so an instance with several set is
        // told about the one that answered rather than the first one somebody thought of.
        public static string? SettingThatAnswered(
            KeyCustody custody, string? suppliedRing, string? suppliedKey, string? suppliedUnderTheRetiredName, string? keysFilePath)
        {
            if (custody is not (KeyCustody.SuppliedByConfiguration or KeyCustody.SuppliedByExternalSecret))
            {
                return null;
            }

            if (custody == KeyCustody.SuppliedByConfiguration)
            {
                return ConfiguredKeyRingSource.SettingThatAnswered(
                    suppliedRing, suppliedKey, suppliedUnderTheRetiredName);
            }

            return string.IsNullOrWhiteSpace(keysFilePath)
                ? null
                : ConfiguredKeyRingSource.AsAnOperatorWouldWriteIt(MountedFileKeyRingSource.PathSettingKey);
        }

        // Every place carrying a key, whether it won or not, in the order the resolution reads them. Only
        // ever used to tell an operator that more than one is set, so it names settings and never values.
        public static IReadOnlyList<string> EverySettingCarryingAKey(
            string? suppliedRing, string? suppliedKey, string? suppliedUnderTheRetiredName, string? keysFilePath)
        {
            var carrying = new List<string>(4);

            AddWhenSet(carrying, suppliedRing, ConfiguredKeyRingSource.RingSettingKey);
            AddWhenSet(carrying, suppliedKey, ConfiguredKeyRingSource.SingleKeySettingKey);
            AddWhenSet(carrying, suppliedUnderTheRetiredName, ConfiguredKeyRingSource.RetiredSingleKeySettingKey);
            AddWhenSet(carrying, keysFilePath, MountedFileKeyRingSource.PathSettingKey);

            return carrying;
        }

        private static void AddWhenSet(List<string> carrying, string? value, string settingKey)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                carrying.Add(ConfiguredKeyRingSource.AsAnOperatorWouldWriteIt(settingKey));
            }
        }
    }
}
