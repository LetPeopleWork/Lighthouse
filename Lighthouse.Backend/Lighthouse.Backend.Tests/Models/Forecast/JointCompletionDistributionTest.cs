using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models.Forecast
{
    // Story #5569 (ADR-110). Tier 1 (exact) + Tier 2 (invariants) of the slice-01 test strategy.
    // Constant-throughput fixtures are point masses, and the product of point masses IS the max the
    // buggy code already returned - every fixture here is deliberately multi-valued.
    public class JointCompletionDistributionTest
    {
        private const int RandomSeed = 5569;

        [Test]
        public void Combine_TwoTeamsWithIdenticalTwoValueHistograms_ProducesTheExactJointHistogram()
        {
            var teamHistogram = new Dictionary<int, int> { { 1, 5000 }, { 2, 2500 }, { 3, 2500 } };

            var joint = JointCompletionDistribution.Combine([teamHistogram, new Dictionary<int, int>(teamHistogram)]);

            // CDF .50/.75/1.00 each -> joint .25/.5625/1.00 -> PMF .25/.3125/.4375
            Assert.That(joint, Is.EqualTo(new Dictionary<int, int> { { 1, 2500 }, { 2, 3125 }, { 3, 4375 } }));
        }

        [Test]
        public void Combine_SingleContributor_ReturnsThatHistogramUnchanged()
        {
            var contributor = new Dictionary<int, int> { { 4, 2 }, { 7, 1 }, { 5, 3 }, { 9, 1 }, { 3, 1 }, { 6, 2 } };

            var joint = JointCompletionDistribution.Combine([contributor]);

            Assert.That(joint, Is.EqualTo(contributor));
        }

        [Test]
        public void Combine_ContributorFinishedBeforeTheUnionMaximum_KeepsItsProbabilityAtOneBeyondItsLastDay()
        {
            var early = new Dictionary<int, int> { { 2, 10 } };
            var late = new Dictionary<int, int> { { 1, 5 }, { 4, 5 } };

            var joint = JointCompletionDistribution.Combine([early, late]);

            Assert.That(joint, Is.EqualTo(new Dictionary<int, int> { { 2, 5 }, { 4, 5 } }));
        }

        [Test]
        public void Combine_ScaledBucketsLeaveAResidue_AssignsItByLargestRemainderPreferringTheEarlierDay()
        {
            var threeTrials = new Dictionary<int, int> { { 1, 1 }, { 2, 2 } };
            var twoTrials = new Dictionary<int, int> { { 1, 1 }, { 2, 1 } };

            var joint = JointCompletionDistribution.Combine([threeTrials, twoTrials]);

            // Joint PMF 1/6 and 5/6 scaled by 3 -> 0.5 / 2.5, floors 0 / 2, one unit of residue,
            // remainders tie at .5 so the earlier day wins.
            Assert.That(joint, Is.EqualTo(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }));
        }

        [Test]
        public void Combine_ContributorWithoutTrials_IsExcludedFromTheProduct()
        {
            var contributor = new Dictionary<int, int> { { 1, 5 }, { 4, 5 } };

            var joint = JointCompletionDistribution.Combine([contributor, new Dictionary<int, int>()]);

            Assert.That(joint, Is.EqualTo(contributor));
        }

        [Test]
        public void Combine_EveryContributorWithoutTrials_ReturnsAnEmptyHistogram()
        {
            var joint = JointCompletionDistribution.Combine([new Dictionary<int, int>(), new Dictionary<int, int> { { 3, 0 } }]);

            Assert.That(joint, Is.Empty);
        }

        [Test]
        public void Combine_NoContributors_ReturnsAnEmptyHistogram()
        {
            var joint = JointCompletionDistribution.Combine([]);

            Assert.That(joint, Is.Empty);
        }

        [Test]
        public void Combine_CrossingContributors_IsIndependentOfInputOrder()
        {
            var tightLate = new Dictionary<int, int> { { 8, 500 }, { 9, 9000 }, { 10, 500 } };
            var wideEarly = new Dictionary<int, int> { { 2, 4000 }, { 9, 3000 }, { 20, 3000 } };
            var expected = new Dictionary<int, int> { { 8, 200 }, { 9, 6450 }, { 10, 350 }, { 20, 3000 } };

            var forwards = JointCompletionDistribution.Combine([tightLate, wideEarly]);
            var backwards = JointCompletionDistribution.Combine([wideEarly, tightLate]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forwards, Is.EqualTo(expected));
                Assert.That(backwards, Is.EqualTo(expected));
            }
        }

        [Test]
        public void Combine_RandomContributors_SumsToThePreservedTotalTrials()
        {
            var random = new Random(RandomSeed);

            for (var run = 0; run < 50; run++)
            {
                var contributors = CreateRandomContributors(random);
                var expectedTotal = contributors.Max(c => c.Values.Sum());

                var joint = JointCompletionDistribution.Combine(contributors);

                Assert.That(joint.Values.Sum(), Is.EqualTo(expectedTotal), $"run {run}");
            }
        }

        [Test]
        public void Combine_RandomContributors_JointProbabilityNeverExceedsAnyContributorProbability()
        {
            var random = new Random(RandomSeed);

            for (var run = 0; run < 50; run++)
            {
                var contributors = CreateRandomContributors(random);

                var joint = JointCompletionDistribution.Combine(contributors);

                var jointTotal = joint.Values.Sum();
                var tolerance = 1.0 / jointTotal + 1e-9;

                foreach (var day in contributors.SelectMany(c => c.Keys).Distinct())
                {
                    var jointProbability = CumulativeProbability(joint, day);

                    foreach (var contributor in contributors)
                    {
                        Assert.That(jointProbability, Is.LessThanOrEqualTo(CumulativeProbability(contributor, day) + tolerance), $"run {run}, day {day}");
                    }
                }
            }
        }

        [Test]
        public void Combine_RepeatedInvocationOnTheSameInput_ProducesTheSameHistogram()
        {
            var contributors = CreateRandomContributors(new Random(RandomSeed));

            var first = JointCompletionDistribution.Combine(contributors);
            var second = JointCompletionDistribution.Combine(contributors);

            Assert.That(second, Is.EqualTo(first));
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
            var contributorCount = random.Next(2, 6);

            return Enumerable.Range(0, contributorCount)
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
