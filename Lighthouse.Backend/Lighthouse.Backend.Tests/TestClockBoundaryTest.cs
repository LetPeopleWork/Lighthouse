using System.Diagnostics;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests
{
    /// <summary>
    /// The frozen test instant is only useful while it sits in the stretch of the night where the
    /// UTC day and the instance day are different days: that is where code that takes "today" from
    /// the wrong zone gives itself away. These tests fail if the instant is moved out of that stretch
    /// or the test host loses its Zurich time zone, either of which would let such code pass again.
    /// </summary>
    [TestFixture]
    public class TestClockBoundaryTest
    {
        private const string InstanceTimeZoneId = "Europe/Zurich";

        [Test]
        public void TestHost_RunsUnderZurichTime()
        {
            Assert.That(
                TimeZoneInfo.Local.HasSameRules(TimeZoneInfo.FindSystemTimeZoneById(InstanceTimeZoneId)),
                Is.True,
                $"Test host is on '{TimeZoneInfo.Local.Id}', which does not keep the same time as {InstanceTimeZoneId}; run the tests with lighthouse.runsettings.");
        }

        [Test]
        public void FrozenNow_IsHalfPastElevenUtc_OnTheDayTheTestProcessStarted()
        {
            using var process = Process.GetCurrentProcess();
            var processStartDay = DateOnly.FromDateTime(process.StartTime.ToUniversalTime());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TestToday.FrozenNow.Offset, Is.EqualTo(TimeSpan.Zero));
                Assert.That(TestToday.FrozenNow.UtcDateTime.TimeOfDay, Is.EqualTo(new TimeSpan(23, 30, 0)));
                Assert.That(DateOnly.FromDateTime(TestToday.FrozenNow.UtcDateTime), Is.EqualTo(processStartDay));
            }
        }

        [Test]
        public void FrozenNow_FallsOnTheNextInstanceDay_WhenTheProcessZoneIsZurich()
        {
            var utcDay = DateOnly.FromDateTime(TestToday.FrozenNow.UtcDateTime);

            var instanceDay = new FakeLighthouseClock(TestToday.FrozenNow, TimeZoneInfo.Local).Today;

            Assert.That(
                instanceDay,
                Is.EqualTo(utcDay.AddDays(1)),
                $"{TestToday.FrozenNow:O} is on {instanceDay} in the test host zone '{TimeZoneInfo.Local.Id}', the same day as in UTC.");
        }

        [TestCase(2026, 7, 15, 2, TestName = "Summer: Zurich keeps UTC+2")]
        [TestCase(2026, 1, 15, 1, TestName = "Winter: Zurich keeps UTC+1")]
        public void FrozenTimeOfDay_FallsOnTheNextInstanceDay_InSummerAndWinter(int year, int month, int day, int expectedOffsetHours)
        {
            var utcDay = new DateOnly(year, month, day);
            var instant = new DateTimeOffset(utcDay.ToDateTime(TimeOnly.FromTimeSpan(TestToday.FrozenNow.UtcDateTime.TimeOfDay)), TimeSpan.Zero);

            var instanceDay = new FakeLighthouseClock(instant, TimeZoneInfo.Local).Today;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    TimeZoneInfo.Local.GetUtcOffset(instant),
                    Is.EqualTo(TimeSpan.FromHours(expectedOffsetHours)),
                    $"Test host zone '{TimeZoneInfo.Local.Id}' does not keep {InstanceTimeZoneId}'s offset on {utcDay}.");
                Assert.That(
                    instanceDay,
                    Is.EqualTo(utcDay.AddDays(1)),
                    $"{instant:O} is on {instanceDay} in the test host zone '{TimeZoneInfo.Local.Id}', the same day as in UTC.");
            }
        }
    }
}
