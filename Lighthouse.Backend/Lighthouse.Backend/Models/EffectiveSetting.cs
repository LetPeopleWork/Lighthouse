namespace Lighthouse.Backend.Models
{
    public static class EffectiveSetting
    {
        // Every source is written out with no catch-all arm, so a third source cannot be added without
        // saying here what an owner reads under it. The compiler also wants an arm for an int cast to
        // SettingSource outside the enum; writing one would be that catch-all, so only that is silenced.
#pragma warning disable CS8524
        public static T Of<T>(SettingSource source, T ownValue, T connectionDefault) => source switch
        {
            SettingSource.ConnectionDefault => connectionDefault,
            SettingSource.Own => ownValue,
        };
#pragma warning restore CS8524
    }
}
