using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// ADR-137 D45: every property is omitted when null, so a pending, unknown, expired, consumed or
    /// malformed nonce serialises to the same bare object and the channel is no oracle for live
    /// sessions. No absolute URL — the Forge side composes the entry URL from the instance it stores.
    /// </summary>
    public class EmbedHandshakeResponse
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Token { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ExpiresAt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RefusalCode { get; set; }
    }
}
