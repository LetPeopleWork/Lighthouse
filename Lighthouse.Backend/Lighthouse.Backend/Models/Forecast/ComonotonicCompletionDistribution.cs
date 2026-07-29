namespace Lighthouse.Backend.Models.Forecast
{
    // A team's own rows finish together, not independently: ForecastService groups trials by team, so
    // intra-team rows share throughput draws and contend via the random FeatureWIP allocation. The
    // comonotonic upper bound - the elementwise minimum of their CDFs - is the honest WITHIN-a-bucket
    // combinator. JointCompletionDistribution is the sibling that multiplies ACROSS buckets, where
    // independence holds by construction. ADR-113 (Story #5587).
    //
    // Unlike Combine, Min deliberately does NOT sort its inputs: it returns one of them unchanged,
    // does no arithmetic and no rounding, and is invariant under permutation of finite inputs. A
    // mirrored .Order() would be dead code the next reader mistakes for a load-bearing invariant.
    // ADR-113 section 3.
    internal static class ComonotonicCompletionDistribution
    {
        // Returns the concrete Dictionary rather than an interface, mirroring Combine: CA1859 fires on
        // the Sonar gate for non-public members declared as an interface.
        public static Dictionary<int, int> Min(IEnumerable<IReadOnlyDictionary<int, int>> histograms)
        {
            var contributors = histograms
                .Select(histogram => histogram.OrderBy(bucket => bucket.Key).ToList())
                .Where(buckets => CompletionHistogram.TrialsIn(buckets) > 0)
                .ToList();

            if (contributors.Count == 0)
            {
                return [];
            }

            if (contributors.Count == 1)
            {
                // Verbatim, not round-tripped: (a/T - b/T) * T can floor a trial short and the residue
                // pass hands it to a different day, which breaks AC-01.5 bit-identity. Correctness, not
                // speed. Days carrying no mass survive here and would not survive the round trip.
                return contributors[0].ToDictionary(bucket => bucket.Key, bucket => bucket.Value);
            }

            var totalTrials = contributors.Max(CompletionHistogram.TrialsIn);
            var days = contributors.SelectMany(buckets => buckets.Select(bucket => bucket.Key)).Distinct().Order().ToArray();

            var cumulativeProbabilities = contributors.Select(buckets => CompletionHistogram.CumulativeProbabilities(buckets, days)).ToList();
            var exactTrials = new double[days.Length];
            var previousProbability = 0d;

            for (var index = 0; index < days.Length; index++)
            {
                var minimumProbability = cumulativeProbabilities.Min(contributor => contributor[index]);

                exactTrials[index] = Math.Max(0, (minimumProbability - previousProbability) * totalTrials);
                previousProbability = minimumProbability;
            }

            return CompletionHistogram.DistributeByLargestRemainder(days, exactTrials, totalTrials);
        }
    }
}
