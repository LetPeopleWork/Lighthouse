using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What one entry of a parent source list names: the tracker's own parent, or an Additional Field of the
    /// connection (a field or a link type phrase).
    /// </summary>
    public enum ParentSourceKind
    {
        TrackerParent = 0,
        AdditionalField = 1,
    }

    /// <summary>
    /// One place to read a parent from. The tracker's own parent carries no field id and an Additional Field
    /// always does; no other entry can be built, by code or by the serialiser.
    /// </summary>
    public sealed record ParentSourceEntry
    {
        public static ParentSourceEntry TrackerParent { get; } = new(ParentSourceKind.TrackerParent, null);

        public ParentSourceKind Kind { get; }

        public int? AdditionalFieldDefinitionId { get; }

        [JsonConstructor]
        private ParentSourceEntry(ParentSourceKind kind, int? additionalFieldDefinitionId)
        {
            if (!CanBeKept(kind, additionalFieldDefinitionId))
            {
                throw new ArgumentException(
                    $"A parent source of kind {kind} cannot have the field id '{additionalFieldDefinitionId}'.");
            }

            Kind = kind;
            AdditionalFieldDefinitionId = additionalFieldDefinitionId;
        }

        public static ParentSourceEntry Field(int additionalFieldDefinitionId)
            => new(ParentSourceKind.AdditionalField, additionalFieldDefinitionId);

        private static bool CanBeKept(ParentSourceKind kind, int? additionalFieldDefinitionId) => kind switch
        {
            ParentSourceKind.TrackerParent => additionalFieldDefinitionId is null,
            ParentSourceKind.AdditionalField => additionalFieldDefinitionId is not null,
            _ => false,
        };
    }
}
