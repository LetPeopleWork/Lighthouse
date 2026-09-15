using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models.ConnectionHealth
{
    /// <summary>
    /// The most recent thing observed about one connection's credential. Stored rather than held in
    /// memory because a verdict has to outlive the process that observed it: a refresh can fail at
    /// 02:00 and the administrator opens the popover at 09:00.
    ///
    /// At most one row per connection — the question is "how is this connection now", not "how has it
    /// been", so a new observation replaces the last rather than appending to a history.
    /// </summary>
    public class ConnectionHealthVerdict : IEntity
    {
        public int Id { get; set; }

        public int WorkTrackingSystemConnectionId { get; set; }

        public ConnectionHealthState State { get; set; }

        /// <summary>
        /// The <c>ConnectionValidationResult.Code</c> the connector answered with, kept as the
        /// connector wrote it. <see cref="State"/> is the coarse four-way answer the popover renders;
        /// this is what a support conversation needs when the four states are not enough.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>The sentence an administrator reads. Written by the connector, not by this layer.</summary>
        public string Message { get; set; } = string.Empty;

        public DateTime ObservedAt { get; set; }
    }
}
