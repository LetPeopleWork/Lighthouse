using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public interface IReportNoteCommands
    {
        Task<ReportOutcome<ReportNoteRead>> Add(ReportOwner owner, int reportId, ReportNoteCaller caller, string? text, DateTime now);
    }
}
