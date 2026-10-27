using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    public interface IReportQueries
    {
        ReportOutcome<ReportTemplatesRead> Templates(ReportOwner owner);

        ReportOutcome<ReportList> List(ReportOwner owner);

        ReportOutcome<ReportsAcrossOwners> ListAcross(IReadOnlyCollection<ReportOwner> readable);

        ReportOutcome<ReportRead> Get(ReportOwner owner, int reportId);
    }
}
