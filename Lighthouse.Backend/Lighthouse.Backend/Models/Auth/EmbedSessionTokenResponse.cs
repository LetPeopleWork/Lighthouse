namespace Lighthouse.Backend.Models.Auth
{
    public class EmbedSessionTokenResponse
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        /// <summary>The absolute entry-point URL with the token applied, so the caller never composes it.</summary>
        public string EmbedUrl { get; set; } = string.Empty;
    }
}
