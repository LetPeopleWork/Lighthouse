using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.API.DTO.Reports
{
    /// <summary>One report's configuration, exactly as the single wizard sends it, and the owners to create it for.</summary>
    public sealed class NewReportsForOwnersRequest : NewReportRequest
    {
        public List<ReportOwnerRequest>? Owners { get; set; }
    }

    public sealed class ReportOwnerRequest
    {
        public ReportOwnerKind? OwnerKind { get; set; }

        public int? OwnerId { get; set; }
    }
}
