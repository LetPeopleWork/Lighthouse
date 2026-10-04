using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    // October 2026: Friday 2, Saturday 3, Sunday 4, Monday 5, Tuesday 6, Wednesday 7, Thursday 8.
    [TestFixture]
    [Category("epic-5510-5881-refinement")]
    public class RefinementCalendarTest
    {
        private static readonly RefinementCadence ThursdaysEveryWeek = RefinementCadence.Of([DayOfWeek.Thursday], 1, null);

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

        private static RefinementCalendar CalendarOn(DateOnly today, IBlackoutPeriodService blackoutPeriodService)
        {
            var clock = Mock.Of<ILighthouseClock>(lighthouseClock => lighthouseClock.Today == today);
            return new RefinementCalendar(clock, blackoutPeriodService);
        }

        /// <summary>Has no blackout days, and keeps every window it was asked about.</summary>
        private sealed class RecordingBlackoutPeriodService : IBlackoutPeriodService
        {
            public List<(DateTime Start, DateTime End)> WindowsAskedFor { get; } = [];

            public IReadOnlyList<BlackoutPeriod> GetEffectiveBlackoutDays(DateTime windowStart, DateTime windowEnd)
            {
                WindowsAskedFor.Add((windowStart, windowEnd));
                return [];
            }

            public IEnumerable<BlackoutPeriod> GetAll() => [];

            public BlackoutPeriod? GetById(int id) => null;

            public Task<BlackoutPeriod> Create(BlackoutPeriodDto dto) => throw new NotSupportedException();

            public Task<BlackoutPeriod> Update(int id, BlackoutPeriodDto dto) => throw new NotSupportedException();

            public Task Delete(int id) => throw new NotSupportedException();
        }
    }
}
