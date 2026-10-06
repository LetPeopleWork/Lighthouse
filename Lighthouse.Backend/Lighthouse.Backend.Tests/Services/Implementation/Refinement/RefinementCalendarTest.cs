using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    // October 2026: Friday 2, Saturday 3, Sunday 4, Monday 5, Tuesday 6, Wednesday 7, Thursday 8.
    [TestFixture]
    [Category("epic-5510-5881-refinement")]
    public class RefinementCalendarTest
    {

        private static readonly RefinementCadence ThursdaysEveryWeek = RefinementCadence.Of([DayOfWeek.Thursday], 1, null);

        private static readonly DateOnly ThursdayTheFifteenth = new(2026, 10, 15);

        [TestCase(2, 6)]
        [TestCase(7, 1)]
        [TestCase(8, 7)]
        public void The_days_until_the_next_Refinement_count_from_the_instances_today(int todayInOctober, int daysUntil)
        {
            var calendar = CalendarOn(new DateOnly(2026, 10, todayInOctober), new RecordingBlackoutPeriodService());

            var facts = calendar.FactsFor(ThursdaysEveryWeek);

            Assert.That(facts.DaysUntilNextRefinement, Is.EqualTo(daysUntil));
        }

        [Test]
        public void Without_a_cadence_there_are_no_days_until_a_next_Refinement()
        {
            var calendar = CalendarOn(new DateOnly(2026, 10, 2), new RecordingBlackoutPeriodService());

            var facts = calendar.FactsFor(null);

            Assert.That(facts.DaysUntilNextRefinement, Is.Null);
        }

        [Test]
        public void A_starting_week_at_the_end_of_the_calendar_names_no_next_Refinement()
        {
            var calendar = CalendarOn(new DateOnly(2026, 10, 2), new RecordingBlackoutPeriodService());

            var facts = calendar.FactsFor(RefinementCadence.Of([DayOfWeek.Thursday], 2, new DateOnly(9999, 12, 27)));

            Assert.That(facts, Is.EqualTo(RefinementCalendarFacts.None));
        }

        // Fetching every blackout day from today to a starting week centuries ahead expands each recurring blackout
        // across all of them, on every read of the tab.
        [Test]
        public void A_starting_week_years_ahead_is_found_without_asking_for_the_blackout_days_in_between()
        {
            var firstThursday = new DateOnly(3000, 1, 9);
            var blackouts = new RecordingBlackoutPeriodService(new BlackoutPeriod { Start = firstThursday, End = firstThursday });
            var calendar = CalendarOn(new DateOnly(2026, 10, 2), blackouts);

            var facts = calendar.FactsFor(RefinementCadence.Of([DayOfWeek.Thursday], 2, new DateOnly(3000, 1, 6)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(facts.NextRefinementDate, Is.EqualTo(new DateOnly(3000, 1, 23)));
                Assert.That(blackouts.WindowsAskedFor, Is.Not.Empty);
                Assert.That(blackouts.WindowsAskedFor.Select(window => (window.End - window.Start).Days + 1), Is.All.LessThanOrEqualTo(367));
            }
        }

        [Test]
        public void A_cadence_day_blacked_out_today_is_no_Refinement_day()
        {
            var today = new DateOnly(2026, 10, 8);
            var calendar = CalendarOn(today, new RecordingBlackoutPeriodService(new BlackoutPeriod { Start = today, End = today }));

            var facts = calendar.FactsFor(ThursdaysEveryWeek);

            Assert.That(facts, Is.EqualTo(new RefinementCalendarFacts(ThursdayTheFifteenth, false, 7)
            {
                Cycle = new RefinementCycle(ThursdayTheFifteenth, new DateOnly(2026, 10, 22)),
            }));
        }

        // --- The Refinement cycle ---

        [TestCase(2, 8, 15)]
        [TestCase(7, 8, 15)]
        [TestCase(8, 8, 15)]
        [TestCase(9, 15, 22)]
        public void The_facts_name_the_cycle_from_the_next_Refinement_or_today_to_the_one_after(int todayInOctober, int startInOctober, int endInOctober)
        {
            var calendar = CalendarOn(new DateOnly(2026, 10, todayInOctober), new RecordingBlackoutPeriodService());

            var facts = calendar.FactsFor(ThursdaysEveryWeek);

            Assert.That(facts.Cycle, Is.EqualTo(new RefinementCycle(new DateOnly(2026, 10, startInOctober), new DateOnly(2026, 10, endInOctober))));
        }

        [Test]
        public void Without_a_cadence_there_is_no_cycle()
        {
            var calendar = CalendarOn(new DateOnly(2026, 10, 2), new RecordingBlackoutPeriodService());

            Assert.That(calendar.FactsFor(null).Cycle, Is.Null);
        }

        // Every day up to 29 September 2027 is blacked out, so the next Refinement is Thursday 30 September 2027,
        // near the end of the year searched for it. The Refinement after it, 7 October 2027, lies past that year
        // and is blacked out as well, so the cycle has to run on to 14 October 2027.
        [Test]
        public void A_blackout_day_on_the_Refinement_after_the_next_is_honoured_past_the_year_searched_for_the_next()
        {
            var blackouts = new RecordingBlackoutPeriodService(
                new BlackoutPeriod { Start = new DateOnly(2026, 10, 3), End = new DateOnly(2027, 9, 29) },
                new BlackoutPeriod { Start = new DateOnly(2027, 10, 7), End = new DateOnly(2027, 10, 7) });
            var calendar = CalendarOn(new DateOnly(2026, 10, 2), blackouts);

            var facts = calendar.FactsFor(ThursdaysEveryWeek);

            Assert.That(facts.Cycle, Is.EqualTo(new RefinementCycle(new DateOnly(2027, 9, 30), new DateOnly(2027, 10, 14))));
        }

        // Each read of the tab expands every recurring blackout across the days asked for, so the second search
        // adds only the few days it looks at beyond the first year, never a second year.
        [TestCase(2)]
        [TestCase(8)]
        public void Only_the_days_the_second_search_adds_are_fetched_beyond_the_first_year(int todayInOctober)
        {
            var blackouts = new RecordingBlackoutPeriodService();
            var calendar = CalendarOn(new DateOnly(2026, 10, todayInOctober), blackouts);

            var facts = calendar.FactsFor(ThursdaysEveryWeek);

            var daysAskedFor = blackouts.WindowsAskedFor.Sum(window => (window.End - window.Start).Days + 1);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(facts.Cycle, Is.Not.Null);
                Assert.That(daysAskedFor, Is.InRange(367, 367 + 7));
            }
        }

        private static RefinementCalendar CalendarOn(DateOnly today, IBlackoutPeriodService blackoutPeriodService)
        {
            var clock = Mock.Of<ILighthouseClock>(lighthouseClock => lighthouseClock.Today == today);
            return new RefinementCalendar(clock, blackoutPeriodService);
        }

        /// <summary>Answers with the blackout periods it holds that touch the window, and keeps every window it was asked about.</summary>
        private sealed class RecordingBlackoutPeriodService(params BlackoutPeriod[] periods) : IBlackoutPeriodService
        {
            public List<(DateTime Start, DateTime End)> WindowsAskedFor { get; } = [];

            public IReadOnlyList<BlackoutPeriod> GetEffectiveBlackoutDays(DateTime windowStart, DateTime windowEnd)
            {
                WindowsAskedFor.Add((windowStart, windowEnd));
                var first = DateOnly.FromDateTime(windowStart);
                var last = DateOnly.FromDateTime(windowEnd);
                return [.. periods.Where(period => period.Start <= last && period.End >= first)];
            }

            public IEnumerable<BlackoutPeriod> GetAll() => [];

            public BlackoutPeriod? GetById(int id) => null;

            public Task<BlackoutPeriod> Create(BlackoutPeriodDto dto) => throw new NotSupportedException();

            public Task<BlackoutPeriod> Update(int id, BlackoutPeriodDto dto) => throw new NotSupportedException();

            public Task Delete(int id) => throw new NotSupportedException();
        }
    }
}
