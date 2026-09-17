namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// What the history can say about one open item, and how much history it had to say it with.
    /// A null risk is the only way "no answer" is expressed; the count says which kind of no-answer
    /// it is, so a surface can tell "nothing ever ran this long" from "too little did".
    /// </summary>
    public readonly record struct SleRiskVerdict(int? Risk, int ComparableItems);

    /// <summary>
    /// The first age at which the risk reaches <paramref name="Risk"/>. The chart paints from here up
    /// to the next zone's age.
    /// </summary>
    public readonly record struct SleRiskZone(int Risk, int FromAge);

    /// <summary>
    /// Of every item that was still open at a given age, the share that went on to take longer than
    /// the target. The age is always supplied by the caller, never read from a clock here, because
    /// the display path asks about the end of a chosen window while write-back asks about today.
    /// </summary>
    public static class SleRiskCalculator
    {
        /// <summary>
        /// How many finished items must have run at least as long as the open one before their
        /// verdict is worth showing. The displayed value is a share of exactly these, so one item
        /// entering or leaving the window moves it by up to 100/count percentage points: ten holds
        /// that under ten points, and below ten a number would swing overnight on precisely the
        /// items a coach is being told to prioritise. Measured, not guessed —
        /// docs/feature/epic-4127-sle-risk/OUT-4127-risk-stability.md.
        /// </summary>
        public const int MinimumComparableItems = 10;

        /// <summary>
        /// Returns a whole percentage, or no answer at all when the question cannot honestly be
        /// answered: no target was published, the age is unusable, or too little of what the team
        /// finished ever ran as long as this item already has. Never zero and never a hundred in
        /// those cases — a number would read as knowledge.
        /// </summary>
        public static SleRiskVerdict For(int ageInDays, int targetRangeInDays, IReadOnlyList<int> closedCycleTimes)
        {
            ArgumentNullException.ThrowIfNull(closedCycleTimes);

            if (targetRangeInDays <= 0 || ageInDays <= 0)
            {
                return new SleRiskVerdict(null, 0);
            }

            var comparableItems = closedCycleTimes.Count(cycleTime => cycleTime >= ageInDays);

            if (comparableItems < MinimumComparableItems)
            {
                return new SleRiskVerdict(null, comparableItems);
            }

            var breaches = closedCycleTimes.Count(cycleTime => cycleTime > targetRangeInDays && cycleTime >= ageInDays);

            // Away from zero rather than to even, so a risk that sits exactly between two whole
            // percentages is reported as the worse of the two. The alternative rounds half of those
            // items down, and this number exists to decide which ones get attention today.
            var risk = (int)Math.Round(100.0 * breaches / comparableItems, MidpointRounding.AwayFromZero);

            return new SleRiskVerdict(risk, comparableItems);
        }

        /// <summary>
        /// The levels the chart draws a background zone above, calmest first.
        /// </summary>
        private static readonly int[] ZoneLevels = [25, 50, 75, 100];

        /// <summary>
        /// The ages at which the risk first reaches each level, for the chart's background. Empty
        /// when no target was published, when nothing has finished, or when the history is too thin
        /// to place even the first boundary.
        ///
        /// The risk only rises with age — below the target the numerator is fixed while the
        /// denominator only shrinks, and above it every remaining item is a breach — so each level is
        /// crossed once and walking the ages upward finds them all without searching.
        /// </summary>
        public static IReadOnlyList<SleRiskZone> Zones(int targetRangeInDays, IReadOnlyList<int> closedCycleTimes)
        {
            ArgumentNullException.ThrowIfNull(closedCycleTimes);

            var zones = new List<SleRiskZone>();

            if (targetRangeInDays <= 0 || closedCycleTimes.Count == 0)
            {
                return zones;
            }

            var oldestFinishedItem = closedCycleTimes.Max();
            var nextLevel = 0;

            for (var age = 1; age <= oldestFinishedItem && nextLevel < ZoneLevels.Length; age++)
            {
                var risk = For(age, targetRangeInDays, closedCycleTimes).Risk;

                if (risk is null)
                {
                    // Fewer items ran this long than the answer needs, and fewer still will have run
                    // longer - so this is the last age anything can be said about, not a gap.
                    break;
                }

                while (nextLevel < ZoneLevels.Length && risk >= ZoneLevels[nextLevel])
                {
                    zones.Add(new SleRiskZone(ZoneLevels[nextLevel], age));
                    nextLevel++;
                }
            }

            return zones;
        }
    }
}
