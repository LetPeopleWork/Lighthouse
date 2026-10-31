using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models.Reports
{
    public static class ReportBulkRun
    {
        /// <summary>The most Teams and Portfolios one bulk creation may name, and the most reports one bulk removal may name; a longer request is refused whole.</summary>
        public const int MostPerRun = 50;
    }

    public enum ReportCreationStatus
    {
        [JsonStringEnumMemberName("created")]
        Created,

        [JsonStringEnumMemberName("notCreated")]
        NotCreated,
    }

    /// <summary>
    /// What a run did for one owner: created with the new report's id, or not created with the reason. A window
    /// refused for reaching before the owner's kept history also names the first day that history holds. The owner
    /// is named when the run found it and the caller may write to it; a gone owner, or one the caller may not
    /// change, goes unnamed, so a request naming ids the caller cannot see learns nothing about them.
    /// </summary>
    public sealed record ReportOwnerOutcome(ReportOwnerKind OwnerKind, int OwnerId, ReportCreationStatus Status, int? ReportId, string? Code, DateOnly? EarliestStart = null)
    {
        public string? OwnerName { get; init; }

        public static ReportOwnerOutcome Created(ReportOwner owner, int reportId)
            => new(owner.Kind, owner.Id, ReportCreationStatus.Created, reportId, null);

        public static ReportOwnerOutcome NotCreated(ReportOwner owner, string? code, DateOnly? earliestStart = null)
            => new(owner.Kind, owner.Id, ReportCreationStatus.NotCreated, null, code, earliestStart);
    }

    public sealed record ReportBulkCreation(IReadOnlyList<ReportOwnerOutcome> Outcomes);

    public enum ReportRemovalStatus
    {
        [JsonStringEnumMemberName("removed")]
        Removed,

        [JsonStringEnumMemberName("notRemoved")]
        NotRemoved,
    }

    /// <summary>What a run did for one report: removed, possibly by somebody else first, or not removed with the reason.</summary>
    public sealed record ReportRemovalOutcome(int ReportId, ReportRemovalStatus Status, string? Code)
    {
        public static ReportRemovalOutcome Removed(int reportId, string? code = null) => new(reportId, ReportRemovalStatus.Removed, code);

        public static ReportRemovalOutcome NotRemoved(int reportId, string code) => new(reportId, ReportRemovalStatus.NotRemoved, code);
    }

    public sealed record ReportBulkRemoval(IReadOnlyList<ReportRemovalOutcome> Outcomes);
}
