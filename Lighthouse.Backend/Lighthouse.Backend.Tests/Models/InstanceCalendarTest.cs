using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Bug #5567 - <see cref="InstanceCalendar.DayOf"/> is the one place the whole fix reduces an
    /// instant to a calendar day, so its handling of the incoming <see cref="DateTimeKind"/> is
    /// load-bearing rather than defensive tidying:
    ///
    /// - a <see cref="DateTimeKind.Local"/> value carries a wall-clock reading in the HOST zone and
    ///   must be CONVERTED, because relabelling it Utc moves the underlying instant by the host
    ///   offset - the standalone-distribution half of this bug (RCA branch B);
    /// - an <see cref="DateTimeKind.Unspecified"/> value is a stored instant that lost its kind on
    ///   the way through a provider or a parser, and must be RELABELLED Utc, because converting it
    ///   would apply the host offset a second time.
    ///
    /// Both directions need a host zone that is NOT UTC to be observable at all - UTC is the one
    /// offset at which the mistake cancels out. lighthouse.runsettings pins the test host to
    /// Europe/Zurich for exactly that reason and
    /// ForecastControllerCalendarDayAnchorTest.TestHost_RunsUnderThePinnedInstanceTimeZone fails
    /// loudly if the pin ever goes inert.
    /// </summary>
    [TestFixture]
    public class InstanceCalendarTest
    {
        private static readonly DateOnly ExpectedDay = new(2026, 7, 27);

        [OneTimeSetUp]
        public void RequireANonUtcHostZone()
        {
            Assert.That(
                TimeZoneInfo.Local,
                Is.Not.EqualTo(TimeZoneInfo.Utc),
                "these cases are vacuous on a UTC host - the lighthouse.runsettings TZ pin must be in effect");
        }

        /// <summary>
        /// A local-kind value read as if it were already UTC lands on the wrong instant, and for the
        /// ~2h nightly window either side of midnight that is a different calendar day.
        /// </summary>
        [Test]
        public void DayOf_LocalKindInstant_ConvertsItRatherThanRelabellingIt()
        {
            var lateEveningUtc = new DateTime(2026, 7, 27, 23, 30, 0, DateTimeKind.Utc);

            var day = InstanceCalendar.DayOf(lateEveningUtc.ToLocalTime(), TimeZoneInfo.Utc);

            Assert.That(day, Is.EqualTo(ExpectedDay));
        }

        /// <summary>
        /// The mirror image: a stored instant that arrives without a kind is already UTC, so
        /// converting it would subtract the host offset from a value that never carried it.
        /// </summary>
        [Test]
        public void DayOf_UnspecifiedKindInstant_IsReadAsUtcRatherThanAsHostLocalTime()
        {
            var justAfterUtcMidnight = new DateTime(2026, 7, 27, 0, 30, 0, DateTimeKind.Unspecified);

            var day = InstanceCalendar.DayOf(justAfterUtcMidnight, TimeZoneInfo.Utc);

            Assert.That(day, Is.EqualTo(ExpectedDay));
        }

        /// <summary>The control: a UTC-kind instant is neither converted nor shifted.</summary>
        [Test]
        public void DayOf_UtcKindInstant_IsLeftWhereItIs()
        {
            var justAfterUtcMidnight = new DateTime(2026, 7, 27, 0, 30, 0, DateTimeKind.Utc);

            var day = InstanceCalendar.DayOf(justAfterUtcMidnight, TimeZoneInfo.Utc);

            Assert.That(day, Is.EqualTo(ExpectedDay));
        }
    }
}
