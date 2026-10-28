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

    /// <summary>What a run did for one owner: created with the new report's id, or not created with the reason.</summary>
    public sealed record ReportOwnerOutcome(ReportOwnerKind OwnerKind, int OwnerId, ReportCreationStatus Status, int? ReportId, string? Code)
    {
        public static ReportOwnerOutcome Created(ReportOwner owner, int reportId)
            => new(owner.Kind, owner.Id, ReportCreationStatus.Created, reportId, null);

        public static ReportOwnerOutcome NotCreated(ReportOwner owner, string? code)
            => new(owner.Kind, owner.Id, ReportCreationStatus.NotCreated, null, code);
    }

    public sealed record ReportBulkCreation(IReadOnlyList<ReportOwnerOutcome> Outcomes);
}
