namespace Lighthouse.Backend.Models.Reports
{
    public sealed class ReportNote
    {
        public const int LongestText = 2000;

        public int Id { get; set; }

        public int ReportId { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? LastEditedAt { get; set; }

        public int? AuthorUserProfileId { get; set; }

        public string? AuthorDisplayName { get; set; }

        public bool MayBeChangedBy(int? callerProfileId) => new NoteSignature(AuthorUserProfileId).MayBeChangedBy(callerProfileId);

        public ReportOutcome<ReportNote> Revise(string? rawText, DateTime editedAt)
        {
            var text = rawText?.Trim() ?? string.Empty;
            var refusal = TextRefusal(text);
            if (refusal is not null)
            {
                return ReportOutcome<ReportNote>.Refused(refusal);
            }

            Text = text;
            LastEditedAt = editedAt;
            return ReportOutcome<ReportNote>.Done(this);
        }

        private static string? TextRefusal(string text)
        {
            if (text.Length == 0)
            {
                return ReportRefusalCodes.NoteTextMissing;
            }

            return text.Length > LongestText ? ReportRefusalCodes.NoteTextTooLong : null;
        }

        public static ReportOutcome<ReportNote> Write(int reportId, string? rawText, int? signedByProfileId, string? signerDisplayName, DateTime createdAt)
        {
            var text = rawText?.Trim() ?? string.Empty;
            var refusal = TextRefusal(text);
            if (refusal is not null)
            {
                return ReportOutcome<ReportNote>.Refused(refusal);
            }

            return ReportOutcome<ReportNote>.Done(new ReportNote
            {
                ReportId = reportId,
                Text = text,
                CreatedAt = createdAt,
                AuthorUserProfileId = signedByProfileId,
                AuthorDisplayName = signerDisplayName,
            });
        }
    }
}
