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

            return StartsBefore(EarliestStart(today, doneItemsCutoffDays), ReportWindow.EndingOn(thenEnd, lengthDays)) ? ReportRefusalCodes.ThenBeforeCutoff : null;
        }

        // A window too long for the calendar to name starts before any day the owner can keep.
        private static bool StartsBefore(DateOnly? earliest, ReportWindow? window)
            => earliest is DateOnly earliestDay && (window is null || window.Start < earliestDay);

        public static ReportWindow? NowWindow(DateOnly today, int lengthDays) => ReportWindow.EndingOn(today, lengthDays);
    }
}
