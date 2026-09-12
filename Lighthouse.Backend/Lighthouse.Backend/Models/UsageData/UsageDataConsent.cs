using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// One browser's answer about usage data. Consent is per browser rather than per account because
    /// most Lighthouse instances have no accounts at all - with authentication off every caller shares
    /// one identity, so an account-scoped answer would be the first person's answer imposed on
    /// everybody.
    /// </summary>
    public class UsageDataConsent : IEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The SHA-256 digest of the token this browser holds. Only the digest is stored, so the
        /// server can recognise a token a browser presents but can never reproduce one itself.
        /// </summary>
        public string TokenHash { get; set; } = string.Empty;

        public UsageDataDecision Decision { get; set; }

        /// <summary>When the button was pressed, and when a withdrawal later replaced that answer.</summary>
        public DateTime DecidedAt { get; set; }

        /// <summary>
        /// The last time this browser presented its token. A grant only counts while this is recent,
        /// which is how a browser that cleared its storage eventually stops counting: clearing storage
        /// happens entirely on the user's machine and sends nothing, so silence is the only signal
        /// there is.
        /// </summary>
        public DateTime LastSeenAt { get; set; }

        /// <summary>
        /// When this browser was last shown the dialog without asking for it. Null until the
        /// unprompted ask exists; it is here now because adding a column later costs a migration on
        /// every supported database.
        /// </summary>
        public DateTime? AskedAt { get; set; }

        /// <summary>
        /// The pseudonym the collector sees, so it can tell a repeat visit from a new one. Derived
        /// from nothing and meaningless outside Lighthouse. It is worked out on the server from the
        /// token the browser presents and is never a field in any request or response, in either
        /// direction - so no caller can choose it, and nobody can emit under somebody else's.
        ///
        /// Null on a row that recorded a refusal, and on every row that was already in the table
        /// when this column arrived: a browser that said no has no pseudonym anywhere.
        /// </summary>
        public string? AnalyticsId { get; set; }
    }
}
