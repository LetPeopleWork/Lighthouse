using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public sealed class ReportNoteQueries(IReportNoteStore store) : IReportNoteQueries
    {
        internal IReportNoteStore Store { get; } = store;

        public ReportOutcome<IReadOnlyList<ReportNoteRead>> List(ReportOwner owner, int reportId)
            => ListAs(owner, reportId, ReportNoteRead.Of);

        public ReportOutcome<IReadOnlyList<ReportNoteRead>> List(ReportOwner owner, int reportId, ReportNoteCaller caller)
            => ListAs(owner, reportId, note => ReportNoteRead.Of(note, caller));

        private ReportOutcome<IReadOnlyList<ReportNoteRead>> ListAs(ReportOwner owner, int reportId, Func<ReportNote, ReportNoteRead> read)
        {
            if (!Store.ReportExists(owner, reportId))
            {
                return ReportOutcome<IReadOnlyList<ReportNoteRead>>.NotFound();
            }

            IReadOnlyList<ReportNoteRead> notes =
            [
                .. Store.NotesNewestFirst(owner, reportId)
                    .Select(read),
            ];

            return ReportOutcome<IReadOnlyList<ReportNoteRead>>.Done(notes);
        }
    }
}
