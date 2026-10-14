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

        /// <summary>
        /// The first day the owner still holds finished items for, or null when it keeps every one of them. A cutoff
        /// reaching back before the first day the calendar can name holds everything from that first day on.
        /// </summary>
        public static DateOnly? EarliestStart(DateOnly today, int doneItemsCutoffDays)
        {
            if (doneItemsCutoffDays <= 0)
            {
                return null;
            }

            return doneItemsCutoffDays < today.DayNumber ? today.AddDays(-doneItemsCutoffDays) : DateOnly.MinValue;
        }

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

            if (ReportWindow.EndingOn(thenEnd, lengthDays) is not ReportWindow window)
            {
                return ReportRefusalCodes.ThenTooLong;
            }

            if (thenEnd > today)
            {
                return ReportRefusalCodes.ThenEndsInTheFuture;
            }

            return EarliestStart(today, doneItemsCutoffDays) is DateOnly earliest && window.Start < earliest ? ReportRefusalCodes.ThenBeforeCutoff : null;
        }

        /// <summary>Why a Now of this length cannot be kept; null when it is long enough and starts on or after the oldest kept day.</summary>
        public static string? NowRefusal(int lengthDays, DateOnly today, int doneItemsCutoffDays)
        {
            if (lengthDays < MinimumLengthDays)
            {
                return ReportRefusalCodes.NowTooShort;
            }

            if (NowWindow(today, lengthDays) is not ReportWindow window)
            {
                return ReportRefusalCodes.NowBeforeCutoff;
            }

            return EarliestStart(today, doneItemsCutoffDays) is DateOnly earliest && window.Start < earliest ? ReportRefusalCodes.NowBeforeCutoff : null;
        }

        public static ReportWindow? NowWindow(DateOnly today, int lengthDays) => ReportWindow.EndingOn(today, lengthDays);
    }
}
