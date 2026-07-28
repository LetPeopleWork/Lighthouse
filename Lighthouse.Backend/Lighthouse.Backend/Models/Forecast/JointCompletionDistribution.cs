namespace Lighthouse.Backend.Models.Forecast
{
    // A feature is done when every contributing team is done, so its completion CDF is the product of the
    // teams' CDFs - not the worst team's histogram. ADR-110 (Story #5569).
    internal static class JointCompletionDistribution
    {
        public static Dictionary<int, int> Combine(IEnumerable<IReadOnlyDictionary<int, int>> histograms)
        {
            var contributors = histograms
                .Select(histogram => histogram.OrderBy(bucket => bucket.Key).ToList())
                .Where(buckets => buckets.Sum(bucket => bucket.Value) > 0)
                .ToList();

            if (contributors.Count == 0)
            {
                return [];
            }

            var totalTrials = contributors.Max(buckets => buckets.Sum(bucket => bucket.Value));
            var days = contributors.SelectMany(buckets => buckets.Select(bucket => bucket.Key)).Distinct().Order().ToArray();

            var cumulativeProbabilities = contributors.Select(buckets => CumulativeProbabilities(buckets, days)).ToList();
            var exactTrials = new double[days.Length];
            var previousProbability = 0d;

            for (var index = 0; index < days.Length; index++)
            {
                var jointProbability = 1d;
                foreach (var contributor in cumulativeProbabilities)
                {
                    jointProbability *= contributor[index];
                }

                exactTrials[index] = Math.Max(0, (jointProbability - previousProbability) * totalTrials);
                previousProbability = jointProbability;
            }

            return DistributeByLargestRemainder(days, exactTrials, totalTrials);
        }

        private static double[] CumulativeProbabilities(List<KeyValuePair<int, int>> buckets, int[] days)
        {
            var totalTrials = (double)buckets.Sum(bucket => bucket.Value);
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

        private static Dictionary<int, int> DistributeByLargestRemainder(int[] days, double[] exactTrials, int totalTrials)
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
