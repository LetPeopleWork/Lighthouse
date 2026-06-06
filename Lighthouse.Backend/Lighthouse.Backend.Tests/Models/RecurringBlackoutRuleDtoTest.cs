using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class RecurringBlackoutRuleDtoTest
    {
        private static readonly DayOfWeek[] FridayOnly = [DayOfWeek.Friday];

        private static readonly DayOfWeek[] WeekendDays = [DayOfWeek.Saturday, DayOfWeek.Sunday];

        [Test]
        public void Constructor_GivenEntityWithOpenEndedRule_PreservesAllFields()
        {
            var entity = new RecurringBlackoutRule
            {
                Id = 7,
                Weekdays = [.. FridayOnly],
                IntervalWeeks = 4,
                Start = new DateOnly(2026, 6, 12),
                End = null,
                Description = "Sprint review blackout",
            };

            var subject = new RecurringBlackoutRuleDto(entity);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Id, Is.EqualTo(7));
                Assert.That(subject.Weekdays, Is.EquivalentTo(FridayOnly));
                Assert.That(subject.IntervalWeeks, Is.EqualTo(4));
                Assert.That(subject.Start, Is.EqualTo(new DateOnly(2026, 6, 12)));
                Assert.That(subject.End, Is.Null);
                Assert.That(subject.Description, Is.EqualTo("Sprint review blackout"));
            }
        }

        [Test]
        public void ToEntity_GivenDtoWithBoundedEnd_PreservesAllFields()
        {
            var dto = new RecurringBlackoutRuleDto
            {
                Weekdays = [.. WeekendDays],
                IntervalWeeks = 1,
                Start = new DateOnly(2026, 6, 1),
                End = new DateOnly(2026, 12, 31),
                Description = "Weekends",
            };

            var entity = dto.ToEntity();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entity.Weekdays, Is.EquivalentTo(WeekendDays));
                Assert.That(entity.IntervalWeeks, Is.EqualTo(1));
                Assert.That(entity.Start, Is.EqualTo(new DateOnly(2026, 6, 1)));
                Assert.That(entity.End, Is.EqualTo(new DateOnly(2026, 12, 31)));
                Assert.That(entity.Description, Is.EqualTo("Weekends"));
            }
        }
    }
}
