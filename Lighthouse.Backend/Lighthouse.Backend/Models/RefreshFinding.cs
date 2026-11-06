using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Something a refresh noticed about how a setting is configured that an administrator has to fix in
    /// the tracker or in Lighthouse, because the refresh read less than the setting asked for. The tracker's
    /// own words travel literally, so the screen can build the sentence in whatever terms the reader uses.
    /// <paramref name="HadANextSource"/> says whether an entry of a parent source list had another entry after
    /// it in the run that raised the finding. It is kept here because the list may have changed by the time
    /// the finding is read, and it defaults to false so that a finding stored without it claims nothing.
    /// </summary>
    public sealed record RefreshFinding(
        RefreshFindingKind Kind,
        RefreshFindingSetting Setting,
        string Named,
        IReadOnlyList<string> Seen,
        bool HadANextSource = false);

    // Stored and sent by name, so the members can be reordered or added to without relabelling a stored finding.
    [JsonConverter(typeof(JsonStringEnumConverter<RefreshFindingKind>))]
    public enum RefreshFindingKind
    {
        NamedLinkMatchedNothing,
        NamedLinkPointsBothWays,
        RelationTypeHasNoDirection,
        RelationTypeNamedWithoutItsEnd,
        RelationTypeCannotBeReadHere,
        ParentSourceMatchedNothing,
    }

    [JsonConverter(typeof(JsonStringEnumConverter<RefreshFindingSetting>))]
    public enum RefreshFindingSetting
    {
        ParentOverrideField,
        DependencyField,
        SizeEstimateField,
        FeatureOwnerField,
        EstimationField,
    }
}
