using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    public interface IReportCommands
    {
        Task<ReportOutcome<ReportRead>> Create(ReportOwner owner, NewReport request);
    }
}
