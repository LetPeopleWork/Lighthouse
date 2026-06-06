using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation
{
    public static class RecurringBlackoutRuleExtensions
    {
        public static IEnumerable<BlackoutPeriod> ExpandToBlackoutDays(this RecurringBlackoutRule rule, DateOnly windowStart, DateOnly windowEnd)
        {
            var rangeStart = windowStart > rule.Start ? windowStart : rule.Start;
            var rangeEnd = rule.End is null || rule.End.Value > windowEnd ? windowEnd : rule.End.Value;

            if (rangeStart > rangeEnd || rule.Weekdays.Count == 0)
            {
                return [];
            }

            var anchorMonday = MondayOfWeek(rule.Start);
            var totalDays = (rangeEnd.DayNumber - rangeStart.DayNumber) + 1;

            return Enumerable.Range(0, totalDays)
                .Select(rangeStart.AddDays)
                .Where(day => Matches(rule, day, anchorMonday))
                .Select(day => new BlackoutPeriod { Start = day, End = day, Description = rule.Description });
        }

        private static bool Matches(RecurringBlackoutRule rule, DateOnly day, DateOnly anchorMonday)
        {
            if (!rule.Weekdays.Contains(day.DayOfWeek))
            {
                return false;
            }

            var weeksBetween = (MondayOfWeek(day).DayNumber - anchorMonday.DayNumber) / 7;

            return weeksBetween >= 0 && weeksBetween % rule.IntervalWeeks == 0;
        }

        private static DateOnly MondayOfWeek(DateOnly date)
        {
            return date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        }
    }
}
