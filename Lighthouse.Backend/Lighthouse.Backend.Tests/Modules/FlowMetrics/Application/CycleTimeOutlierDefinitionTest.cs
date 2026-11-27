using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application
{
    /// <summary>
    /// The default outlier rule: a shown item is an outlier when its Cycle Time is above the upper natural process limit
    /// of the baseline, and a baseline of fewer than 2 Cycle Times has no limit.
    /// </summary>
    public class CycleTimeOutlierDefinitionTest
    {
        /// <summary>Average 20.5, average moving range 25/7: the upper limit is exactly 30 days.</summary>
        private static readonly int[] BaselineWithALimitOf30 = [18, 13, 12, 24, 26, 25, 25, 21];

        private static readonly int[] TheItemsAbove30 = [103, 104];

        private static readonly CycleTimeObservation[] ShownAround30 =
        [
            new(101, 12),
            new(102, 30),
            new(103, 31),
            new(104, 90),
        ];

        [TestCase(new int[0])]
        [TestCase(new[] { 40 })]
        public void A_baseline_of_fewer_than_2_Cycle_Times_has_no_limits(int[] baseline)
        {
            var verdict = new UpperNaturalProcessLimitOutlierDefinition().Judge(baseline, ShownAround30);

            Assert.That(verdict, Is.TypeOf<OutlierVerdict.NoLimits>(),
                $"WHAT: a baseline of {baseline.Length} Cycle Times gave {verdict}. " +
                "WHY: with fewer than 2 values there is no moving range, so the process behaviour chart has no limit either. " +
                "HOW: answer NoLimits below 2 baseline Cycle Times.");
        }

        [Test]
        public void A_baseline_of_exactly_2_Cycle_Times_has_a_limit()
        {
            var verdict = new UpperNaturalProcessLimitOutlierDefinition().Judge([10, 12], ShownAround30);

            Assert.That(verdict, Is.TypeOf<OutlierVerdict.Limited>(),
                $"WHAT: a baseline of 2 Cycle Times gave {verdict}. WHY: 2 values give one moving range and so a limit. " +
                "HOW: answer NoLimits only below 2 baseline Cycle Times.");
        }

        [Test]
        public void Only_items_above_the_limit_are_outliers_and_an_item_on_the_limit_is_not()
        {
            var verdict = new UpperNaturalProcessLimitOutlierDefinition().Judge(BaselineWithALimitOf30, ShownAround30);

            var limited = verdict as OutlierVerdict.Limited;
            Assert.That(limited, Is.Not.Null, $"WHAT: the verdict is {verdict}. WHY: 8 baseline Cycle Times give a limit. HOW: answer Limited.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(limited!.UpperNaturalProcessLimitDays, Is.EqualTo(30.0).Within(1e-9),
                    "WHAT: the limit is not 30 days. WHY: the list title states the same limit the process behaviour chart draws. " +
                    "HOW: take the limit from XmRCalculator.UpperNaturalProcessLimitOf over the baseline.");
                Assert.That(limited.OutlierWorkItemIds, Is.EqualTo(TheItemsAbove30),
                    "WHAT: the outliers are not exactly the 31-day and 90-day items, in the order shown. " +
                    "WHY: an outlier is strictly above the limit; the 30-day item sits on it. HOW: keep the items whose Cycle Time exceeds the limit.");
            }
        }
    }
}
