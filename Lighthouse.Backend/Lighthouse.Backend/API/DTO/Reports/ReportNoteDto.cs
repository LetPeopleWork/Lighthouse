using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Modules.Reports.Domain;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.API.DTO.Reports
{
    public sealed class ReportNoteDto
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateOnly CreatedOn { get; set; }

        public DateTime? LastEditedAt { get; set; }

        public DateOnly? LastEditedOn { get; set; }

        public string? AuthorDisplayName { get; set; }

        public static ReportNoteDto From(ReportNoteRead note, ILighthouseClock clock)
        {
            return new ReportNoteDto
            {
                Id = note.Id,
                Text = note.Text,
                CreatedAt = note.CreatedAt,
                CreatedOn = clock.ToInstanceDay(note.CreatedAt),
                LastEditedAt = note.LastEditedAt,
                LastEditedOn = note.LastEditedAt.HasValue ? clock.ToInstanceDay(note.LastEditedAt.Value) : null,
                AuthorDisplayName = note.AuthorDisplayName,
            };
        }
    }
}
