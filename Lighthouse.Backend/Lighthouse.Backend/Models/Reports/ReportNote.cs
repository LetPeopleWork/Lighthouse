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

        public static ReportOutcome<ReportNote> Write(int reportId, string? rawText, int? signedByProfileId, string? signerDisplayName, DateTime createdAt)
        {
            throw new NotImplementedException();
        }
    }
}
