namespace Lighthouse.Backend.Models.Auth
{
    public class ApiKeyCreationResult
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CreatedByUser { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The plaintext API key. This is only returned once on creation and is never stored.
        /// </summary>
        public string PlainTextKey { get; set; } = string.Empty;
    }
}
