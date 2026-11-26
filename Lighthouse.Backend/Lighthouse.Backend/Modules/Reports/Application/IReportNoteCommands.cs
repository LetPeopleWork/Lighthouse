using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public interface IReportNoteCommands
    {
        Task<ReportOutcome<ReportNoteRead>> Add(ReportOwner owner, int reportId, ReportNoteCaller caller, string? text, DateTime now);

        Task<ReportOutcome<ReportNoteRead>> Edit(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller, string? text, DateTime now);

        Task<ReportOutcome<ReportNoteRead>> Delete(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller);
    }
}
