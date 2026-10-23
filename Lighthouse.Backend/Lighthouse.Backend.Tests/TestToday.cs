using System.Diagnostics;

namespace Lighthouse.Backend.Tests
{
    /// <summary>
    /// The day to hand to a signature that takes one, for tests whose subject is NOT the calendar
    /// anchor. Those subjects keep the behaviour they had before the anchor moved, so their
    /// expectations are unchanged.
    ///
    /// Tests that ARE about the anchor must use <see cref="TestDoubles.FakeLighthouseClock"/> with a
    /// literal expectation instead - re-deriving the production expression makes an assertion pass
    /// for every possible value of "today".
    /// </summary>
    internal static class TestToday
    {
        /// <summary>
        /// Half past eleven at night UTC on the day the test process started. In Zurich that is
        /// already the next day, so code that takes "today" from UTC instead of from the instance
        /// time zone fails on every run rather than only on runs made late at night. Taken from the
        /// process start so it is the same instant however long the run takes.
        /// </summary>
        internal static DateTimeOffset FrozenNow { get; } = HalfPastElevenUtcOnTheDayTheProcessStarted();

        /// <summary>
        /// One clock per test host, so a subject and the expectations around it can never land on
        /// different days - not even for a run that crosses midnight.
        /// </summary>
        internal static TestDoubles.FakeLighthouseClock Clock { get; } = new(DateTimeOffset.UtcNow);

        internal static DateOnly Ambient => Clock.Today;

        internal static DateTime AmbientAsUtcMidnight => Clock.TodayAsUtcMidnight;

        /// <summary>
        /// A date far enough ahead that the controller's "must be in the future" guard accepts it,
        /// for tests whose subject is not the delivery date. A literal passes until the calendar
        /// reaches it and then fails every run after.
        /// </summary>
        internal static DateTime AFutureDate => Clock.TodayAsUtcMidnight.AddDays(30);

        internal static TimeZoneInfo Zone => Clock.Zone;

        private static DateTimeOffset HalfPastElevenUtcOnTheDayTheProcessStarted()
        {
            using var process = Process.GetCurrentProcess();
            var startDay = process.StartTime.ToUniversalTime().Date;
            return new DateTimeOffset(startDay.AddHours(23).AddMinutes(30), TimeSpan.Zero);
        }
    }
}
