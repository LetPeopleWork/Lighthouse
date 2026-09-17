namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Of every item that was still open at a given age, the share that went on to take longer than
    /// the target. The age is always supplied by the caller, never read from a clock here, because
    /// the display path asks about the end of a chosen window while write-back asks about today.
    /// </summary>
    public static class SleRiskCalculator
    {
        /// <summary>
        /// Returns a whole percentage, or null when the question cannot be answered: no target was
        /// published, the age is unusable, or nothing that ever finished ran as long as the item
        /// already has. Null is not zero and not a hundred — there is no evidence either way, and a
        /// number would read as if there were.
        /// </summary>
        public static int? Risk(int ageInDays, int targetRangeInDays, IReadOnlyList<int> closedCycleTimes)
        {
            ArgumentNullException.ThrowIfNull(closedCycleTimes);

            if (targetRangeInDays <= 0 || ageInDays <= 0)
            {
                return null;
            }

            var survivors = closedCycleTimes.Count(cycleTime => cycleTime >= ageInDays);

            if (survivors == 0)
            {
                return null;
            }

            var breaches = closedCycleTimes.Count(cycleTime => cycleTime > targetRangeInDays && cycleTime >= ageInDays);

            // Away from zero rather than to even, so a risk that sits exactly between two whole
            // percentages is reported as the worse of the two. The alternative rounds half of those
            // items down, and this number exists to decide which ones get attention today.
            return (int)Math.Round(100.0 * breaches / survivors, MidpointRounding.AwayFromZero);
        }
    }
}
