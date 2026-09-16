using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.ConnectionHealth;

namespace Lighthouse.Backend.Services.Interfaces.ConnectionHealth
{
    /// <summary>
    /// The one answer to "is this connection's credential working". Three things record a verdict: a
    /// refresh for that connection ended, an administrator pressed Test connection, or nothing had
    /// heard from the connection in long enough that somebody had to ask.
    ///
    /// That third one is not a loop that asks every tracker on a schedule. It asks only connections
    /// whose last answer is missing or older than the staleness threshold — so a connection something
    /// refreshes is never asked at all, and on an instance where every connection belongs to a team the
    /// cost is nothing. What it is for is the connection no team and no portfolio uses, which nothing
    /// else will ever ask about, and which used to read "not checked" for as long as the process lived.
    ///
    /// Only <see cref="RefreshStaleVerdictsAsync"/> takes a cancellation token, and only because it is
    /// the one method a background service enters: it must stop when the host stops. The others cannot
    /// honour one — their reads are synchronous and <c>ValidateConnection</c> deliberately does not
    /// page — so a token there would be a parameter every implementation ignores.
    /// </summary>
    public interface IConnectionHealthService
    {
        Task<IReadOnlyList<ConnectionHealthDto>> GetHealthAsync();

        /// <summary>
        /// Asks the work tracking system, once, right now, and records what it answered.
        /// Null when no connection has that id.
        /// </summary>
        Task<ConnectionHealthDto?> TestConnectionAsync(int connectionId);

        /// <summary>
        /// A refresh reached the tracker, authenticated against it and read real data back, which is the
        /// strongest evidence available that the credential works — stronger than Test connection, which
        /// reads one trivial record. So it records health rather than merely clearing the failure before
        /// it. Erasing the verdict instead is what sent an administrator who had just checked a
        /// connection back to "not checked yet" after the next hourly refresh, with nothing on screen to
        /// say what had undone their check.
        /// </summary>
        Task RecordRefreshSucceededAsync(WorkTrackingSystemConnection connection);

        /// <summary>
        /// A refresh failed. The failure itself carries no status code by the time it arrives here, so
        /// the connection is asked to validate itself and the classification comes from that answer.
        /// </summary>
        Task RecordRefreshFailedAsync(WorkTrackingSystemConnection connection);

        /// <summary>
        /// Asks the connections whose verdict is missing or older than the staleness threshold, and
        /// records what they answered. Leaves every other connection alone — that is what keeps this
        /// from being a recurring outbound call per connection, and it is the whole reason the rule is
        /// expressed as an age rather than as a cadence.
        ///
        /// A connection is claimed before it is asked, so two instances that both find one stale ask it
        /// once between them rather than once each.
        /// </summary>
        Task RefreshStaleVerdictsAsync(CancellationToken cancellationToken);
    }
}
