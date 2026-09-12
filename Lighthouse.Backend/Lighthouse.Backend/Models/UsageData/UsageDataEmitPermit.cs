namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// Proof that somebody agreed, minted only where that was actually checked. The part that sends
    /// requires one and has no other way in, so forwarding something nobody agreed to takes a
    /// deliberate act rather than a forgotten call - which is the mistake this shape is here to
    /// prevent. It is not a guarantee the compiler enforces: this is one assembly, and a determined
    /// author can reach the constructor. The architecture rule carries the rest.
    /// </summary>
    public sealed class UsageDataEmitPermit
    {
        internal UsageDataEmitPermit(string analyticsId)
        {
            AnalyticsId = analyticsId;
        }

        /// <summary>
        /// What the collector counts this browser under. Worked out here from the record, never sent
        /// to the browser and never accepted from it.
        /// </summary>
        public string AnalyticsId { get; }
    }
}
