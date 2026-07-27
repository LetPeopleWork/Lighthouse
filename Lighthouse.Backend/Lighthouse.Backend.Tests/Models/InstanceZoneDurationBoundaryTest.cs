using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Bug #5567 - Finding F. The UTC byte-identity tests cannot see these: under a UTC instance the
    /// two definitions of a day collapse into one, so only a non-UTC zone exposes a duration whose
    /// two ends are reduced differently.
    ///
    /// Every expectation is a literal - re-deriving the production expression is root cause D.
    /// </summary>
    [TestFixture]
    public class InstanceZoneDurationBoundaryTest
    {
        /// <summary>2026-07-27T23:30Z is already 2026-07-28 in Zurich (CEST, UTC+2).</summary>
        private static readonly DateTimeOffset LateEveningInZurich = new(2026, 7, 27, 23, 30, 0, TimeSpan.Zero);

        private static readonly TimeZoneInfo Zurich = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        /// <summary>Inside the offset window: 2026-07-27T22:30Z is 2026-07-28T00:30 in Zurich.</summary>
        private static readonly DateTime StartedInsideTheOffsetWindow = new(2026, 7, 27, 22, 30, 0, DateTimeKind.Utc);

        [Test]
        public void WorkItemAge_ItemStartedInsideTheOffsetWindow_ReducesTheStartInTheInstanceZone()
        {
            var clock = new FakeLighthouseClock(LateEveningInZurich, Zurich);
            var workItem = new WorkItem
            {
                StateCategory = StateCategories.Doing,
                StartedDate = StartedInsideTheOffsetWindow,
            };

            var ageInZurich = workItem.WorkItemAge(clock.Zone, clock.Today);

            clock.SetZone(TimeZoneInfo.Utc);
            var ageInUtc = workItem.WorkItemAge(clock.Zone, clock.Today);

            using (Assert.EnterMultipleScope())
            {
                // Started on the 28th in Zurich, today is the 28th in Zurich - its first day.
                Assert.That(ageInZurich, Is.EqualTo(1));

                // A UTC instance starts it on the 27th and stands on the 27th - unchanged from HEAD.
                Assert.That(ageInUtc, Is.EqualTo(1));
            }
        }

        [Test]
        public void AgeOnDay_ItemStartedInsideTheOffsetWindow_ReducesTheStartInTheInstanceZone()
        {
            var clock = new FakeLighthouseClock(LateEveningInZurich, Zurich);
            var workItem = new WorkItem { StartedDate = StartedInsideTheOffsetWindow };

            var age = workItem.AgeOnDay(clock.Zone, new DateOnly(2026, 7, 28));

            Assert.That(age, Is.EqualTo(1), "The 28th is the item's first day in Zurich, not its second.");
        }

        [Test]
        public void CycleTime_ItemClosedInsideTheOffsetWindow_ReducesBothEndsInTheInstanceZone()
        {
            var clock = new FakeLighthouseClock(LateEveningInZurich, Zurich);
            var workItem = new WorkItem
            {
                StateCategory = StateCategories.Done,
                StartedDate = new DateTime(2026, 7, 27, 9, 0, 0, DateTimeKind.Utc),
                ClosedDate = new DateTime(2026, 7, 28, 22, 30, 0, DateTimeKind.Utc),
            };

            var cycleTimeInZurich = workItem.CycleTime(clock.Zone);

            clock.SetZone(TimeZoneInfo.Utc);
            var cycleTimeInUtc = workItem.CycleTime(clock.Zone);

            using (Assert.EnterMultipleScope())
            {
                // Closed after Zurich midnight: the 27th, 28th and 29th, inclusive.
                Assert.That(cycleTimeInZurich, Is.EqualTo(3));

                // A UTC instance closes it on the 28th - unchanged from HEAD.
                Assert.That(cycleTimeInUtc, Is.EqualTo(2));
            }
        }
    }
}
