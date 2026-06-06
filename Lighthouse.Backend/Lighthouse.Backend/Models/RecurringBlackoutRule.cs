using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class RecurringBlackoutRule : IEntity
    {
        public int Id { get; set; }

        public List<DayOfWeek> Weekdays { get; set; } = [];

        public int IntervalWeeks { get; set; } = 1;

        public DateOnly Start { get; set; }

        public DateOnly? End { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
