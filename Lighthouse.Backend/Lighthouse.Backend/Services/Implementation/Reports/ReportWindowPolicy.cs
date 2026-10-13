using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    /// <summary>
    /// The bounds of a report's windows. Pure: today and how far back the owner keeps finished items are always
    /// handed in, so the same answer comes back for the same day whoever asks.
    /// </summary>
    public static class ReportWindowPolicy
    {
        public const int MinimumLengthDays = 14;

        /// <summary>The first day the owner still holds finished items for, or null when it keeps every one of them.</summary>
        public static DateOnly? EarliestStart(DateOnly today, int doneItemsCutoffDays)
            => doneItemsCutoffDays > 0 ? today.AddDays(-doneItemsCutoffDays) : null;

        /// <summary>Whether Then could still be computed afresh from what the owner holds today.</summary>
        public static bool ThenRebuildable(DateOnly thenStart, DateOnly today, int doneItemsCutoffDays)
            => EarliestStart(today, doneItemsCutoffDays) is not DateOnly earliest || thenStart >= earliest;

        /// <summary>Why Then, as entered, cannot be frozen; null when its length, end and start are acceptable.</summary>
        public static string? ThenRefusal(DateOnly thenEnd, int lengthDays, DateOnly today, int doneItemsCutoffDays)
        {
            if (lengthDays < MinimumLengthDays)
            {
                return ReportRefusalCodes.ThenTooShort;
            }

            if (thenEnd > today)
            {
                return ReportRefusalCodes.ThenEndsInTheFuture;
            }

            return StartsBefore(EarliestStart(today, doneItemsCutoffDays), thenEnd, lengthDays) ? ReportRefusalCodes.ThenBeforeCutoff : null;
        }

        // Day numbers rather than AddDays, so a length reaching past the start of the calendar is still answered.
        private static bool StartsBefore(DateOnly? earliest, DateOnly end, int lengthDays)
            => earliest is DateOnly earliestDay && (long)end.DayNumber - (lengthDays - 1) < earliestDay.DayNumber;

        public static ReportWindow? NowWindow(DateOnly today, int lengthDays) => ReportWindow.EndingOn(today, lengthDays);
    }
}
