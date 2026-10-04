namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// "These weekdays, every so many weeks, counted from a starting week": whether a given day falls on it.
    /// Weeks run Monday to Sunday, and no day before the starting week ever matches.
    /// </summary>
    public static class WeeklyRecurrence
    {
        public static bool Matches(IReadOnlyCollection<DayOfWeek> weekdays, int intervalWeeks, DateOnly anchorWeek, DateOnly day)
        {
            if (!weekdays.Contains(day.DayOfWeek))
            {
                return false;
            }

            var weeksBetween = (MondayOfWeek(day).DayNumber - MondayOfWeek(anchorWeek).DayNumber) / 7;

            return weeksBetween >= 0 && weeksBetween % intervalWeeks == 0;
        }

        public static DateOnly MondayOfWeek(DateOnly date)
        {
            return date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        }
    }
}
