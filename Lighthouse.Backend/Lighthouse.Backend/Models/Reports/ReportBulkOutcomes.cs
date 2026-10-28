using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models.Reports
{
    public enum ReportCreationStatus
    {
        [JsonStringEnumMemberName("created")]
        Created,

        [JsonStringEnumMemberName("notCreated")]
        NotCreated,
    }

    /// <summary>
    /// What a run did for one owner: created with the new report's id, or not created with the reason. A window
    /// refused for reaching before the owner's kept history also names the first day that history holds.
    /// </summary>
    public sealed record ReportOwnerOutcome(ReportOwnerKind OwnerKind, int OwnerId, ReportCreationStatus Status, int? ReportId, string? Code, DateOnly? EarliestStart = null)
    {
        public static ReportOwnerOutcome Created(ReportOwner owner, int reportId)
            => new(owner.Kind, owner.Id, ReportCreationStatus.Created, reportId, null);

        public static ReportOwnerOutcome NotCreated(ReportOwner owner, string? code, DateOnly? earliestStart = null)
            => new(owner.Kind, owner.Id, ReportCreationStatus.NotCreated, null, code, earliestStart);
    }

    public sealed record ReportBulkCreation(IReadOnlyList<ReportOwnerOutcome> Outcomes);
}
