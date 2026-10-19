namespace Lighthouse.Backend.Models
{
    /// <summary>Where an owner's setting comes from, and the value it holds as its own.</summary>
    public readonly record struct StoredSetting(SettingSource Source, int? OwnValue)
    {
        public static readonly StoredSetting FollowingTheDefault = new(SettingSource.ConnectionDefault, null);

        /// <param name="value">Null is a deliberate None, whatever the connection's default says.</param>
        public static StoredSetting Owning(int? value) => new(SettingSource.Own, value);

        public bool FollowsTheDefault => Source == SettingSource.ConnectionDefault;

        public int? ReadWith(int? connectionDefault) => EffectiveSetting.Of(Source, OwnValue, connectionDefault);

        public StoredSetting Taking(SettingSource? sentSource, int? sentValue, int? connectionDefault)
            => IncomingSetting.AppliedTo(this, ReadWith(connectionDefault), sentSource, sentValue);

        /// <summary>The value the owner would hold as its own once the sent setting is taken; null when it would follow, or choose None.</summary>
        public int? OwnedOnceTaking(SettingSource? sentSource, int? sentValue, int? connectionDefault)
            => Taking(sentSource, sentValue, connectionDefault) is { Source: SettingSource.Own } taken ? taken.OwnValue : null;
    }

    public static class IncomingSetting
    {
        /// <summary>The source a save chooses for an owner's setting; null when the save leaves the setting as it was.</summary>
        /// <param name="sentIsSet">Whether the sent value names anything, as against an empty value.</param>
        // A client that sends a source means it. One that sends none may simply not know sources exist, so it
        // only changes anything when it sends a set value the owner does not already read; an empty value or
        // the value read back is an echo, not a choice.
        public static SettingSource? SourceChosen<T>(SettingSource? sentSource, T sentValue, bool sentIsSet, T effectiveBefore) => sentSource switch
        {
            { } source => source,
            null when sentIsSet && !EqualityComparer<T>.Default.Equals(sentValue, effectiveBefore) => SettingSource.Own,
            null => null,
        };

        // The compiler also wants an arm for an int cast to SettingSource outside the enum; writing one would be
        // a catch-all that hides a new source, so only that is silenced.
#pragma warning disable CS8524
        public static StoredSetting AppliedTo(StoredSetting stored, int? effectiveBefore, SettingSource? sentSource, int? sentValue)
            => SourceChosen(sentSource, sentValue, sentValue is not null, effectiveBefore) switch
            {
                SettingSource.Own => StoredSetting.Owning(sentValue),
                SettingSource.ConnectionDefault => StoredSetting.FollowingTheDefault,
                null => stored,
            };
#pragma warning restore CS8524
    }
}
