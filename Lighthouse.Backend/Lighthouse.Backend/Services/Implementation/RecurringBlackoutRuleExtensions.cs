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

            var totalDays = (rangeEnd.DayNumber - rangeStart.DayNumber) + 1;

            return Enumerable.Range(0, totalDays)
                .Select(rangeStart.AddDays)
                .Where(day => WeeklyRecurrence.Matches(rule.Weekdays, rule.IntervalWeeks, rule.Start, day))
                .Select(day => new BlackoutPeriod { Start = day, End = day, Description = rule.Description });
        }
    }
}
