// SCAFFOLD: true
namespace Lighthouse.Backend.Models.Staleness
{
    public enum StaleReasonKind
    {
        TimeInState,
        BlockedDuration,

        // A Blocked item that has also sat past the time-in-state threshold. It is shown beside the
        // blocked reason so the reader sees both clocks, but on its own it never makes the item stale.
        TimeInStateWhileBlocked,
    }
}
