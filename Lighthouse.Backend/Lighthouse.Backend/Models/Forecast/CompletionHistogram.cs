namespace Lighthouse.Backend.Models.Forecast
{
    // Shared histogram primitives for the two completion combinators. Both must allocate the
    // largest-remainder residue identically or the delivery rollup stops being bit-identical to a
    // single shared feature's own forecast (ADR-113, AC-01.5) - duplicating them would let the two
    // residue rules drift apart silently.
    //
    // __SCAFFOLD__ - DELIVER's FIRST commit is a behaviour-preserving refactor(forecast): lift the
    // three private helpers out of JointCompletionDistribution VERBATIM into these bodies, proven by
    // JointCompletionDistributionTest passing untouched. That refactor commit is separate from, and
    // precedes, the feature commit.
    internal static class CompletionHistogram
    {
        // A method group rather than an inline lambda: CS9236 fires on Sonar when the same nested
        // generic lambda has to be bound repeatedly. Keep it a method group after the extraction.
        public static int TrialsIn(List<KeyValuePair<int, int>> buckets)
        {
            throw new InvalidOperationException("__SCAFFOLD__ CompletionHistogram.TrialsIn is not implemented yet");
        }

        public static double[] CumulativeProbabilities(List<KeyValuePair<int, int>> buckets, int[] days)
        {
            throw new InvalidOperationException("__SCAFFOLD__ CompletionHistogram.CumulativeProbabilities is not implemented yet");
        }

        public static Dictionary<int, int> DistributeByLargestRemainder(int[] days, double[] exactTrials, int totalTrials)
        {
            throw new InvalidOperationException("__SCAFFOLD__ CompletionHistogram.DistributeByLargestRemainder is not implemented yet");
        }
    }
}
