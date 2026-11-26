using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public interface IReportNoteStore
    {
        bool ReportExists(ReportOwner owner, int reportId);

        IReadOnlyList<ReportNote> NotesNewestFirst(ReportOwner owner, int reportId);

        void Add(ReportNote note);

        Task Save();

        ReportNote? FindForEdit(ReportOwner owner, int reportId, int noteId);

        void Remove(ReportNote note);

        Task<bool> TrySave();
    }
}
