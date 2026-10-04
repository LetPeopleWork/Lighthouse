using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Tests.Models.Refinement
{
    /// <summary>One cadence has one stored form, however the admin happened to name it.</summary>
    [TestFixture]
    [Category("epic-5510-5881-refinement")]
    public class RefinementCadenceTest
    {
        private static readonly DateOnly MondayFifthOfOctober = new(2026, 10, 5);

        private static readonly DayOfWeek[] MondayToSunday =
        [
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
        ];

        private static readonly DayOfWeek[] OnlyThursday = [DayOfWeek.Thursday];

        [Test]
        public void Every_day_of_a_week_names_that_week_by_its_Monday([Range(0, 6)] int daysIntoTheWeek)
        {
            var cadence = RefinementCadence.Of(OnlyThursday, 2, MondayFifthOfOctober.AddDays(daysIntoTheWeek));

            Assert.That(cadence.AnchorWeek, Is.EqualTo(MondayFifthOfOctober));
        }

        [Test]
        public void No_starting_week_stays_no_starting_week()
        {
            Assert.That(RefinementCadence.Of(OnlyThursday, 1, null).AnchorWeek, Is.Null);
        }

        [Test]
        public void Weekdays_named_in_any_order_and_any_number_of_times_are_kept_once_each_from_Monday()
        {
            DayOfWeek[] named = [DayOfWeek.Sunday, DayOfWeek.Thursday, DayOfWeek.Monday, DayOfWeek.Sunday, .. MondayToSunday.Reverse(), DayOfWeek.Thursday];

            Assert.That(RefinementCadence.Of(named, 1, null).Weekdays, Is.EqualTo(MondayToSunday));
        }

        [Test]
        public void The_interval_is_kept_as_named()
        {
            Assert.That(RefinementCadence.Of(OnlyThursday, 3, MondayFifthOfOctober).IntervalWeeks, Is.EqualTo(3));
        }
    }
}
