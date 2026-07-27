namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Bug #5567 - the named seam for "which calendar day is it?". An instant has no time zone; a
    /// calendar day is defined by one, and with no named seam every author reached for the ambient
    /// UTC day. Days are handed out as <see cref="DateOnly"/> and the one <see cref="DateTime"/>
    /// member carries <see cref="DateTimeKind.Utc"/>, because the global EF converter would shift a
    /// local-kind midnight onto the previous day. Entities take the day as a parameter rather than
    /// this interface, so <c>Models</c> stays free of <c>Services.Interfaces</c>.
    /// </summary>
    public interface ILighthouseClock
    {
        /// <summary>The instance's current calendar day, in the configured instance time zone.</summary>
        DateOnly Today { get; }

        /// <summary><see cref="Today"/> as midnight with <see cref="DateTimeKind.Utc"/>, for EF and for APIs that still speak <see cref="DateTime"/>.</summary>
        DateTime TodayAsUtcMidnight { get; }

        /// <summary>The current instant, delegated to the registered <see cref="TimeProvider"/>.</summary>
        DateTimeOffset Now { get; }

        /// <summary>The resolved instance time zone.</summary>
        TimeZoneInfo Zone { get; }

        /// <summary>
        /// Reduces a stored UTC instant to the calendar day it falls on in the instance time zone:
        /// an item closed at 22:30Z belongs to the next day in Europe/Zurich.
        /// </summary>
        DateOnly ToInstanceDay(DateTime utcInstant);
    }
}
