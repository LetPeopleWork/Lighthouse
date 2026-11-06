using Lighthouse.Backend.Models;
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

        /// <summary>The entry this names; null when it is neither the tracker's own parent nor a field, so the list can refuse it.</summary>
        public ParentSourceEntry? ToEntry() => (Kind, AdditionalFieldDefinitionId) switch
        {
            (ParentSourceKind.TrackerParent, null) => ParentSourceEntry.TrackerParent,
            (ParentSourceKind.AdditionalField, { } field) => ParentSourceEntry.Field(field),
            _ => null,
        };
    }
}
