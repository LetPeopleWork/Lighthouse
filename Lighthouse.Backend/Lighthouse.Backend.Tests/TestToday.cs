namespace Lighthouse.Backend.Tests
{
    /// <summary>
    /// Bug #5567 - the day to hand to a signature that now takes one, for tests whose subject is
    /// NOT the calendar anchor. Those subjects keep the behaviour they had before the anchor moved,
    /// so their expectations are unchanged.
    ///
    /// Tests that ARE about the anchor must use <see cref="TestDoubles.FakeLighthouseClock"/> with a
    /// literal expectation instead - re-deriving the production expression is root cause D of this
    /// bug and makes an assertion pass for every possible value of "today".
    /// </summary>
    internal static class TestToday
    {
        /// <summary>
        /// One clock per test host, so a subject and the expectations around it can never land on
        /// different days - not even for a run that crosses midnight.
        /// </summary>
        internal static TestDoubles.FakeLighthouseClock Clock { get; } = new(DateTimeOffset.UtcNow);

        internal static DateOnly Ambient => Clock.Today;

        internal static DateTime AmbientAsUtcMidnight => Clock.TodayAsUtcMidnight;

        /// <summary>
        /// A date the <see cref="Models.Delivery"/> constructor's own guard will accept, for tests
        /// whose subject is not the delivery date. A literal passes until the calendar reaches it
        /// and then fails every run after.
        /// </summary>
        internal static DateTime AFutureDate => Clock.TodayAsUtcMidnight.AddDays(30);

        internal static TimeZoneInfo Zone => Clock.Zone;
    }
}
