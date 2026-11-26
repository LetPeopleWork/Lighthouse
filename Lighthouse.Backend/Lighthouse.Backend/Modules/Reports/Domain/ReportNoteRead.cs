namespace Lighthouse.Backend.Modules.Reports.Domain
{
    public sealed record ReportNoteRead(int Id, string Text, DateTime CreatedAt, DateTime? LastEditedAt, string? AuthorDisplayName);
}
