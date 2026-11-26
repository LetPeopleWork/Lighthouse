using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public sealed class ReportNoteQueries(IReportNoteStore store) : IReportNoteQueries
    {
        internal IReportNoteStore Store { get; } = store;

        public ReportOutcome<IReadOnlyList<ReportNoteRead>> List(ReportOwner owner, int reportId)
        {
            if (!Store.ReportExists(owner, reportId))
            {
                return ReportOutcome<IReadOnlyList<ReportNoteRead>>.NotFound();
            }

            IReadOnlyList<ReportNoteRead> notes =
            [
                .. Store.NotesNewestFirst(owner, reportId)
                    .Select(note => new ReportNoteRead(note.Id, note.Text, note.CreatedAt, note.LastEditedAt, note.AuthorDisplayName)),
            ];

            return ReportOutcome<IReadOnlyList<ReportNoteRead>>.Done(notes);
        }
    }
}
