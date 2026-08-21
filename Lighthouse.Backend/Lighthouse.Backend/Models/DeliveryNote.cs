using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class DeliveryNote : IEntity
    {
        public int Id { get; set; }

        public int DeliveryId { get; set; }

        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// A genuine instant, so the global <c>Properties&lt;DateTime&gt;()</c> converter is correct
        /// for it. The day a reader sees is reduced from this once, server-side, in the instance's
        /// own zone - the client is never handed an instant to turn into a day itself.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        public DateTime? LastEditedAt { get; set; }

        /// <summary>
        /// Null on an instance with authentication switched off, where there is nobody to name. That
        /// is the value the "may I change this note" rule is written around, so it must stay nullable.
        /// </summary>
        public int? AuthorUserProfileId { get; set; }

        /// <summary>
        /// The author's name as it stood when the note was written. Held separately from the profile
        /// so a durable record does not silently re-label itself when somebody is renamed or removed.
        /// </summary>
        public string? AuthorDisplayName { get; set; }
    }
}
