using System.Globalization;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    // October 2026: Friday 2, Saturday 3, Sunday 4, Monday 5, Tuesday 6, Wednesday 7, Thursday 8.
    [TestFixture]
    public class RefinementCadenceCalendarTest
    {
        // Every week
        [TestCase("Thursday", 1, null, "2026-10-02", "2026-10-08", false)]
        [TestCase("Thursday", 1, null, "2026-10-07", "2026-10-08", false)]
        [TestCase("Thursday", 1, null, "2026-10-08", "2026-10-15", true)]
        [TestCase("Thursday", 1, null, "2026-10-09", "2026-10-15", false)]
        [TestCase("Sunday", 1, null, "2026-10-03", "2026-10-04", false)]
        [TestCase("Sunday", 1, null, "2026-10-04", "2026-10-11", true)]
        // Every second week from the week of Monday 5 October
        [TestCase("Tuesday", 2, "2026-10-05", "2026-10-02", "2026-10-06", false)]
        [TestCase("Tuesday", 2, "2026-10-05", "2026-10-06", "2026-10-20", true)]
        [TestCase("Tuesday", 2, "2026-10-05", "2026-10-13", "2026-10-20", false)]
        [TestCase("Tuesday", 2, "2026-10-05", "2026-10-14", "2026-10-20", false)]
        [TestCase("Tuesday", 2, "2026-10-05", "2026-10-20", "2026-11-03", true)]
        [TestCase("Monday", 2, "2026-10-05", "2026-10-11", "2026-10-19", false)]
        // A starting week named by a day other than its Monday is still that week
        [TestCase("Tuesday", 2, "2026-10-08", "2026-10-13", "2026-10-20", false)]
        // Every third week
        [TestCase("Friday", 3, "2026-10-05", "2026-10-09", "2026-10-30", true)]
        [TestCase("Friday", 3, "2026-10-05", "2026-10-16", "2026-10-30", false)]
        [TestCase("Friday", 3, "2026-10-05", "2026-10-23", "2026-10-30", false)]
        // A starting week still to come: nothing falls before it, not even a day a whole interval earlier
        [TestCase("Tuesday", 2, "2026-10-19", "2026-10-02", "2026-10-20", false)]
        [TestCase("Tuesday", 2, "2026-10-19", "2026-10-06", "2026-10-20", false)]
        [TestCase("Tuesday", 3, "2026-11-02", "2026-10-06", "2026-11-03", false)]
        // Two weekdays: whichever comes first
        [TestCase("Tuesday,Thursday", 1, null, "2026-10-06", "2026-10-08", true)]
        [TestCase("Tuesday,Thursday", 1, null, "2026-10-08", "2026-10-13", true)]
        [TestCase("Tuesday,Thursday", 2, "2026-10-05", "2026-10-08", "2026-10-20", true)]
        [TestCase("Tuesday,Thursday", 2, "2026-10-05", "2026-10-07", "2026-10-08", false)]
        // No cadence to speak of
        [TestCase("", 1, null, "2026-10-08", null, false)]
        [TestCase("Thursday", 0, null, "2026-10-08", null, false)]
        public void TheNextRefinementIsTheFirstCadenceDayStrictlyAfterToday(string weekdays, int intervalWeeks, string? anchorWeek, string today, string? nextRefinement, bool isRefinementDay)
        {
            var cadence = Cadence(weekdays, intervalWeeks, anchorWeek);

            var next = RefinementCadenceCalendar.NextAfter(cadence, Day(today));
            var isCadenceDay = RefinementCadenceCalendar.IsCadenceDay(cadence, Day(today));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(next, Is.EqualTo(nextRefinement is null ? (DateOnly?)null : Day(nextRefinement)));
                Assert.That(isCadenceDay, Is.EqualTo(isRefinementDay));
            }
        }

        [Test]
        public void ATeamWithoutACadenceHasNoNextRefinementAndNoRefinementDay()
        {
            var next = RefinementCadenceCalendar.NextAfter(null, Day("2026-10-08"));
            var isCadenceDay = RefinementCadenceCalendar.IsCadenceDay(null, Day("2026-10-08"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(next, Is.Null);
                Assert.That(isCadenceDay, Is.False);
            }
        }

        private static RefinementCadence Cadence(string weekdays, int intervalWeeks, string? anchorWeek)
        {
            return new RefinementCadence
            {
                Weekdays = [.. weekdays.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<DayOfWeek>)],
                IntervalWeeks = intervalWeeks,
                AnchorWeek = anchorWeek is null ? null : Day(anchorWeek),
            };
        }

        private static DateOnly Day(string isoDay) => DateOnly.ParseExact(isoDay, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
