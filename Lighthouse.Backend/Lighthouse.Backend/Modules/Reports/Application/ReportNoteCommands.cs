using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public sealed class ReportNoteCommands(IReportNoteStore store) : IReportNoteCommands
    {
        internal IReportNoteStore Store { get; } = store;

        public async Task<ReportOutcome<ReportNoteRead>> Add(ReportOwner owner, int reportId, ReportNoteCaller caller, string? text, DateTime now)
        {
            if (!Store.ReportExists(owner, reportId))
            {
                return ReportOutcome<ReportNoteRead>.NotFound();
            }

            var signed = caller as ReportNoteCaller.Signed;
            if (signed is null && caller is ReportNoteCaller.Unresolved)
            {
                return ReportOutcome<ReportNoteRead>.Refused(ReportRefusalCodes.NoteAuthorUnknown);
            }

            var written = ReportNote.Write(reportId, text, signed?.ProfileId, signed?.DisplayName, now);
            if (written.Value is not ReportNote note)
            {
                return ReportOutcome<ReportNoteRead>.Refused(written.Code ?? ReportRefusalCodes.NoteTextMissing);
            }

            Store.Add(note);
            await Store.Save();

            return ReportOutcome<ReportNoteRead>.Done(ReportNoteRead.Of(note));
        }

        public Task<ReportOutcome<ReportNoteRead>> Edit(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller, string? text, DateTime now)
            => throw new NotImplementedException();

        public Task<ReportOutcome<ReportNoteRead>> Delete(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller)
            => throw new NotImplementedException();
    }
}
