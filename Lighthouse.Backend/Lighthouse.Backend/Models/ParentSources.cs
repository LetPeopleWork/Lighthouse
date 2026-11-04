namespace Lighthouse.Backend.Models
{
    public enum ParentSourcesViolation
    {
        Empty,
        TooMany,
        Duplicate,
        FieldWithoutAnId,
    }

    /// <summary>The list, or why it cannot be kept. Exactly one of the two is filled.</summary>
    public sealed record ParentSourcesOutcome(ParentSources? List, IReadOnlyList<ParentSourcesViolation> Violations);

    /// <summary>
    /// The ordered list of places a Team or a Portfolio reads its parent from: one to five entries, none twice.
    /// </summary>
    // SCAFFOLD: Story 6501. Every member below throws until the value object is built.
    public sealed class ParentSources
    {
        public const int Maximum = 5;

        public IReadOnlyList<ParentSourceEntry> Entries
            => throw UnbuiltOnThisList(nameof(Entries));

        public static ParentSourcesOutcome Of(IEnumerable<ParentSourceEntry> entries)
            => throw Unbuilt(nameof(Of));

        /// <summary>The text the list is stored as, and rendered with, byte for byte.</summary>
        public string ToStoredText()
            => throw UnbuiltOnThisList(nameof(ToStoredText));

        /// <summary>Reads stored text back. Text the factory refuses throws instead of being repaired.</summary>
        public static ParentSources FromStoredText(string storedText)
            => throw Unbuilt(nameof(FromStoredText));

        /// <summary>
        /// What a build one release back can read of this list: the id of its first Additional Field entry, or
        /// nothing when it holds none.
        /// </summary>
        public int? ToTheLegacyField()
            => throw UnbuiltOnThisList(nameof(ToTheLegacyField));

        private NotImplementedException UnbuiltOnThisList(string member)
            => Unbuilt($"{member} on {GetType().Name}");

        private static NotImplementedException Unbuilt(string member)
            => new($"SCAFFOLD: Story 6501, ParentSources.{member} is not built yet.");
    }
}
