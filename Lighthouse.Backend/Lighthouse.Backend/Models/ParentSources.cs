using System.Text.Json;

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
    public sealed class ParentSources
    {
        public const int Maximum = 5;

        // The entry's constructor parameters count as required here, so a stored entry that lost a member is
        // refused. Left to the default options it would silently read as the tracker's own parent.
        private static readonly JsonSerializerOptions StoredTextReading = new() { RespectRequiredConstructorParameters = true };

        private ParentSources(List<ParentSourceEntry> entries)
        {
            Entries = entries.AsReadOnly();
        }

        /// <summary>The list that reads exactly what the tracker itself calls the parent.</summary>
        public static ParentSources OnlyTheTrackersOwnParent { get; } = new([ParentSourceEntry.TrackerParent]);

        public IReadOnlyList<ParentSourceEntry> Entries { get; }

        public static ParentSourcesOutcome Of(IEnumerable<ParentSourceEntry> entries)
        {
            var candidates = entries.ToList();
            var violations = ViolationsOf(candidates).ToList();

            return violations.Count == 0
                ? new ParentSourcesOutcome(new ParentSources(candidates), [])
                : new ParentSourcesOutcome(null, violations);
        }

        /// <summary>The text the list is stored as, and rendered with, byte for byte.</summary>
        public string ToStoredText() => JsonSerializer.Serialize(Entries, (JsonSerializerOptions?)null);

        /// <summary>Reads stored text back. Text the factory refuses throws instead of being repaired.</summary>
        public static ParentSources FromStoredText(string storedText)
        {
            var entries = JsonSerializer.Deserialize<List<ParentSourceEntry>>(storedText, StoredTextReading);
            var outcome = Of(entries ?? []);

            return outcome.List ?? throw new FormatException(
                $"The stored parent sources '{storedText}' cannot be kept: {string.Join(", ", outcome.Violations)}.");
        }

        /// <summary>
        /// What a build one release back can read of this list: the id of its first Additional Field entry, or
        /// nothing when it holds none.
        /// </summary>
        public int? ToTheLegacyField()
            => Entries.FirstOrDefault(entry => entry.Kind == ParentSourceKind.AdditionalField)?.AdditionalFieldDefinitionId;

        /// <summary>What a single old field means as a list: that field then the tracker's own parent, or the own parent alone.</summary>
        public static ParentSources FromTheLegacyField(int? additionalFieldDefinitionId)
            => additionalFieldDefinitionId is { } field
                ? new([ParentSourceEntry.Field(field), ParentSourceEntry.TrackerParent])
                : OnlyTheTrackersOwnParent;

        private static IEnumerable<ParentSourcesViolation> ViolationsOf(List<ParentSourceEntry> entries)
        {
            if (entries.Count == 0)
            {
                yield return ParentSourcesViolation.Empty;
            }

            if (entries.Count > Maximum)
            {
                yield return ParentSourcesViolation.TooMany;
            }

            if (entries.Distinct().Count() < entries.Count)
            {
                yield return ParentSourcesViolation.Duplicate;
            }

            // An entry itself cannot lack its id, so the only form left is a null where an entry belongs: stored
            // text can hold one, and an entry that names nothing must not reach a reader.
            if (entries.Exists(entry => entry is null))
            {
                yield return ParentSourcesViolation.FieldWithoutAnId;
            }
        }
    }
}
