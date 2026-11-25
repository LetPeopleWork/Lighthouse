using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public interface IReportQueries
    {
        ReportOutcome<ReportTemplatesRead> Templates(ReportOwner owner);

        ReportOutcome<ReportList> List(ReportOwner owner);

        ReportOutcome<ReportsAcrossOwners> ListAcross(IReadOnlyCollection<ReportOwner> readable);

        ReportOutcome<ReportCreationOptions> CreationOptions(IReadOnlyCollection<ReportOwner> readable);

        ReportOutcome<ReportRead> Get(ReportOwner owner, int reportId);
    }
}
