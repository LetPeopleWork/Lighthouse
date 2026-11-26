using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public interface IReportNoteQueries
    {
        ReportOutcome<IReadOnlyList<ReportNoteRead>> List(ReportOwner owner, int reportId);
    }
}
