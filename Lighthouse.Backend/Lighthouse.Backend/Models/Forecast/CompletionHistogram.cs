namespace Lighthouse.Backend.Models.Forecast
{
    // Shared histogram primitives for the two completion combinators. Both must allocate the
    // largest-remainder residue identically or the delivery rollup stops being bit-identical to a
    // single shared feature's own forecast (ADR-113, AC-01.5) - duplicating them would let the two
    // residue rules drift apart silently.
    internal static class CompletionHistogram
    {
        // A method group rather than an inline lambda: CS9236 fires on Sonar when the same nested
        // generic lambda has to be bound repeatedly. Keep it a method group after the extraction.
        public static int TrialsIn(List<KeyValuePair<int, int>> buckets)
        {
            return buckets.Sum(bucket => bucket.Value);
        }

        public static double[] CumulativeProbabilities(List<KeyValuePair<int, int>> buckets, int[] days)
        {
            var totalTrials = (double)TrialsIn(buckets);
            var probabilities = new double[days.Length];
            var completedTrials = 0;
            var nextBucket = 0;

            for (var index = 0; index < days.Length; index++)
            {
                while (nextBucket < buckets.Count && buckets[nextBucket].Key <= days[index])
                {
                    completedTrials += buckets[nextBucket].Value;
                    nextBucket++;
                }

                probabilities[index] = completedTrials / totalTrials;
            }

            return probabilities;
        }

        public static Dictionary<int, int> DistributeByLargestRemainder(int[] days, double[] exactTrials, int totalTrials)
        {
            var trials = new int[days.Length];
            var assignedTrials = 0;

            for (var index = 0; index < days.Length; index++)
            {
                trials[index] = (int)Math.Floor(exactTrials[index]);
                assignedTrials += trials[index];
            }

            var residue = totalTrials - assignedTrials;
            var byLargestRemainder = Enumerable.Range(0, days.Length)
                .OrderByDescending(index => exactTrials[index] - trials[index])
                .ThenBy(index => days[index])
                .Take(residue);

            foreach (var index in byLargestRemainder)
            {
                trials[index]++;
            }

            var histogram = new Dictionary<int, int>();
            for (var index = 0; index < days.Length; index++)
            {
                if (trials[index] > 0)
                {
                    histogram[days[index]] = trials[index];
                }
            }

            return histogram;
        }
    }
}
