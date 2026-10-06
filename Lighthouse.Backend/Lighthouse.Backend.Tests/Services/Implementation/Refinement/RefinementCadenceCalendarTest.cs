using System.Globalization;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;

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
        public void A_search_that_ends_exactly_on_the_calendars_last_day_still_looks_a_whole_year()
        {
            var today = DateOnly.MaxValue.AddDays(-366);

            var daysSearched = RefinementCadenceCalendar.DaysSearched(Cadence("Thursday", 1, null), today);

            Assert.That(daysSearched, Is.EqualTo((today.AddDays(1), DateOnly.MaxValue)));
        }

        [TestCase("", 1)]
        [TestCase("Thursday", 0)]
        public void A_cadence_with_no_weekday_or_no_interval_searches_no_days(string weekdays, int intervalWeeks)
        {
            var daysSearched = RefinementCadenceCalendar.DaysSearched(Cadence(weekdays, intervalWeeks, null), Day("2026-10-02"));

            Assert.That(daysSearched, Is.Null);
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

        // --- The Refinement cycle the need covers ---

        // Thursdays every week
        [TestCase("Thursday", 1, null, "2026-10-02", "2026-10-08", "2026-10-15")]
        [TestCase("Thursday", 1, null, "2026-10-07", "2026-10-08", "2026-10-15")]
        [TestCase("Thursday", 1, null, "2026-10-08", "2026-10-08", "2026-10-15")]
        [TestCase("Thursday", 1, null, "2026-10-09", "2026-10-15", "2026-10-22")]
        // Mondays and Thursdays: the gap is three days, then four
        [TestCase("Monday,Thursday", 1, null, "2026-10-02", "2026-10-05", "2026-10-08")]
        [TestCase("Monday,Thursday", 1, null, "2026-10-05", "2026-10-05", "2026-10-08")]
        [TestCase("Monday,Thursday", 1, null, "2026-10-06", "2026-10-08", "2026-10-12")]
        [TestCase("Monday,Thursday", 1, null, "2026-10-08", "2026-10-08", "2026-10-12")]
        // Tuesdays every second week from the week of Monday 5 October
        [TestCase("Tuesday", 2, "2026-10-05", "2026-10-02", "2026-10-06", "2026-10-20")]
        [TestCase("Tuesday", 2, "2026-10-05", "2026-10-06", "2026-10-06", "2026-10-20")]
        [TestCase("Tuesday", 2, "2026-10-05", "2026-10-13", "2026-10-20", "2026-11-03")]
        // A starting week still to come
        [TestCase("Tuesday", 2, "2026-10-19", "2026-10-02", "2026-10-20", "2026-11-03")]
        public void The_cycle_runs_from_the_next_Refinement_or_from_today_on_a_Refinement_day_to_the_Refinement_after_it(
            string weekdays, int intervalWeeks, string? anchorWeek, string today, string start, string end)
        {
            var cycle = RefinementCadenceCalendar.CycleFrom(Cadence(weekdays, intervalWeeks, anchorWeek), Day(today), NoBlackouts);

            Assert.That(cycle, Is.EqualTo(new RefinementCycle(Day(start), Day(end))));
        }

        // Thursdays every week
        [TestCase("2026-10-15", "2026-10-02", "2026-10-08", "2026-10-22")]
        [TestCase("2026-10-08", "2026-10-02", "2026-10-15", "2026-10-22")]
        [TestCase("2026-10-08", "2026-10-08", "2026-10-15", "2026-10-22")]
        [TestCase("2026-10-15,2026-10-22", "2026-10-02", "2026-10-08", "2026-10-29")]
        [TestCase("2026-10-12", "2026-10-02", "2026-10-08", "2026-10-15")]
        public void A_blacked_out_Refinement_is_skipped_at_either_end_of_the_cycle(string blackoutDays, string today, string start, string end)
        {
            var blackedOut = blackoutDays.Split(',').Select(Day).ToHashSet();

            var cycle = RefinementCadenceCalendar.CycleFrom(ThursdaysEveryWeek, Day(today), blackedOut.Contains);

            Assert.That(cycle, Is.EqualTo(new RefinementCycle(Day(start), Day(end))));
        }

        [TestCase("", 1)]
        [TestCase("Thursday", 0)]
        public void A_cadence_with_no_weekday_or_no_interval_has_no_cycle(string weekdays, int intervalWeeks)
        {
            var cycle = RefinementCadenceCalendar.CycleFrom(Cadence(weekdays, intervalWeeks, null), FridayTheSecond, NoBlackouts);

            Assert.That(cycle, Is.Null);
        }

        [Test]
        public void A_Team_without_a_cadence_has_no_cycle()
        {
            Assert.That(RefinementCadenceCalendar.CycleFrom(null, FridayTheSecond, NoBlackouts), Is.Null);
        }

        // The next Refinement happens, but every one in the year after it is blacked out.
        [Test]
        public void A_next_Refinement_with_none_after_it_for_as_far_as_the_calendar_looks_has_no_cycle()
        {
            var nextRefinement = Day("2026-10-08");

            var cycle = RefinementCadenceCalendar.CycleFrom(ThursdaysEveryWeek, FridayTheSecond, day => day > nextRefinement);

            Assert.That(cycle, Is.Null);
        }

        // For every day over six weeks, under every cadence, with and without blackout days scattered across
        // the calendar: the cycle starts today when today is a Refinement day and at the next Refinement
        // otherwise, ends at the Refinement after its start, and holds no other Refinement in between.
        [TestCaseSource(nameof(EveryDayUnderEveryCadence))]
        public void The_cycle_is_the_gap_between_two_Refinements_in_a_row(string weekdays, int intervalWeeks, string? anchorWeek, string blackouts, string today)
        {
            var cadence = Cadence(weekdays, intervalWeeks, anchorWeek);
            var isBlackedOut = BlackoutPatterns[blackouts];
            var day = Day(today);
            var expectedStart = RefinementCadenceCalendar.IsCadenceDay(cadence, day, isBlackedOut)
                ? day
                : RefinementCadenceCalendar.NextAfter(cadence, day, isBlackedOut);

            var cycle = RefinementCadenceCalendar.CycleFrom(cadence, day, isBlackedOut);

            Assert.That(cycle, Is.Not.Null);
            var refinementsInside = Enumerable.Range(1, cycle!.End.DayNumber - cycle.Start.DayNumber - 1)
                .Select(cycle.Start.AddDays)
                .Where(inside => RefinementCadenceCalendar.IsCadenceDay(cadence, inside, isBlackedOut));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cycle.Start, Is.EqualTo(expectedStart));
                Assert.That(cycle.End, Is.EqualTo(RefinementCadenceCalendar.NextAfter(cadence, cycle.Start, isBlackedOut)));
                Assert.That(cycle.End, Is.GreaterThan(cycle.Start));
                Assert.That(refinementsInside, Is.Empty);
            }
        }

        // Which day of the week it is never changes a weekly cycle: without blackout days it is always one week.
        [TestCaseSource(nameof(EveryDayUnderEveryWeeklyCadence))]
        public void A_weekly_cycle_is_one_week_whatever_day_today_is(string weekday, string today)
        {
            var cycle = RefinementCadenceCalendar.CycleFrom(Cadence(weekday, 1, null), Day(today), NoBlackouts);

            Assert.That(cycle?.End.DayNumber - cycle?.Start.DayNumber, Is.EqualTo(7));
        }


        private static readonly DateOnly FridayTheSecond = new(2026, 10, 2);

        private static readonly RefinementCadence ThursdaysEveryWeek = RefinementCadence.Of([DayOfWeek.Thursday], 1, null);

        private static readonly DateOnly FirstDaySwept = new(2026, 9, 28);

        private const int DaysSwept = 42;

        private static readonly Dictionary<string, Func<DateOnly, bool>> BlackoutPatterns = new()
        {
            ["none"] = _ => false,
            ["scattered"] = day => day.DayNumber % 5 == 0 || day.DayNumber % 11 == 3,
        };

        private static readonly (string Weekdays, int IntervalWeeks, string? AnchorWeek)[] CadencesSwept =
        [
            ("Thursday", 1, null),
            ("Monday,Thursday", 1, null),
            ("Sunday", 1, null),
            ("Tuesday", 2, "2026-10-05"),
            ("Friday", 3, "2026-10-05"),
            ("Tuesday,Thursday", 2, "2026-10-19"),
        ];

        private static IEnumerable<TestCaseData> EveryDayUnderEveryCadence()
            => from cadence in CadencesSwept
               from blackouts in BlackoutPatterns.Keys
               from offset in Enumerable.Range(0, DaysSwept)
               select new TestCaseData(cadence.Weekdays, cadence.IntervalWeeks, cadence.AnchorWeek, blackouts, IsoDay(FirstDaySwept.AddDays(offset)));

        private static IEnumerable<TestCaseData> EveryDayUnderEveryWeeklyCadence()
            => from weekday in Enum.GetNames<DayOfWeek>()
               from offset in Enumerable.Range(0, 14)
               select new TestCaseData(weekday, IsoDay(FirstDaySwept.AddDays(offset)));

        private static string IsoDay(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

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
