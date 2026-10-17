namespace Lighthouse.Backend.Models
{
    /// <summary>Where an owner's setting comes from, and the value it holds as its own.</summary>
    public readonly record struct StoredSetting(SettingSource Source, int? OwnValue);

    public static class IncomingSetting
    {
        // A client that sends a source means it. One that sends none may simply not know sources exist, so it
        // only changes anything when it sends a set value the owner does not already read; an empty value or
        // the value read back is an echo, not a choice. The compiler also wants an arm for an int cast to
        // SettingSource outside the enum; writing one would be a catch-all that hides a new source, so only
        // that is silenced.
#pragma warning disable CS8524
        public static StoredSetting AppliedTo(StoredSetting stored, int? effectiveBefore, SettingSource? sentSource, int? sentValue) => sentSource switch
        {
            SettingSource.Own => new StoredSetting(SettingSource.Own, sentValue),
            SettingSource.ConnectionDefault => new StoredSetting(SettingSource.ConnectionDefault, null),
            null when sentValue is not null && sentValue != effectiveBefore => new StoredSetting(SettingSource.Own, sentValue),
            null => stored,
        };
#pragma warning restore CS8524
    }
}
