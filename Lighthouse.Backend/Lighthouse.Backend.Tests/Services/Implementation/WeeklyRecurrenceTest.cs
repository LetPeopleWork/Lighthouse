using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class WeeklyRecurrenceTest
    {
        private static readonly DayOfWeek[] Fridays = [DayOfWeek.Friday];

        private static readonly DayOfWeek[] TuesdaysAndThursdays = [DayOfWeek.Tuesday, DayOfWeek.Thursday];

        private static readonly DayOfWeek[] NoWeekday = [];

        // Every fourth Friday from the week of Friday 12 June 2026.
        [TestCase(2026, 6, 12, true)]
        [TestCase(2026, 7, 10, true)]
        [TestCase(2026, 8, 7, true)]
        [TestCase(2026, 6, 19, false)]
        [TestCase(2026, 6, 26, false)]
        [TestCase(2026, 7, 3, false)]
        [TestCase(2026, 7, 9, false)]
        public void EveryFourthFriday_MatchesOnlyTheFridaysOfEveryFourthWeek(int year, int month, int day, bool matches)
        {
            Assert.That(WeeklyRecurrence.Matches(Fridays, 4, new DateOnly(2026, 6, 12), new DateOnly(year, month, day)), Is.EqualTo(matches));
        }

        // A week before the starting week is never a recurrence, even one a whole interval earlier.
        [TestCase(2026, 6, 5)]
        [TestCase(2026, 5, 15)]
        public void ADayBeforeTheStartingWeek_NeverMatches(int year, int month, int day)
        {
            Assert.That(WeeklyRecurrence.Matches(Fridays, 4, new DateOnly(2026, 6, 12), new DateOnly(year, month, day)), Is.False);
        }

        // The starting week is the week as a whole, whichever of its days names it.
        [TestCase(2026, 6, 8)]
        [TestCase(2026, 6, 14)]
        public void AnyDayOfTheStartingWeek_AnchorsTheSameWeeks(int year, int month, int day)
        {
            Assert.That(WeeklyRecurrence.Matches(Fridays, 2, new DateOnly(year, month, day), new DateOnly(2026, 6, 26)), Is.True);
        }

        // Every second week from the week of Monday 5 October 2026, on Tuesdays and Thursdays.
        [TestCase(2026, 10, 6, true)]
        [TestCase(2026, 10, 8, true)]
        [TestCase(2026, 10, 13, false)]
        [TestCase(2026, 10, 15, false)]
        [TestCase(2026, 10, 20, true)]
        [TestCase(2026, 10, 22, true)]
        [TestCase(2026, 10, 21, false)]
        public void TwoWeekdaysEverySecondWeek_MatchEachOfThemInTheOnWeeksOnly(int year, int month, int day, bool matches)
        {
            Assert.That(WeeklyRecurrence.Matches(TuesdaysAndThursdays, 2, new DateOnly(2026, 10, 5), new DateOnly(year, month, day)), Is.EqualTo(matches));
        }

        [Test]
        public void EveryWeek_MatchesTheWeekdayInEveryWeekFromTheStart()
        {
            Assert.That(WeeklyRecurrence.Matches(Fridays, 1, new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 19)), Is.True);
        }

        [Test]
        public void NoWeekdays_MatchesNothing()
        {
            Assert.That(WeeklyRecurrence.Matches(NoWeekday, 1, new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 12)), Is.False);
        }

        // Sunday closes the week that began on Monday, so it belongs to that week, not to the next one.
        [TestCase(2026, 10, 4, 2026, 9, 28)]
        [TestCase(2026, 10, 5, 2026, 10, 5)]
        [TestCase(2026, 10, 8, 2026, 10, 5)]
        [TestCase(2026, 10, 11, 2026, 10, 5)]
        public void MondayOfWeek_IsTheMondayThatOpensTheDaysWeek(int year, int month, int day, int mondayYear, int mondayMonth, int mondayDay)
        {
            Assert.That(WeeklyRecurrence.MondayOfWeek(new DateOnly(year, month, day)), Is.EqualTo(new DateOnly(mondayYear, mondayMonth, mondayDay)));
        }
    }
}
