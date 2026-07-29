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
    //
    // __SCAFFOLD__ - DISTILL wrote the seam and the contract; DELIVER writes the body.
    internal static class ComonotonicCompletionDistribution
    {
        // Returns the concrete Dictionary rather than an interface, mirroring Combine: CA1859 fires on
        // the Sonar gate for non-public members declared as an interface.
        //
        // Contract (ADR-113):
        //   contributors = each histogram's buckets ordered by day key, TrialsIn > 0
        //   count == 0  -> []                       caller treats this as "no bucket", never a distribution
        //   count == 1  -> that histogram VERBATIM  short-circuit; the CDF round-trip is not the identity
        //                                           in IEEE 754 and one shifted trial breaks AC-01.5
        //   count >= 2  -> elementwise minimum of the contributors' cumulative series over the ascending
        //                  union of their day keys, differentiated, scaled by contributors.Max(TrialsIn)
        //                  and allocated with DistributeByLargestRemainder
        public static Dictionary<int, int> Min(IEnumerable<IReadOnlyDictionary<int, int>> histograms)
        {
            throw new InvalidOperationException("__SCAFFOLD__ ComonotonicCompletionDistribution.Min is not implemented yet");
        }
    }
}
