using System.Globalization;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models
{
    public class RecurringBlackoutRuleDto
    {
        public RecurringBlackoutRuleDto()
        {
        }

        public RecurringBlackoutRuleDto(RecurringBlackoutRule rule)
        {
            Id = rule.Id;
            Weekdays = [.. rule.Weekdays];
            IntervalWeeks = rule.IntervalWeeks;
            Start = rule.Start;
            End = rule.End;
            Description = rule.Description;
        }

        public int? Id { get; set; }

        [JsonRequired]
        public List<DayOfWeek> Weekdays { get; set; } = [];

        [JsonRequired]
        public int IntervalWeeks { get; set; } = 1;

        [JsonRequired]
        public DateOnly Start { get; set; }

        public DateOnly? End { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Summary => BuildSummary();

        private string BuildSummary()
        {
            var dayNames = string.Join(", ", Weekdays.Select(day => day.ToString()));
            var cadence = IntervalWeeks == 1
                ? "weekly"
                : $"every {IntervalWeeks} weeks";
            var startText = Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var endText = End is null
                ? "no end"
                : End.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return $"Every {dayNames} — {cadence} — from {startText} — {endText}";
        }

        public RecurringBlackoutRule ToEntity()
        {
            return new RecurringBlackoutRule
            {
                Weekdays = [.. Weekdays],
                IntervalWeeks = IntervalWeeks,
                Start = Start,
                End = End,
                Description = Description,
            };
        }
    }
}
