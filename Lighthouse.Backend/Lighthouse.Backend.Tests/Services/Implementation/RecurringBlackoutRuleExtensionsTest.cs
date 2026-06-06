using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class RecurringBlackoutRuleExtensionsTest
    {
        private static readonly DayOfWeek[] FridayOnly = [DayOfWeek.Friday];
        private static readonly DayOfWeek[] WeekendDays = [DayOfWeek.Saturday, DayOfWeek.Sunday];

        [Test]
        public void ExpandToBlackoutDays_EveryFourthFriday_MatchesAnchoredFridaysAndSkipsOffWeekFridays()
        {
            var rule = new RecurringBlackoutRule
            {
                Weekdays = [.. FridayOnly],
                IntervalWeeks = 4,
                Start = new DateOnly(2026, 6, 12),
                End = new DateOnly(2026, 12, 31),
            };

            var days = MatchedDays(rule, new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31));

            Assert.That(days, Is.EquivalentTo(new[]
            {
                new DateOnly(2026, 6, 12),
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 8, 7),
            }));
        }

        [Test]
        public void ExpandToBlackoutDays_DaysOutsideRuleStartEndBounds_AreExcluded()
        {
            var rule = new RecurringBlackoutRule
            {
                Weekdays = [.. FridayOnly],
                IntervalWeeks = 4,
                Start = new DateOnly(2026, 6, 12),
                End = new DateOnly(2026, 12, 31),
            };

            var days = MatchedDays(rule, new DateOnly(2026, 6, 1), new DateOnly(2027, 1, 31));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(days, Does.Not.Contain(new DateOnly(2026, 6, 5)));
                Assert.That(days, Does.Not.Contain(new DateOnly(2027, 1, 8)));
            }
        }

        [Test]
        public void ExpandToBlackoutDays_OpenEndedRule_IsBoundedByConsumerWindow()
        {
            var rule = new RecurringBlackoutRule
            {
                Weekdays = [.. WeekendDays],
                IntervalWeeks = 1,
                Start = new DateOnly(2026, 6, 1),
                End = null,
            };

            var days = MatchedDays(rule, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14));

            Assert.That(days, Is.EquivalentTo(new[]
            {
                new DateOnly(2026, 6, 6),
                new DateOnly(2026, 6, 7),
                new DateOnly(2026, 6, 13),
                new DateOnly(2026, 6, 14),
            }));
        }

        [Test]
        public void ExpandToBlackoutDays_IntervalOne_ReproducesPlainWeekly()
        {
            var rule = new RecurringBlackoutRule
            {
                Weekdays = [.. FridayOnly],
                IntervalWeeks = 1,
                Start = new DateOnly(2026, 6, 12),
                End = null,
            };

            var days = MatchedDays(rule, new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 3));

            Assert.That(days, Is.EquivalentTo(new[]
            {
                new DateOnly(2026, 6, 12),
                new DateOnly(2026, 6, 19),
                new DateOnly(2026, 6, 26),
                new DateOnly(2026, 7, 3),
            }));
        }

        [Test]
        public void ExpandToBlackoutDays_DayMatchingRuleButOutsideWindow_IsExcluded()
        {
            var rule = new RecurringBlackoutRule
            {
                Weekdays = [.. FridayOnly],
                IntervalWeeks = 1,
                Start = new DateOnly(2026, 6, 12),
                End = null,
            };

            var days = MatchedDays(rule, new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 25));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(days, Does.Not.Contain(new DateOnly(2026, 6, 12)));
                Assert.That(days, Is.EquivalentTo(new[] { new DateOnly(2026, 6, 19) }));
            }
        }

        [Test]
        public void ExpandToBlackoutDays_EmptyWeekdays_YieldsNoDays()
        {
            var rule = new RecurringBlackoutRule
            {
                Weekdays = [],
                IntervalWeeks = 1,
                Start = new DateOnly(2026, 6, 1),
                End = null,
            };

            var result = rule.ExpandToBlackoutDays(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ExpandToBlackoutDays_EachMatchingDay_YieldsSingleDayBlackoutPeriod()
        {
            var rule = new RecurringBlackoutRule
            {
                Weekdays = [.. FridayOnly],
                IntervalWeeks = 4,
                Start = new DateOnly(2026, 6, 12),
                End = null,
            };

            var period = rule
                .ExpandToBlackoutDays(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30))
                .Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(period.Start, Is.EqualTo(new DateOnly(2026, 6, 12)));
                Assert.That(period.End, Is.EqualTo(new DateOnly(2026, 6, 12)));
            }
        }

        private static List<DateOnly> MatchedDays(RecurringBlackoutRule rule, DateOnly windowStart, DateOnly windowEnd)
        {
            return rule.ExpandToBlackoutDays(windowStart, windowEnd).Select(period => period.Start).ToList();
        }
    }
}
