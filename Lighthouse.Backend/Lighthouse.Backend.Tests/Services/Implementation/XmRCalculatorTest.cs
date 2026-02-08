using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class XmRCalculatorTest
    {
        [Test]
        public void Calculate_WithEmptyBaseline_ReturnsEmptyResult()
        {
            var baseline = Array.Empty<int>();
            var display = new[] { 1, 2 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Average, Is.Zero);
                Assert.That(result.UpperNaturalProcessLimit, Is.Zero);
                Assert.That(result.LowerNaturalProcessLimit, Is.Zero);
                Assert.That(result.SpecialCauseClassifications, Has.Length.EqualTo(display.Length));
                Assert.That(result.SpecialCauseClassifications, Is.All.EqualTo(SpecialCauseType.None));
            }
        }

        [Test]
        public void Calculate_WithSingleBaselineValue_ReturnsAverageOnly()
        {
            var baseline = new[] { 5 };
            var display = new[] { 5, 8 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Average, Is.EqualTo(5.0));
                Assert.That(result.UpperNaturalProcessLimit, Is.EqualTo(5.0));
                Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo(5.0));
                Assert.That(result.SpecialCauseClassifications, Has.Length.EqualTo(2));
            }
        }

        [Test]
        public void Calculate_ComputesAverageFromBaselineOnly()
        {
            // Baseline: 10, 20, 30, 40 → average = 25
            // Display values should not affect the average
            var baseline = new[] { 10, 20, 30, 40 };
            var display = new[] { 100 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            Assert.That(result.Average, Is.EqualTo(25.0));
        }

        [Test]
        public void Calculate_ComputesMovingRangeBarFromBaseline()
        {
            // Baseline: 10, 20, 30, 40
            // Moving ranges: |20-10|=10, |30-20|=10, |40-30|=10 → MRbar = 10
            // UNPL = 25 + 2.66 * 10 = 51.6
            // LNPL = 25 - 2.66 * 10 = -1.6
            var baseline = new[] { 10, 20, 30, 40 };
            var display = new[] { 25 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Average, Is.EqualTo(25.0));
                Assert.That(result.UpperNaturalProcessLimit, Is.EqualTo(51.6).Within(0.01));
                Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo(-1.6).Within(0.01));
            }
        }

        [Test]
        public void Calculate_ClampsLnplToZero_WhenFlagSet()
        {
            // LNPL would be -1.6 but should be clamped to 0
            var baseline = new[] { 10, 20, 30, 40 };
            var display = new[] { 25 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: true);

            Assert.That(result.LowerNaturalProcessLimit, Is.Zero);
        }

        [Test]
        public void Calculate_DoesNotClampLnpl_WhenFlagNotSet()
        {
            var baseline = new[] { 10, 20, 30, 40 };
            var display = new[] { 25 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            Assert.That(result.LowerNaturalProcessLimit, Is.LessThan(0));
        }

        [Test]
        public void Calculate_AllIdenticalBaselineValues_ProducesZeroRangeLimits()
        {
            // All values identical → MRbar = 0 → UNPL = LNPL = average
            var baseline = new[] { 5, 5, 5, 5, 5 };
            var display = new[] { 5, 6 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Average, Is.EqualTo(5.0));
                Assert.That(result.UpperNaturalProcessLimit, Is.EqualTo(5.0));
                Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo(5.0));
            }
        }

        [Test]
        public void Calculate_LargeChange_PointBeyondUnpl()
        {
            // Baseline: 10, 10, 10, 10 → average=10, MRbar=0, UNPL=10, LNPL=10
            // A display point at 50 should be flagged as Large Change
            var baseline = new[] { 10, 10, 10, 10 };
            var display = new[] { 50 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            Assert.That(result.SpecialCauseClassifications[0], Is.EqualTo(SpecialCauseType.LargeChange));
        }

        [Test]
        public void Calculate_LargeChange_PointBelowLnpl()
        {
            // Baseline: 100, 110, 100, 110 → average=105, MRbar=10, UNPL=131.6, LNPL=78.4
            // A display point at 50 is below LNPL → Large Change
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 50 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            Assert.That(result.SpecialCauseClassifications[0], Is.EqualTo(SpecialCauseType.LargeChange));
        }

        [Test]
        public void Calculate_NoSpecialCause_PointWithinLimits()
        {
            // Baseline: 100, 110, 100, 110 → average=105, MRbar=10, UNPL=131.6, LNPL=78.4
            // Point at 105 is within limits
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 105 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            Assert.That(result.SpecialCauseClassifications[0], Is.EqualTo(SpecialCauseType.None));
        }

        [Test]
        public void Calculate_SmallShift_EightConsecutiveAboveAverage()
        {
            // Baseline: varied around 50 → average = 50, MRbar = 20
            // UNPL = 50+53.2=103.2, LNPL = 50-53.2=-3.2
            // 8 consecutive display points at 55 (above average, within 1σ) → Small Shift
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            var display = new[] { 55, 55, 55, 55, 55, 55, 55, 55 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            // The 8th consecutive point above average (last point) should be SmallShift
            Assert.That(result.SpecialCauseClassifications[display.Length - 1], Is.EqualTo(SpecialCauseType.SmallShift));
        }

        [Test]
        public void Calculate_SmallShift_EightConsecutiveBelowAverage()
        {
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            var display = new[] { 45, 45, 45, 45, 45, 45, 45, 45 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            Assert.That(result.SpecialCauseClassifications[display.Length - 1], Is.EqualTo(SpecialCauseType.SmallShift));
        }

        [Test]
        public void Calculate_ModerateShift_FourOfFiveBeyondOneSigma()
        {
            // Baseline: average=50, MRbar=20, 1σ=17.73
            // 1σ boundary above = 67.73
            // 4 of 5 successive display points > 67.73 → Moderate Shift
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            var display = new[] { 70, 70, 70, 50, 70 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            var lastIndex = display.Length - 1;
            Assert.That(result.SpecialCauseClassifications[lastIndex], Is.EqualTo(SpecialCauseType.ModerateShift));
        }

        [Test]
        public void Calculate_ModerateChange_TwoOfThreeBeyondTwoSigma()
        {
            // Baseline: average=50, MRbar=20, 2σ=35.46
            // 2σ boundary above = 85.46
            // 2 of 3 successive display points > 85.46 → Moderate Change
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            var display = new[] { 90, 50, 90 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            var lastIndex = display.Length - 1;
            Assert.That(result.SpecialCauseClassifications[lastIndex], Is.EqualTo(SpecialCauseType.ModerateChange));
        }

        [Test]
        public void Calculate_HigherPriorityRuleWins_LargeChangeOverModerateChange()
        {
            // A point beyond UNPL is LargeChange even if it also triggers ModerateChange
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // UNPL = 50 + 2.66*20 = 103.2
            var display = new[] { 110, 50, 110 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            var lastIndex = display.Length - 1;
            Assert.That(result.SpecialCauseClassifications[lastIndex], Is.EqualTo(SpecialCauseType.LargeChange));
        }

        [Test]
        public void Calculate_DisjointBaselineAndDisplay_UsesBaselineForStatistics()
        {
            // Baseline period (e.g. Nov 2025) completely separate from display (e.g. Jan 2026)
            var baseline = new[] { 10, 20, 30, 40 };
            // average=25, MRbar=10, UNPL=51.6, LNPL=-1.6
            var display = new[] { 25, 30, 60 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Average, Is.EqualTo(25.0));
                Assert.That(result.UpperNaturalProcessLimit, Is.EqualTo(51.6).Within(0.01));
                Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo(-1.6).Within(0.01));
                Assert.That(result.SpecialCauseClassifications, Has.Length.EqualTo(3));
                Assert.That(result.SpecialCauseClassifications[2], Is.EqualTo(SpecialCauseType.LargeChange)); // 60 > UNPL 51.6
            }
        }

        [Test]
        public void Calculate_ReturnsClassificationsForDisplayPointsOnly()
        {
            var baseline = new[] { 10, 20, 30, 40 };
            var display = new[] { 25, 30, 35, 40, 45 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            Assert.That(result.SpecialCauseClassifications, Has.Length.EqualTo(5));
        }

        [Test]
        public void Calculate_TwoBaselineValues_ComputesLimitsFromSingleMovingRange()
        {
            // Two baseline values: 10, 20 → average=15, MRbar=10
            // UNPL = 15 + 2.66*10 = 41.6
            // LNPL = 15 - 2.66*10 = -11.6
            var baseline = new[] { 10, 20 };
            var display = new[] { 15 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Average, Is.EqualTo(15.0));
                Assert.That(result.UpperNaturalProcessLimit, Is.EqualTo(41.6).Within(0.01));
                Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo(-11.6).Within(0.01));
            }
        }

        [Test]
        public void Calculate_ClassificationsAppliedToDisplayValues()
        {
            // Identical display values at average → all None
            var baseline = new[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            var display = new[] { 10, 10, 10, 10, 10 };

            var result = XmRCalculator.Calculate(baseline, display, clampLnplToZero: false);

            foreach (var classification in result.SpecialCauseClassifications)
            {
                Assert.That(classification, Is.EqualTo(SpecialCauseType.None));
            }
        }
    }
}
