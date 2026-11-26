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

        public async Task<ReportOutcome<ReportNoteRead>> Edit(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller, string? text, DateTime now)
        {
            var judged = Judge(owner, reportId, noteId, caller);
            if (judged.Value is not ReportNote note)
            {
                return Unchanged(judged);
            }

            var revised = note.Revise(text, now);
            if (revised.Value is not ReportNote)
            {
                return ReportOutcome<ReportNoteRead>.Refused(revised.Code ?? ReportRefusalCodes.NoteTextMissing);
            }

            return await Saved(note);
        }

        public async Task<ReportOutcome<ReportNoteRead>> Delete(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller)
        {
            var judged = Judge(owner, reportId, noteId, caller);
            if (judged.Value is not ReportNote note)
            {
                return Unchanged(judged);
            }

            Store.Remove(note);

            return await Saved(note);
        }

        // Found first, then who asks, then whether the note is theirs to change.
        private ReportOutcome<ReportNote> Judge(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller)
        {
            var note = Store.FindForEdit(owner, reportId, noteId);
            if (note is null)
            {
                return ReportOutcome<ReportNote>.NotFound();
            }

            if (caller is ReportNoteCaller.Unresolved)
            {
                return ReportOutcome<ReportNote>.Refused(ReportRefusalCodes.NoteAuthorUnknown);
            }

            return note.MayBeChangedBy(caller.ProfileIdOrNone)
                ? ReportOutcome<ReportNote>.Done(note)
                : ReportOutcome<ReportNote>.Refused(ReportRefusalCodes.NoteNotYours);
        }

        private static ReportOutcome<ReportNoteRead> Unchanged(ReportOutcome<ReportNote> judged)
            => judged.Status == ReportOutcomeStatus.NotFound
                ? ReportOutcome<ReportNoteRead>.NotFound()
                : ReportOutcome<ReportNoteRead>.Refused(judged.Code ?? ReportRefusalCodes.NoteNotYours);

        private async Task<ReportOutcome<ReportNoteRead>> Saved(ReportNote note)
            => await Store.TrySave()
                ? ReportOutcome<ReportNoteRead>.Done(ReportNoteRead.Of(note))
                : ReportOutcome<ReportNoteRead>.NotFound();
    }
}
