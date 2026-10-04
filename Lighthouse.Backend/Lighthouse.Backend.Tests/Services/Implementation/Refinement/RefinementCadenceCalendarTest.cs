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

            var next = RefinementCadenceCalendar.NextAfter(cadence, Day(today), NoBlackouts);
            var isCadenceDay = RefinementCadenceCalendar.IsCadenceDay(cadence, Day(today), NoBlackouts);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(next, Is.EqualTo(nextRefinement is null ? (DateOnly?)null : Day(nextRefinement)));
                Assert.That(isCadenceDay, Is.EqualTo(isRefinementDay));
            }
        }

        // Thursdays every week
        [TestCase("2026-10-08", "2026-10-02", "2026-10-15", false)]
        [TestCase("2026-10-08,2026-10-15", "2026-10-02", "2026-10-22", false)]
        [TestCase("2026-10-08", "2026-10-08", "2026-10-15", false)]
        [TestCase("2026-10-15", "2026-10-08", "2026-10-22", true)]
        [TestCase("2026-10-09", "2026-10-02", "2026-10-08", false)]
        public void A_Refinement_on_a_blackout_day_is_skipped(string blackoutDays, string today, string nextRefinement, bool isRefinementDay)
        {
            var cadence = Cadence("Thursday", 1, null);
            var blackedOut = blackoutDays.Split(',').Select(Day).ToHashSet();

            var next = RefinementCadenceCalendar.NextAfter(cadence, Day(today), blackedOut.Contains);
            var isCadenceDay = RefinementCadenceCalendar.IsCadenceDay(cadence, Day(today), blackedOut.Contains);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(next, Is.EqualTo(Day(nextRefinement)));
                Assert.That(isCadenceDay, Is.EqualTo(isRefinementDay));
            }
        }

        [Test]
        public void A_cadence_blacked_out_for_as_far_as_the_calendar_looks_has_no_next_Refinement()
        {
            var cadence = Cadence("Thursday", 1, null);
            var today = Day("2026-10-02");
            var lastDaySearched = RefinementCadenceCalendar.DaysSearched(cadence, today)?.Last;

            var next = RefinementCadenceCalendar.NextAfter(cadence, today, day => day <= lastDaySearched);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(next, Is.Null);
                Assert.That(lastDaySearched, Is.GreaterThanOrEqualTo(today.AddYears(1)));
            }
        }

        // A year's search from that week would run past the last day the calendar has.
        [Test]
        public void A_starting_week_at_the_end_of_the_calendar_has_no_next_Refinement()
        {
            var next = RefinementCadenceCalendar.NextAfter(Cadence("Thursday", 2, "9999-12-27"), Day("2026-10-02"), NoBlackouts);

            Assert.That(next, Is.Null);
        }

        [Test]
        public void ATeamWithoutACadenceHasNoNextRefinementAndNoRefinementDay()
        {
            var next = RefinementCadenceCalendar.NextAfter(null, Day("2026-10-08"), NoBlackouts);
            var isCadenceDay = RefinementCadenceCalendar.IsCadenceDay(null, Day("2026-10-08"), NoBlackouts);

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

        private static readonly Func<DateOnly, bool> NoBlackouts = _ => false;

        private static DateOnly Day(string isoDay) => DateOnly.ParseExact(isoDay, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
