using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    public interface IReportReader
    {
        Report? GetForOwner(ReportOwner owner, int reportId);

        IReadOnlyList<Report> ListForOwner(ReportOwner owner);

        IReadOnlyList<Report> ListForOwners(IReadOnlyCollection<ReportOwner> owners);

        /// <summary>The Team or Portfolio the report belongs to, or null when no such report is stored.</summary>
        ReportOwner? OwnerOf(int reportId);
    }
}
