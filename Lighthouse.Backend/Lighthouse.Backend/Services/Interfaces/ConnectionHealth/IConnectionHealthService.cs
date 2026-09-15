using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.ConnectionHealth;

namespace Lighthouse.Backend.Services.Interfaces.ConnectionHealth
{
    /// <summary>
    /// The one answer to "is this connection's credential working". There are exactly two ways a
    /// verdict gets recorded — a refresh for that connection ended, or an administrator pressed Test
    /// connection — and no third, because a background loop that asked every tracker on a schedule
    /// would spend a rate limit Lighthouse already shares with its own CI.
    ///
    /// No method takes a cancellation token. Nothing on this path can honour one: the reads are
    /// synchronous and <c>ValidateConnection</c> deliberately does not page, so a token here would be
    /// a parameter every implementation ignores.
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
        /// A refresh reached the tracker and came back, which is direct evidence the credential was
        /// accepted — so whatever failure was recorded before it no longer describes this connection.
        /// It does not record health: until somebody tests it, "it worked once" is not a promise the
        /// popover makes.
        /// </summary>
        Task RecordRefreshSucceededAsync(WorkTrackingSystemConnection connection);

        /// <summary>
        /// A refresh failed. The failure itself carries no status code by the time it arrives here, so
        /// the connection is asked to validate itself and the classification comes from that answer.
        /// </summary>
        Task RecordRefreshFailedAsync(WorkTrackingSystemConnection connection);
    }
}
