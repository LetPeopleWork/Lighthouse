namespace Lighthouse.Backend.Models.ConnectionHealth
{
    /// <summary>
    /// What Lighthouse is willing to say about a connection's credential. Append new members only:
    /// the value is stored as a number, so a member inserted above an existing one relabels every
    /// verdict recorded so far.
    ///
    /// <see cref="Unknown"/> is first because it is the state of a connection nothing has been
    /// observed about, and that is what a fresh row and a fresh instance both are.
    /// </summary>
    public enum ConnectionHealthState
    {
        /// <summary>
        /// Nothing has been observed. Never rendered as healthy: claiming a credential works because
        /// nothing has disproved it is how a status icon becomes decorative.
        /// </summary>
        Unknown = 0,

        /// <summary>The work tracking system accepted the credential when it was last asked.</summary>
        Healthy = 1,

        /// <summary>
        /// Something went wrong and the connector could not say it was the credential. Deliberately
        /// the fallback for every unclassified failure: sending an administrator to reissue a
        /// credential that was never the problem costs more than saying less.
        /// </summary>
        Unreachable = 2,

        /// <summary>The credential itself needs attention — rejected, revoked, expired, or unreadable.</summary>
        AuthenticationFailed = 3,
    }
}
