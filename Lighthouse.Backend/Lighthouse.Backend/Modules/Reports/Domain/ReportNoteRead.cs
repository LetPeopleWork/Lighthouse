using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Modules.Reports.Domain
{
    public sealed record ReportNoteRead(int Id, string Text, DateTime CreatedAt, DateTime? LastEditedAt, string? AuthorDisplayName, bool CanModify = false)
    {
        public static ReportNoteRead Of(ReportNote note)
            => new(note.Id, note.Text, note.CreatedAt, note.LastEditedAt, note.AuthorDisplayName);

        public static ReportNoteRead Of(ReportNote note, ReportNoteCaller caller)
            => Of(note) with { CanModify = caller is not ReportNoteCaller.Unresolved && note.MayBeChangedBy(caller.ProfileIdOrNone) };
    }
}
