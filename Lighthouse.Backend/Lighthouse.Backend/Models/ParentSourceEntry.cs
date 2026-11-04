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

    // SCAFFOLD: Story 6501. A plain value so the acceptance tests can build lists. The real entry validates in
    // its constructor and throws for a TrackerParent with a field id, an AdditionalField without one and an
    // unknown kind, so tests build entries only through TrackerParent and Field, and build a refused list from
    // stored text. This scaffold does not validate yet.
    public sealed record ParentSourceEntry(ParentSourceKind Kind, int? AdditionalFieldDefinitionId)
    {
        public static ParentSourceEntry TrackerParent { get; } = new(ParentSourceKind.TrackerParent, null);

        public static ParentSourceEntry Field(int additionalFieldDefinitionId)
            => new(ParentSourceKind.AdditionalField, additionalFieldDefinitionId);
    }
}
