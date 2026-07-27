namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Bug #5567 - the named seam for "which calendar day is it?".
    ///
    /// An instant has no timezone; a calendar day is defined by one. The codebase already models
    /// the first (the two UtcDateTimeConverter classes plus the TimeProvider seam) but never
    /// modelled the second, so with no named seam every author reached for the ambient UTC day.
    /// (Spelling that expression out here would add a 50th hit to the anchor inventory the 03-01
    /// source guard counts, so it is deliberately described rather than quoted.)
    ///
    /// Two non-negotiable constraints shape this surface:
    /// 1. Days are handed out as <see cref="DateOnly"/>, and the one <see cref="DateTime"/> member
    ///    carries <see cref="DateTimeKind.Utc"/>. The global EF value converter applies
    ///    <c>ToUniversalTime()</c> to values AND to query parameters, so a local-midnight with
    ///    <see cref="DateTimeKind.Local"/> would be shifted back by the offset on write and land on
    ///    the previous UTC day - re-introducing this exact bug through the persistence layer.
    /// 2. Entities take the day as a parameter, never an injected clock, so the purity tests over
    ///    <c>ProjectWorkingDays</c> / <c>ExpandToBlackoutDays</c> keep holding.
    /// </summary>
    public interface ILighthouseClock
    {
        /// <summary>The instance's current calendar day, in the configured instance time zone.</summary>
        DateOnly Today { get; }

        /// <summary>
        /// <see cref="Today"/> as midnight with <see cref="DateTimeKind.Utc"/> - safe to hand to EF
        /// and to any API that still speaks <see cref="DateTime"/>.
        /// </summary>
        DateTime TodayAsUtcMidnight { get; }

        /// <summary>The current instant, delegated to the registered <see cref="TimeProvider"/>.</summary>
        DateTimeOffset Now { get; }

        /// <summary>The resolved instance time zone.</summary>
        TimeZoneInfo Zone { get; }

        /// <summary>
        /// Reduces a stored UTC instant to the calendar day it falls on in the instance time zone.
        /// An item closed at 22:30Z belongs to the next day in Europe/Zurich.
        /// </summary>
        DateOnly ToInstanceDay(DateTime utcInstant);
    }
}
