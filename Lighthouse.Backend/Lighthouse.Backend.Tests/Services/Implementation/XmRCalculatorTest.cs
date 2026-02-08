using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class XmRCalculatorTest
    {
        private static void AssertNoCauses(IReadOnlyList<IReadOnlyList<SpecialCauseType>> classifications, int index)
        {
            Assert.That(classifications[index], Is.Empty,
                $"Expected no causes at index {index} but found [{string.Join(", ", classifications[index])}]");
        }

        private static void AssertHasCause(IReadOnlyList<IReadOnlyList<SpecialCauseType>> classifications, int index, SpecialCauseType expected)
        {
            Assert.That(classifications[index], Does.Contain(expected),
                $"Expected {expected} at index {index} but found [{string.Join(", ", classifications[index])}]");
        }

        private static void AssertDoesNotHaveCause(IReadOnlyList<IReadOnlyList<SpecialCauseType>> classifications, int index, SpecialCauseType expected)
        {
            Assert.That(classifications[index], Does.Not.Contain(expected),
                $"Expected no {expected} at index {index} but found [{string.Join(", ", classifications[index])}]");
        }

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
                Assert.That(result.SpecialCauseClassifications, Has.Count.EqualTo(display.Length));
                Assert.That(result.SpecialCauseClassifications[0], Is.Empty);
                Assert.That(result.SpecialCauseClassifications[1], Is.Empty);
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
                Assert.That(result.SpecialCauseClassifications, Has.Count.EqualTo(2));
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
                Assert.That(result.LowerNaturalProcessLimit, Is.Zero);
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
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 0, SpecialCauseType.LargeChange);
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
                AssertDoesNotHaveCause(result.SpecialCauseClassifications, i, SpecialCauseType.ModerateChange);
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
                AssertDoesNotHaveCause(result.SpecialCauseClassifications, i, SpecialCauseType.ModerateShift);
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

            // All 8 points should be tagged SmallShift
            for (var i = 0; i < display.Length; i++)
            {
                AssertHasCause(result.SpecialCauseClassifications, i, SpecialCauseType.SmallShift);
            }
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

            AssertHasCause(result.SpecialCauseClassifications, 0, SpecialCauseType.LargeChange);
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

            AssertHasCause(result.SpecialCauseClassifications, 0, SpecialCauseType.LargeChange);
        }

        [Test]
        public void Calculate_NoSpecialCause_PointWithinLimits()
        {
            // Baseline: 100, 110, 100, 110 → average=105, MRbar=10, UNPL=131.6, LNPL=78.4
            // Point at 105 is within limits
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 105 };

            var result = XmRCalculator.Calculate(baseline, display);

            AssertNoCauses(result.SpecialCauseClassifications, 0);
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

            // All 8 points in the run should be tagged SmallShift
            for (var i = 0; i < display.Length; i++)
            {
                AssertHasCause(result.SpecialCauseClassifications, i, SpecialCauseType.SmallShift);
            }
        }

        [Test]
        public void Calculate_SmallShift_EightConsecutiveBelowAverage()
        {
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            var display = new[] { 45, 45, 45, 45, 45, 45, 45, 45 };

            var result = XmRCalculator.Calculate(baseline, display);

            // All 8 points in the run should be tagged SmallShift
            for (var i = 0; i < display.Length; i++)
            {
                AssertHasCause(result.SpecialCauseClassifications, i, SpecialCauseType.SmallShift);
            }
        }

        [Test]
        public void Calculate_SmallShift_NinePointsTagsFirstNinePoints()
        {
            // 9 points all above average → windows [0..7] and [1..8] both qualify
            // All 9 points should be SmallShift
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            var display = new[] { 55, 55, 55, 55, 55, 55, 55, 55, 55 };

            var result = XmRCalculator.Calculate(baseline, display);

            for (var i = 0; i < display.Length; i++)
            {
                AssertHasCause(result.SpecialCauseClassifications, i, SpecialCauseType.SmallShift);
            }
        }

        [Test]
        public void Calculate_SmallShift_PointOnAverageBreaksRun()
        {
            // Average = 50. A point exactly at 50 is on neither side → breaks the run
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // 7 points above, then exactly average, then 7 more above → no window of 8 qualifies
            var display = new[] { 55, 55, 55, 55, 55, 55, 55, 50, 55, 55, 55, 55, 55, 55, 55 };

            var result = XmRCalculator.Calculate(baseline, display);

            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 0, SpecialCauseType.SmallShift);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 7, SpecialCauseType.SmallShift);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 14, SpecialCauseType.SmallShift);
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

            // Only points ABOVE average should be marked
            // Index 3 (value 50 = average) should NOT be marked
            AssertHasCause(result.SpecialCauseClassifications, 0, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 1, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 2, SpecialCauseType.ModerateShift);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 3, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 4, SpecialCauseType.ModerateShift);
        }

        [Test]
        public void Calculate_ModerateShift_OnlyThreeOfFiveAboveDoesNotTrigger()
        {
            // 3 of 5 above 1σ is not enough
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // avg=50, 1σ upper=67.73
            var display = new[] { 70, 50, 70, 50, 70 };

            var result = XmRCalculator.Calculate(baseline, display);

            for (var i = 0; i < display.Length; i++)
            {
                AssertDoesNotHaveCause(result.SpecialCauseClassifications, i, SpecialCauseType.ModerateShift);
            }
        }

        [Test]
        public void Calculate_ModerateShift_MixedSidesDoNotTrigger()
        {
            // 2 above 1σ upper and 2 below 1σ lower in the same window → does not trigger
            // because the sides are separate
            // Baseline: 100, 110, 100, 110 → avg=105, MRbar=10, σ=8.865
            // 1σ upper = 113.87, 1σ lower = 96.13
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 115, 90, 115, 90, 115 };

            var result = XmRCalculator.Calculate(baseline, display);

            // Each side only has 2 or 3 of 5 — not 4, so no ModerateShift
            for (var i = 0; i < display.Length; i++)
            {
                AssertDoesNotHaveCause(result.SpecialCauseClassifications, i, SpecialCauseType.ModerateShift);
            }
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

            // Only points ABOVE average should be marked (indices 0 and 2)
            // Index 1 (value 50 = average) should NOT be marked
            AssertHasCause(result.SpecialCauseClassifications, 0, SpecialCauseType.ModerateChange);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 1, SpecialCauseType.ModerateChange);
            AssertHasCause(result.SpecialCauseClassifications, 2, SpecialCauseType.ModerateChange);
        }

        [Test]
        public void Calculate_ModerateChange_MixedSidesDoNotTrigger()
        {
            // 1 above 2σ upper and 1 below 2σ lower in the same window → does not trigger
            // because sides are counted separately
            // Baseline: 100, 110, 100, 110 → avg=105, MRbar=10, σ=8.865
            // 2σ upper = 122.73, 2σ lower = 87.27
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 125, 105, 80 };

            var result = XmRCalculator.Calculate(baseline, display);

            // Above: only 1 of 3 > 122.73; Below: only 1 of 3 < 87.27 → no ModerateChange
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 2, SpecialCauseType.ModerateChange);
        }

        [Test]
        public void Calculate_MultipleCauses_LargeChangeAndModerateChange()
        {
            // A point beyond UNPL is LargeChange AND if it sits in a 2-of-3 window it also gets ModerateChange
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // avg=50, MRbar=20, UNPL=103.2, 2σ upper=85.46
            // Points: 90, 50, 110 → point at 110 > UNPL (LargeChange)
            // Window [90, 50, 110]: 2 of 3 (90, 110) > 85.46 → ModerateChange for all 3
            var display = new[] { 90, 50, 110 };

            var result = XmRCalculator.Calculate(baseline, display);

            // Index 2 should have both LargeChange and ModerateChange
            AssertHasCause(result.SpecialCauseClassifications, 2, SpecialCauseType.LargeChange);
            AssertHasCause(result.SpecialCauseClassifications, 2, SpecialCauseType.ModerateChange);
            // Index 0 should have ModerateChange (part of the window)
            AssertHasCause(result.SpecialCauseClassifications, 0, SpecialCauseType.ModerateChange);
        }

        [Test]
        public void Calculate_MultipleCauses_SmallShiftAndModerateShift()
        {
            // A long run of high values can trigger both SmallShift and ModerateShift
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // avg=50, MRbar=20, 1σ upper=67.73
            // 8 points at 70 (all > 1σ upper=67.73 and all > avg=50)
            var display = new[] { 70, 70, 70, 70, 70, 70, 70, 70 };

            var result = XmRCalculator.Calculate(baseline, display);

            for (var i = 0; i < display.Length; i++)
            {
                AssertHasCause(result.SpecialCauseClassifications, i, SpecialCauseType.SmallShift);
                AssertHasCause(result.SpecialCauseClassifications, i, SpecialCauseType.ModerateShift);
            }
        }

        [Test]
        public void Calculate_HigherPriorityRuleWins_LargeChangeOverModerateChange()
        {
            // A point beyond UNPL is LargeChange even if it also triggers ModerateChange
            // Both causes should be present
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // UNPL = 50 + 2.66*20 = 103.2
            var display = new[] { 110, 50, 110 };

            var result = XmRCalculator.Calculate(baseline, display);

            var lastIndex = display.Length - 1;
            AssertHasCause(result.SpecialCauseClassifications, lastIndex, SpecialCauseType.LargeChange);
            // Also ModerateChange since 2 of 3 > 2σ upper (85.46)
            AssertHasCause(result.SpecialCauseClassifications, lastIndex, SpecialCauseType.ModerateChange);
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
                Assert.That(result.SpecialCauseClassifications, Has.Count.EqualTo(3));
                AssertHasCause(result.SpecialCauseClassifications, 2, SpecialCauseType.LargeChange); // 170 > UNPL 163.2
            }
        }

        [Test]
        public void Calculate_ReturnsClassificationsForDisplayPointsOnly()
        {
            var baseline = new[] { 10, 20, 30, 40 };
            var display = new[] { 25, 30, 35, 40, 45 };

            var result = XmRCalculator.Calculate(baseline, display);

            Assert.That(result.SpecialCauseClassifications, Has.Count.EqualTo(5));
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
                Assert.That(classification, Is.Empty);
            }
        }

        [Test]
        public void Calculate_Rule2_OnlyMarksPointsAboveAverage()
        {
            // Rule 2: 2 of 3 successive beyond 2σ on same side
            // avg=50, MRbar=20, 2σ upper=85.46
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // display: [50, 90, 50, 90, 50]
            // Window [1,2,3]: 90,50,90 → 2 of 3 > 85.46
            // Only indices 1 and 3 (both > average) should be marked
            // Index 2 (value 50 = average) should NOT be marked
            var display = new[] { 50, 90, 50, 90, 50 };

            var result = XmRCalculator.Calculate(baseline, display);

            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 0, SpecialCauseType.ModerateChange);
            AssertHasCause(result.SpecialCauseClassifications, 1, SpecialCauseType.ModerateChange);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 2, SpecialCauseType.ModerateChange);
            AssertHasCause(result.SpecialCauseClassifications, 3, SpecialCauseType.ModerateChange);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 4, SpecialCauseType.ModerateChange);
        }

        [Test]
        public void Calculate_Rule3_OnlyMarksPointsAboveAverage()
        {
            // Rule 3: 4 of 5 successive beyond 1σ on same side
            // avg=50, MRbar=20, 1σ upper=67.73
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // display: [50, 70, 70, 70, 50, 70, 50]
            // Window [1,2,3,4,5]: 70,70,70,50,70 → 4 of 5 > 67.73
            // Only indices 1,2,3,5 (all > average) should be marked
            // Index 4 (value 50 = average) should NOT be marked
            var display = new[] { 50, 70, 70, 70, 50, 70, 50 };

            var result = XmRCalculator.Calculate(baseline, display);

            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 0, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 1, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 2, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 3, SpecialCauseType.ModerateShift);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 4, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 5, SpecialCauseType.ModerateShift);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 6, SpecialCauseType.ModerateShift);
        }

        [Test]
        public void Calculate_Rule4_AllEightPointsInRunAreMarked()
        {
            // Rule 4: 8 successive on same side
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // avg=50, 9 points above: first 8 form a run, 9th extends it
            var display = new[] { 45, 55, 55, 55, 55, 55, 55, 55, 55 };

            var result = XmRCalculator.Calculate(baseline, display);

            // Point 0 is below average → not part of any SmallShift
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 0, SpecialCauseType.SmallShift);
            // Points 1-8: 8 above average → all tagged by window [1..8]
            for (var i = 1; i <= 8; i++)
            {
                AssertHasCause(result.SpecialCauseClassifications, i, SpecialCauseType.SmallShift);
            }
        }

        [Test]
        public void Calculate_Rule2Below_TwoOfThreeBeyondTwoSigmaLower()
        {
            // Baseline with positive LNPL: 100, 110, 100, 110 → avg=105, MRbar=10, σ=8.865
            // 2σ lower = 105 - 17.73 = 87.27
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 80, 100, 80 };

            var result = XmRCalculator.Calculate(baseline, display);

            // 2 of 3 < 87.27 → all three marked
            for (var i = 0; i < display.Length; i++)
            {
                AssertHasCause(result.SpecialCauseClassifications, i, SpecialCauseType.ModerateChange);
            }
        }

        [Test]
        public void Calculate_Rule3Below_FourOfFiveBeyondOneSigmaLower()
        {
            // Baseline with positive LNPL: 100, 110, 100, 110 → avg=105, MRbar=10, σ=8.865
            // 1σ lower = 105 - 8.865 = 96.14
            var baseline = new[] { 100, 110, 100, 110 };
            var display = new[] { 90, 90, 90, 100, 90 };

            var result = XmRCalculator.Calculate(baseline, display);

            // 4 of 5 < 96.14 → all five marked
            for (var i = 0; i < display.Length; i++)
            {
                AssertHasCause(result.SpecialCauseClassifications, i, SpecialCauseType.ModerateShift);
            }
        }

        [Test]
        public void Calculate_PointExactlyOnSigmaLine_DoesNotCount()
        {
            // Baseline: all 100 → avg=100, MRbar=0, all sigma lines = 100, UNPL=LNPL=100
            // σ=0. 1σ and 2σ lines are all at 100.
            // A point at exactly 100 should NOT trigger Rule 2 or 3 (strict >)
            var baseline = new[] { 100, 100, 100, 100 };
            var display = new[] { 100, 100, 100, 100, 100 };

            var result = XmRCalculator.Calculate(baseline, display);

            for (var i = 0; i < display.Length; i++)
            {
                AssertNoCauses(result.SpecialCauseClassifications, i);
            }
        }

        [Test]
        public void Calculate_CausesAreSortedByEnumValue()
        {
            // A point that has multiple causes should have them sorted by enum value
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // avg=50, UNPL=103.2, 2σ upper=85.46
            var display = new[] { 110, 50, 110 };

            var result = XmRCalculator.Calculate(baseline, display);

            // Index 2: LargeChange (1) and ModerateChange (2) — should be in enum order
            var causes = result.SpecialCauseClassifications[2];
            Assert.That(causes, Has.Count.GreaterThanOrEqualTo(2));
            for (var i = 1; i < causes.Count; i++)
            {
                Assert.That(causes[i], Is.GreaterThanOrEqualTo(causes[i - 1]));
            }
        }

        [Test]
        public void Calculate_ModerateChange_MixedSidesWithSufficientCounts_DoesNotTrigger()
        {
            // Even if we have 2+ points beyond 2σ in a window, if they're on opposite sides, no signal
            // Baseline: 100, 110, 100, 110 → avg=105, MRbar=10, σ=8.865
            // 2σ upper = 122.73, 2σ lower = 87.27
            var baseline = new[] { 100, 110, 100, 110 };
            // Window has 1 point way above and 1 point way below - opposite sides
            var display = new[] { 125, 105, 80 };

            var result = XmRCalculator.Calculate(baseline, display);

            // Should NOT trigger because qualifying points are on opposite sides
            for (var i = 0; i < display.Length; i++)
            {
                AssertDoesNotHaveCause(result.SpecialCauseClassifications, i, SpecialCauseType.ModerateChange);
            }
        }

        [Test]
        public void Calculate_ModerateShift_MixedSidesWithSufficientCounts_DoesNotTrigger()
        {
            // 4+ points beyond 1σ but on opposite sides → no signal
            // Baseline: 100, 110, 100, 110 → avg=105, MRbar=10, σ=8.865
            // 1σ upper = 113.87, 1σ lower = 96.13
            var baseline = new[] { 100, 110, 100, 110 };
            // 2 high, 2 low, 1 middle = 4 beyond 1σ but mixed sides
            var display = new[] { 115, 90, 115, 90, 105 };

            var result = XmRCalculator.Calculate(baseline, display);

            // Should NOT trigger because qualifying points are on opposite sides
            for (var i = 0; i < display.Length; i++)
            {
                AssertDoesNotHaveCause(result.SpecialCauseClassifications, i, SpecialCauseType.ModerateShift);
            }
        }

        [Test]
        public void Calculate_ModerateChange_OnlyMarksPointsOnCorrectSide()
        {
            // When 2 of 3 are beyond 2σ upper, only mark points above average (not the middle point)
            // Baseline: avg=50, MRbar=20, 2σ upper=85.46
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // Window: [90, 45, 90] → 2 points beyond 2σ, but middle point is below average
            var display = new[] { 90, 45, 90 };

            var result = XmRCalculator.Calculate(baseline, display);

            // Indices 0 and 2 should be marked (above average)
            AssertHasCause(result.SpecialCauseClassifications, 0, SpecialCauseType.ModerateChange);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 1, SpecialCauseType.ModerateChange);
            AssertHasCause(result.SpecialCauseClassifications, 2, SpecialCauseType.ModerateChange);
        }

        [Test]
        public void Calculate_ModerateShift_OnlyMarksPointsOnCorrectSide()
        {
            // When 4 of 5 are beyond 1σ upper, only mark points above average
            // Baseline: avg=50, MRbar=20, 1σ upper=67.73
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            // Window: [70, 70, 70, 45, 70] → 4 beyond 1σ, but index 3 is below average
            var display = new[] { 70, 70, 70, 45, 70 };

            var result = XmRCalculator.Calculate(baseline, display);

            // Indices 0,1,2,4 should be marked (above average)
            AssertHasCause(result.SpecialCauseClassifications, 0, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 1, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 2, SpecialCauseType.ModerateShift);
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 3, SpecialCauseType.ModerateShift);
            AssertHasCause(result.SpecialCauseClassifications, 4, SpecialCauseType.ModerateShift);
        }

        [Test]
        public void Calculate_SmallShift_PointExactlyOnAverageBreaksRun_Fixed()
        {
            // This test should now pass with the corrected logic
            var baseline = new[] { 40, 60, 40, 60, 40, 60, 40, 60 };
            var display = new[] { 55, 55, 55, 55, 55, 55, 55, 50, 55, 55, 55, 55, 55, 55, 55 };

            var result = XmRCalculator.Calculate(baseline, display);

            // First 7 points before the break
            for (var i = 0; i < 7; i++)
            {
                AssertDoesNotHaveCause(result.SpecialCauseClassifications, i, SpecialCauseType.SmallShift);
            }

            // Point on average
            AssertDoesNotHaveCause(result.SpecialCauseClassifications, 7, SpecialCauseType.SmallShift);

            // Last 7 points after the break
            for (var i = 8; i < 15; i++)
            {
                AssertDoesNotHaveCause(result.SpecialCauseClassifications, i, SpecialCauseType.SmallShift);
            }
        }
    }
}
