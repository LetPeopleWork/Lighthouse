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
                .Where(buckets => CompletionHistogram.TrialsIn(buckets) > 0)
                .ToList();

            if (contributors.Count == 0)
            {
                return [];
            }

            var totalTrials = contributors.Max(CompletionHistogram.TrialsIn);
            var days = contributors.SelectMany(buckets => buckets.Select(bucket => bucket.Key)).Distinct().Order().ToArray();

            var cumulativeProbabilities = contributors.Select(buckets => CompletionHistogram.CumulativeProbabilities(buckets, days)).ToList();
            var exactTrials = new double[days.Length];
            var previousProbability = 0d;

            for (var index = 0; index < days.Length; index++)
            {
                // Multiply in a canonical order rather than the caller's. IEEE 754 multiplication is not
                // associative, so a reordered input can differ in the last bit and tip a rounding decision
                // below; sorting first makes the result depend only on the values (AC-01.6).
                var jointProbability = 1d;

                // Stryker disable once Linq: descending is an equally canonical order and yields the same
                // product - what matters is that the order does not come from the caller.
                foreach (var probability in cumulativeProbabilities.Select(contributor => contributor[index]).Order())
                {
                    jointProbability *= probability;
                }

                exactTrials[index] = Math.Max(0, (jointProbability - previousProbability) * totalTrials);
                previousProbability = jointProbability;
            }

            return CompletionHistogram.DistributeByLargestRemainder(days, exactTrials, totalTrials);
        }
    }
}
