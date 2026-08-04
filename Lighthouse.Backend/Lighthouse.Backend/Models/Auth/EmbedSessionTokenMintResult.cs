namespace Lighthouse.Backend.Models.Auth
{
    public class EmbedSessionTokenMintResult
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
