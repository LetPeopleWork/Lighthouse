namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Whether this instance fills in the past days an over-time chart is missing. Asked where a fill is
    /// requested and again where a waiting one is about to start, so that switching it off stops asks that
    /// were already queued as well as new ones.
    /// </summary>
    public interface IOverTimeHistoryFillSwitch
    {
        /// <summary>
        /// Read afresh on every call, never remembered, so switching it takes effect on the next chart load
        /// on every replica without a restart.
        /// </summary>
        bool IsSwitchedOn();
    }
}
