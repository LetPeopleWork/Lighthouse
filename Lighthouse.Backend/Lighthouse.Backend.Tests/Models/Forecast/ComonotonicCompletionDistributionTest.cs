using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models.Forecast
{
    // Story #5587 (ADR-113), slice-01. The elementwise minimum WITHIN a team's bucket - the sibling of
    // JointCompletionDistribution, which multiplies ACROSS buckets.
    //
    // Constant-throughput fixtures are point masses and the minimum of point masses is one of them, so
    // they cannot tell Min from Combine at all. Every fixture here is deliberately multi-valued, and
    // Min_TwoIdenticalContributors_ReturnsThatHistogramUnchanged is the one that separates the two
    // operators outright: minimum leaves an identical pair alone, product squares it.
    [Ignore("RED until Story #5587 slice-01 implements ComonotonicCompletionDistribution.Min")]
    public class ComonotonicCompletionDistributionTest
    {
        private const int RandomSeed = 5587;

        [Test]
        public void Min_TwoCrossingContributors_TakesTheElementwiseMinimumOfTheirCumulativeSeries()
        {
            // AC-01.1. Union days 1/3/5. Cumulatives .80/.80/1.00 and .00/.90/1.00; the elementwise
            // minimum is .00/.80/1.00, which is NEITHER input - the fixture crosses on purpose.
            var wideEarly = new Dictionary<int, int> { { 1, 8000 }, { 5, 2000 } };
            var tightLate = new Dictionary<int, int> { { 3, 9000 }, { 5, 1000 } };

            var minimum = ComonotonicCompletionDistribution.Min([wideEarly, tightLate]);

            Assert.That(minimum, Is.EqualTo(new Dictionary<int, int> { { 3, 8000 }, { 5, 2000 } }));
        }

        [Test]
        public void Min_TwoIdenticalContributors_ReturnsThatHistogramUnchanged()
        {
            // The discriminator between the two combinators. Minimum of two identical CDFs is that CDF;
            // JointCompletionDistribution.Combine on the same input squares it and returns
            // {1:2500, 2:3125, 3:4375}. A test that passes for both is not coverage of Min.
            var contributor = new Dictionary<int, int> { { 1, 5000 }, { 2, 2500 }, { 3, 2500 } };

            var minimum = ComonotonicCompletionDistribution.Min([contributor, new Dictionary<int, int>(contributor)]);

            Assert.That(minimum, Is.EqualTo(contributor));
        }

        [Test]
        public void Min_SingleContributor_ReturnsThatHistogramVerbatimIncludingItsEmptyDays()
        {
            // The count == 1 short-circuit is correctness, not speed. Round-tripping one histogram
            // through cumulative -> differentiate -> largest-remainder drops the day-9 bucket (it
            // carries no mass) and can shift a trial in IEEE 754. Either breaks AC-01.5 bit-identity.
            var only = new Dictionary<int, int> { { 5, 5000 }, { 9, 0 }, { 12, 5000 } };

            var minimum = ComonotonicCompletionDistribution.Min([only]);

            Assert.That(minimum, Is.EqualTo(only));
        }

        [Test]
        public void Min_ContributorFinishedBeforeTheUnionMaximum_HoldsItsProbabilityAtOneBeyondItsLastDay()
        {
            var early = new Dictionary<int, int> { { 2, 10 } };
            var late = new Dictionary<int, int> { { 1, 5 }, { 4, 5 } };

            var minimum = ComonotonicCompletionDistribution.Min([early, late]);

            Assert.That(minimum, Is.EqualTo(new Dictionary<int, int> { { 2, 5 }, { 4, 5 } }));
        }

        [Test]
        public void Min_ScaledContributorsLeaveAResidue_AssignsItByLargestRemainder()
        {
            // Contributors simulated with different trial counts. The minimum cumulative series is
            // 1/3 then 1, scaled by the preserved 7 trials -> 2.33 / 4.67; the floors leave one unit of
            // residue, which goes to the larger remainder (day 2). The tie-break itself (equal
            // remainders resolve to the earlier day) lives in the shared CompletionHistogram and is
            // already pinned by JointCompletionDistributionTest - not duplicated here.
            var threeTrials = new Dictionary<int, int> { { 1, 1 }, { 2, 2 } };
            var sevenTrials = new Dictionary<int, int> { { 1, 3 }, { 2, 4 } };

            var minimum = ComonotonicCompletionDistribution.Min([threeTrials, sevenTrials]);

            Assert.That(minimum, Is.EqualTo(new Dictionary<int, int> { { 1, 2 }, { 2, 5 } }));
        }

        [Test]
        public void Min_ContributorWithoutTrials_IsExcludedFromTheMinimum()
        {
            // Defence in depth, not the mechanism: after guard 2 a zero-trial row with remaining work
            // cannot reach Min at all. A test must not treat this filter as THE rule for a finished
            // pair - that rule is the remaining-work predicate in DeliveryCompletionForecast.
            var contributor = new Dictionary<int, int> { { 1, 5 }, { 4, 5 } };

            var minimum = ComonotonicCompletionDistribution.Min([contributor, new Dictionary<int, int>()]);

            Assert.That(minimum, Is.EqualTo(contributor));
        }

        [Test]
        public void Min_EveryContributorWithoutTrials_ReturnsAnEmptyHistogram()
        {
            var minimum = ComonotonicCompletionDistribution.Min([new Dictionary<int, int>(), new Dictionary<int, int> { { 3, 0 } }]);

            Assert.That(minimum, Is.Empty);
        }

        [Test]
        public void Min_NoContributors_ReturnsAnEmptyHistogram()
        {
            // The caller reads this as "no bucket", never as a distribution: an empty bucket that
            // reached the cross-bucket product would drag the delivery to zero.
            var minimum = ComonotonicCompletionDistribution.Min([]);

            Assert.That(minimum, Is.Empty);
        }

        [Test]
        public void Min_ContributorsInEitherOrder_ProduceTheSameHistogram()
        {
            // Min does not sort its inputs and does not need to - it returns one of them unchanged and
            // rounds nothing. The invariant is asserted, the sort is deliberately absent (ADR-113 s.3).
            var wideEarly = new Dictionary<int, int> { { 2, 4000 }, { 9, 3000 }, { 20, 3000 } };
            var tightLate = new Dictionary<int, int> { { 8, 500 }, { 9, 9000 }, { 10, 500 } };

            var forwards = ComonotonicCompletionDistribution.Min([wideEarly, tightLate]);
            var backwards = ComonotonicCompletionDistribution.Min([tightLate, wideEarly]);

            Assert.That(backwards, Is.EqualTo(forwards));
        }

        [Test]
        public void Min_RandomContributors_IsNeverAboveAnyContributorsCumulativeProbability()
        {
            var random = new Random(RandomSeed);

            for (var run = 0; run < 50; run++)
            {
                var contributors = CreateRandomContributors(random);

                var minimum = ComonotonicCompletionDistribution.Min(contributors);

                var tolerance = 1.0 / minimum.Values.Sum() + 1e-9;

                foreach (var day in contributors.SelectMany(c => c.Keys).Distinct())
                {
                    foreach (var contributor in contributors)
                    {
                        Assert.That(
                            CumulativeProbability(minimum, day),
                            Is.LessThanOrEqualTo(CumulativeProbability(contributor, day) + tolerance),
                            $"run {run}, day {day}");
                    }
                }
            }
        }

        [Test]
        public void Min_RandomContributors_SumsToThePreservedTotalTrials()
        {
            var random = new Random(RandomSeed);

            for (var run = 0; run < 50; run++)
            {
                var contributors = CreateRandomContributors(random);
                var expectedTotal = contributors.Max(c => c.Values.Sum());

                var minimum = ComonotonicCompletionDistribution.Min(contributors);

                Assert.That(minimum.Values.Sum(), Is.EqualTo(expectedTotal), $"run {run}");
            }
        }

        private static double CumulativeProbability(IReadOnlyDictionary<int, int> histogram, int day)
        {
            var total = histogram.Values.Sum();
            if (total == 0)
            {
                return 0;
            }

            return histogram.Where(bucket => bucket.Key <= day).Sum(bucket => bucket.Value) / (double)total;
        }

        private static List<Dictionary<int, int>> CreateRandomContributors(Random random)
        {
            return Enumerable.Range(0, random.Next(2, 6))
                .Select(_ => CreateRandomHistogram(random))
                .ToList();
        }

        private static Dictionary<int, int> CreateRandomHistogram(Random random)
        {
            var days = Enumerable.Range(0, random.Next(2, 7))
                .Select(_ => random.Next(1, 40))
                .Distinct()
                .OrderBy(day => day)
                .ToList();

            var totalTrials = random.Next(1, 5) * 1000;
            var histogram = days.ToDictionary(day => day, _ => 0);
            var remaining = totalTrials;

            foreach (var day in days.Take(days.Count - 1))
            {
                var share = random.Next(0, remaining + 1);
                histogram[day] = share;
                remaining -= share;
            }

            histogram[days[^1]] = remaining;

            return histogram;
        }
    }
}
