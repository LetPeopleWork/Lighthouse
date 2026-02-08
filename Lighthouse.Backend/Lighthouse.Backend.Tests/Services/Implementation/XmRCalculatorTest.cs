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

            var result = XmRCalculator.Calculate(baseline, display);

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

            var result = XmRCalculator.Calculate(baseline, display);

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

            var result = XmRCalculator.Calculate(baseline, display);

            Assert.That(result.Average, Is.EqualTo(25.0));
        }

        [Test]
        public void Calculate_ComputesMovingRangeBarFromBaseline()
        {
            // Baseline: 10, 20, 30, 40
            // Moving ranges: |20-10|=10, |30-20|=10, |40-30|=10 → MRbar = 10
            // UNPL = 25 + 2.66 * 10 = 51.6
            // LNPL = 25 - 2.66 * 10 = -1.6 → clamped to 0 (zero-bounded)
            var baseline = new[] { 10, 20, 30, 40 };
            var display = new[] { 25 };

            var result = XmRCalculator.Calculate(baseline, display);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Average, Is.EqualTo(25.0));
                Assert.That(result.UpperNaturalProcessLimit, Is.EqualTo(51.6).Within(0.01));
                Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo(0));
            }
        }

        [Test]
        public void Calculate_ZeroBoundedData_ClampsLnplToZero()
        {
            // All baseline values >= 0 → zero-bounded → LNPL clamped to 0
            var baseline = new[] { 10, 20, 30, 40 };
            var display = new[] { 25 };

            var result = XmRCalculator.Calculate(baseline, display);

            Assert.That(result.LowerNaturalProcessLimit, Is.Zero);
        }

        [Test]
        public void Calculate_ZeroBoundedData_LnplNotNegative()
        {
            // When LNPL naturally positive, it stays as-is
            // Baseline: 100, 110, 100, 110 → avg=105, MRbar=10, LNPL=78.4
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 105 };

            var result = XmRCalculator.Calculate(baseline, display);

            Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo(78.4).Within(0.01));
        }

        [Test]
        public void Calculate_ZeroBoundedData_DisablesRule1Below_NoLargeChangeForLowValues()
        {
            // Zero-bounded: LNPL is clamped to 0 → Rule 1 below is invalid
            // Baseline: 5, 15, 5, 15 → avg=10, MRbar=10, LNPL=10-26.6=-16.6 → clamped to 0
            var baseline = new[] { 5, 15, 5, 15 };
            var display = new[] { 0 };

            var result = XmRCalculator.Calculate(baseline, display);

            // LNPL is clamped to 0, and rule 1 below is disabled → 0 is NOT LargeChange
            Assert.That(result.SpecialCauseClassifications[0], Is.Not.EqualTo(SpecialCauseType.LargeChange));
        }

        [Test]
        public void Calculate_ZeroBoundedData_DisablesRule2Below_NoModerateChangeBelowTwoSigma()
        {
            // Zero-bounded: 2σ lower clamped to 0 → Rule 2 below is invalid
            // Baseline: 5, 15, 5, 15, 5, 15, 5, 15 → avg=10, MRbar=10, σ=8.865
            // 2σ lower = 10 - 17.73 = -7.73 → clamped to 0
            var baseline = new[] { 5, 15, 5, 15, 5, 15, 5, 15 };
            var display = new[] { 0, 0, 0 };

            var result = XmRCalculator.Calculate(baseline, display);

            // With 2σ lower invalid, these low points should NOT be ModerateChange
            for (var i = 0; i < display.Length; i++)
            {
                Assert.That(result.SpecialCauseClassifications[i], Is.Not.EqualTo(SpecialCauseType.ModerateChange));
            }
        }

        [Test]
        public void Calculate_ZeroBoundedData_DisablesRule3Below_NoModerateShiftBelowOneSigma()
        {
            // Zero-bounded: 1σ lower clamped to 0 → Rule 3 below is invalid
            // Baseline: 1, 21, 1, 21, 1, 21, 1, 21 → avg=11, MRbar=20, σ=17.73
            // 1σ lower = 11 - 17.73 = -6.73 → clamped to 0
            var baseline = new[] { 1, 21, 1, 21, 1, 21, 1, 21 };
            var display = new[] { 1, 1, 1, 1, 1 };

            var result = XmRCalculator.Calculate(baseline, display);

            // With 1σ lower invalid, low points should NOT be ModerateShift
            for (var i = 0; i < display.Length; i++)
            {
                Assert.That(result.SpecialCauseClassifications[i], Is.Not.EqualTo(SpecialCauseType.ModerateShift));
            }
        }

        [Test]
        public void Calculate_ZeroBoundedData_Rule4SmallShiftStillValid()
        {
            // Rule 4 (SmallShift) depends on average which is never zero for flow data
            // 8 consecutive points below average should still trigger SmallShift
            var baseline = new[] { 1, 21, 1, 21, 1, 21, 1, 21 };
            // avg=11
            var display = new[] { 5, 5, 5, 5, 5, 5, 5, 5 };

            var result = XmRCalculator.Calculate(baseline, display);

            Assert.That(result.SpecialCauseClassifications[display.Length - 1], Is.EqualTo(SpecialCauseType.SmallShift));
        }

        [Test]
        public void Calculate_AllIdenticalBaselineValues_ProducesZeroRangeLimits()
        {
            // All values identical → MRbar = 0 → UNPL = LNPL = average
            var baseline = new[] { 5, 5, 5, 5, 5 };
            var display = new[] { 5, 6 };

            var result = XmRCalculator.Calculate(baseline, display);

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

            var result = XmRCalculator.Calculate(baseline, display);

            Assert.That(result.SpecialCauseClassifications[0], Is.EqualTo(SpecialCauseType.LargeChange));
        }

        [Test]
        public void Calculate_LargeChange_PointBelowLnpl()
        {
            // Baseline: 100, 110, 100, 110 → average=105, MRbar=10, UNPL=131.6, LNPL=78.4
            // LNPL is 78.4 (positive, not clamped) → Rule 1 below is valid
            // A display point at 50 is below LNPL → Large Change
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 50 };

            var result = XmRCalculator.Calculate(baseline, display);

            Assert.That(result.SpecialCauseClassifications[0], Is.EqualTo(SpecialCauseType.LargeChange));
        }

        [Test]
        public void Calculate_NoSpecialCause_PointWithinLimits()
        {
            // Baseline: 100, 110, 100, 110 → average=105, MRbar=10, UNPL=131.6, LNPL=78.4
            // Point at 105 is within limits
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 105 };

            var result = XmRCalculator.Calculate(baseline, display);

            Assert.That(result.SpecialCauseClassifications[0], Is.EqualTo(SpecialCauseType.None));
        }

        [Test]
        public void Calculate_SmallShift_EightConsecutiveAboveAverage()
        {
            // Baseline: varied around 50 → average = 50, MRbar = 20
            // UNPL = 50+53.2=103.2, LNPL = 50-53.2=-3.2 → clamped to 0
            // 8 consecutive display points at 55 (above average, within 1σ) → Small Shift
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            var display = new[] { 55, 55, 55, 55, 55, 55, 55, 55 };

            var result = XmRCalculator.Calculate(baseline, display);

            // The 8th consecutive point above average (last point) should be SmallShift
            Assert.That(result.SpecialCauseClassifications[display.Length - 1], Is.EqualTo(SpecialCauseType.SmallShift));
        }

        [Test]
        public void Calculate_SmallShift_EightConsecutiveBelowAverage()
        {
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            var display = new[] { 45, 45, 45, 45, 45, 45, 45, 45 };

            var result = XmRCalculator.Calculate(baseline, display);

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

            var result = XmRCalculator.Calculate(baseline, display);

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

            var result = XmRCalculator.Calculate(baseline, display);

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

            var result = XmRCalculator.Calculate(baseline, display);

            var lastIndex = display.Length - 1;
            Assert.That(result.SpecialCauseClassifications[lastIndex], Is.EqualTo(SpecialCauseType.LargeChange));
        }

        [Test]
        public void Calculate_DisjointBaselineAndDisplay_UsesBaselineForStatistics()
        {
            // Baseline period completely separate from display
            // Baseline: 100, 120, 100, 120 → average=110, MRbar=20
            // UNPL = 110 + 53.2 = 163.2
            // LNPL = 110 - 53.2 = 56.8 (positive, not clamped)
            var baseline = new[] { 100, 120, 100, 120 };
            var display = new[] { 110, 115, 170 };

            var result = XmRCalculator.Calculate(baseline, display);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Average, Is.EqualTo(110.0));
                Assert.That(result.UpperNaturalProcessLimit, Is.EqualTo(163.2).Within(0.01));
                Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo(56.8).Within(0.01));
                Assert.That(result.SpecialCauseClassifications, Has.Length.EqualTo(3));
                Assert.That(result.SpecialCauseClassifications[2], Is.EqualTo(SpecialCauseType.LargeChange)); // 170 > UNPL 163.2
            }
        }

        [Test]
        public void Calculate_ReturnsClassificationsForDisplayPointsOnly()
        {
            var baseline = new[] { 10, 20, 30, 40 };
            var display = new[] { 25, 30, 35, 40, 45 };

            var result = XmRCalculator.Calculate(baseline, display);

            Assert.That(result.SpecialCauseClassifications, Has.Length.EqualTo(5));
        }

        [Test]
        public void Calculate_TwoBaselineValues_ComputesLimitsFromSingleMovingRange()
        {
            // Two baseline values: 100, 110 → average=105, MRbar=10
            // UNPL = 105 + 2.66*10 = 131.6
            // LNPL = 105 - 2.66*10 = 78.4 (positive, not clamped)
            var baseline = new[] { 100, 110 };
            var display = new[] { 105 };

            var result = XmRCalculator.Calculate(baseline, display);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Average, Is.EqualTo(105.0));
                Assert.That(result.UpperNaturalProcessLimit, Is.EqualTo(131.6).Within(0.01));
                Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo(78.4).Within(0.01));
            }
        }

        [Test]
        public void Calculate_ClassificationsAppliedToDisplayValues()
        {
            // Identical display values at average → all None
            var baseline = new[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
            var display = new[] { 10, 10, 10, 10, 10 };

            var result = XmRCalculator.Calculate(baseline, display);

            foreach (var classification in result.SpecialCauseClassifications)
            {
                Assert.That(classification, Is.EqualTo(SpecialCauseType.None));
            }
        }
    }
}
