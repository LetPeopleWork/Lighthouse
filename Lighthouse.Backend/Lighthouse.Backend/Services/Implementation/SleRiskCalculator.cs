namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Of every item that was still open at a given age, the share that went on to take longer than
    /// the target. The age is always supplied by the caller and never read from a clock here, which
    /// keeps this a pure function of its three arguments.
    ///
    /// Every open item gets a number. There is no way to express "no answer": an item nothing can be
    /// said about is left out of the collection by the caller instead, which says the same thing
    /// without putting a silence where a reader expects a value.
    /// </summary>
    public static class SleRiskCalculator
    {
        /// <summary>Past the target a miss has already happened, whatever the history holds.</summary>
        private const int CertainRisk = 100;

        /// <summary>
        /// Returns a whole percentage for an item of <paramref name="ageInDays"/> against a target of
        /// <paramref name="targetRangeInDays"/>.
        ///
        /// Both integers must be positive and the caller is expected to have established that: the
        /// service returns early for a team with no target and filters out items that have not
        /// started, exactly as the age-percentile read does.
        /// </summary>
        public static int For(int ageInDays, int targetRangeInDays, IReadOnlyList<int> closedCycleTimes)
        {
            ArgumentNullException.ThrowIfNull(closedCycleTimes);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ageInDays);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetRangeInDays);

            // Ahead of the counting because it needs no history to be true. An item already open
            // longer than the target cannot finish inside it, so this is a definitional answer
            // rather than an empirical one - the only one of the four here that owes no evidence.
            //
            // Strictly greater. An item at four days against a four-day target can still close
            // today and meet "four days or less", so the target day itself stays computed.
            if (ageInDays > targetRangeInDays)
            {
                return CertainRisk;
            }

            var comparableItems = closedCycleTimes.Count(cycleTime => cycleTime >= ageInDays);

            // Nothing the team finished ever ran this long, and the item is still inside its target.
            // A share of an empty set is undefined, so this is a choice rather than arithmetic: zero
            // is the least wrong total answer, because the item can still meet the target and a
            // hundred would say it cannot. The cost is that a thin history reads as a cliff - zero
            // up to the target and a hundred the day after - with nothing in between to grade it.
            if (comparableItems == 0)
            {
                return 0;
            }

            // Only one clause, and the missing one is worth explaining. Every breach is necessarily
            // comparable: the certainty rule above means the age is at most the target by the time
            // we get here, so an item that ran longer than the target also ran at least as long as
            // the item being asked about. Testing for it again would read as a second condition
            // where there is only one.
            var breaches = closedCycleTimes.Count(cycleTime => cycleTime > targetRangeInDays);

            // Away from zero rather than to even, so a risk that sits exactly between two whole
            // percentages is reported as the worse of the two. The alternative rounds half of those
            // items down, and this number exists to decide which ones get attention today.
            return (int)Math.Round(100.0 * breaches / comparableItems, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// How much of the team's finished work was still open at <paramref name="ageInDays"/>. This is
        /// a fact about the history, not about how any risk was derived, which is what lets one number
        /// be true beside every answer <see cref="For"/> can give.
        ///
        /// It deliberately takes no target and has no certainty short-circuit. An item past its target
        /// is told it is certain to miss without the history being consulted, and the count beside that
        /// answer must still describe the history rather than report a zero that reads as "no evidence".
        /// </summary>
        public static int FinishedItemsStillOpenAtThisAge(int ageInDays, IReadOnlyList<int> closedCycleTimes)
        {
            ArgumentNullException.ThrowIfNull(closedCycleTimes);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ageInDays);

            // At least as long, not longer: an item that finished on exactly this day was still open
            // when the day began, and it is one of the items the answer was computed over.
            return closedCycleTimes.Count(cycleTime => cycleTime >= ageInDays);
        }
    }
}
