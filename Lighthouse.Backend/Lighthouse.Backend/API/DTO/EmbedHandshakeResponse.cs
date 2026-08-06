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

        /// <summary>
        /// How long the session this grant leads to will last, so the Forge app can decide on a
        /// later page load whether the embed cookie it already holds is still worth framing. It
        /// cannot find out any other way — D13 gives a cross-origin frame no observable signal, and
        /// the cookie is HttpOnly and partitioned. Seconds rather than an instant, because the
        /// session starts at hop 3, not here.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SessionLifetimeSeconds { get; set; }
    }
}
