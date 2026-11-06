using Lighthouse.Backend.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>One entry of a parent source list as a client reads and sends it.</summary>
    public sealed record ParentSourceEntryDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter<ParentSourceKind>))]
        public ParentSourceKind? Kind { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AdditionalFieldDefinitionId { get; init; }

        public static List<ParentSourceEntryDto?> ListOf(ParentSources list)
            => [.. list.Entries.Select(entry => new ParentSourceEntryDto { Kind = entry.Kind, AdditionalFieldDefinitionId = entry.AdditionalFieldDefinitionId })];

        /// <summary>The list as text, so two lists can be compared by what they say.</summary>
        public static string TextOf(List<ParentSourceEntryDto?>? list) => JsonSerializer.Serialize(list);

        /// <summary>The text of a list a client would read, for comparing with one it sent.</summary>
        public static string TextOf(ParentSources? list) => TextOf(list is null ? null : ListOf(list));

        /// <summary>What a sent list comes to, an entry naming nothing included so that the list can refuse it.</summary>
        public static ParentSourcesOutcome OutcomeOf(List<ParentSourceEntryDto?> sent)
            => ParentSources.Of(sent.Select(entry => entry?.ToEntry()!));

        /// <summary>
        /// Whether a save that names the old single field is about that field rather than the list it sends. A
        /// client that knows only the old field sends back the list it read together with the field it changed, so
        /// a list equal to the one held, beside a field that differs from the list's first, is that echo.
        /// </summary>
        public static bool TheOldFieldIsTheEdit(int? sentField, List<ParentSourceEntryDto?>? sentList, ParentSources? held)
            => sentField != held?.ToTheLegacyField() && TextOf(sentList) == TextOf(held);

        /// <summary>The entry this names; null when it is neither the tracker's own parent nor a field, so the list can refuse it.</summary>
        public ParentSourceEntry? ToEntry() => (Kind, AdditionalFieldDefinitionId) switch
        {
            (ParentSourceKind.TrackerParent, null) => ParentSourceEntry.TrackerParent,
            (ParentSourceKind.AdditionalField, { } field) => ParentSourceEntry.Field(field),
            _ => null,
        };
    }
}
