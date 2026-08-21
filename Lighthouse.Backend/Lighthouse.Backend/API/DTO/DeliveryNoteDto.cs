using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class DeliveryNoteDto
    {
        public DeliveryNoteDto()
        {
        }

        public DeliveryNoteDto(DeliveryNote note, DateOnly createdOn, DateOnly? lastEditedOn)
        {
            Id = note.Id;
            DeliveryId = note.DeliveryId;
            Text = note.Text;
            CreatedAt = note.CreatedAt;
            CreatedOn = createdOn;
            LastEditedAt = note.LastEditedAt;
            LastEditedOn = lastEditedOn;
            AuthorDisplayName = note.AuthorDisplayName;
        }

        public int Id { get; set; }

        public int DeliveryId { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The day a reader sees, already reduced in the instance's own zone. Carried beside the
        /// instant so no client has to turn one into the other and land on the wrong day.
        /// </summary>
        public DateOnly CreatedOn { get; set; }

        public DateTime? LastEditedAt { get; set; }

        public DateOnly? LastEditedOn { get; set; }

        /// <summary>
        /// Null when the note has no author, which is every note on an instance with authentication
        /// switched off. The UI shows it as unattributed rather than inventing a name.
        /// </summary>
        public string? AuthorDisplayName { get; set; }
    }
}
