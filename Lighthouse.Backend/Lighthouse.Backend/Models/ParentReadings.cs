namespace Lighthouse.Backend.Models
{
    public enum ParentReadingKind
    {
        NoValue,
        FieldValue,
        LinkCandidates,
        TrackersOwn,
        CannotBeRead,
    }

    /// <summary>What a connector found at one position of a parent source list, before anything is decided.</summary>
    public sealed record ParentReading(ParentReadingKind Kind, string Key, ParentResolution Links)
    {
        public static ParentReading NoValue { get; } = new(ParentReadingKind.NoValue, string.Empty, default);

        public static ParentReading CannotBeRead { get; } = new(ParentReadingKind.CannotBeRead, string.Empty, default);

        public static ParentReading FieldValue(string key) => new(ParentReadingKind.FieldValue, key, default);

        public static ParentReading TrackersOwn(string key) => new(ParentReadingKind.TrackersOwn, key, default);

        public static ParentReading LinkCandidates(ParentResolution links) => new(ParentReadingKind.LinkCandidates, string.Empty, links);
    }

    public sealed record ParentReadingAt(ParentSourceEntry Source, ParentReading Reading);

    /// <summary>One reading per list position, in the list's order.</summary>
    public sealed record ParentReadings(IReadOnlyList<ParentReadingAt> Positions)
    {
        public static ParentReadings Of(params ParentReadingAt[] positions) => new(positions);
    }
}
