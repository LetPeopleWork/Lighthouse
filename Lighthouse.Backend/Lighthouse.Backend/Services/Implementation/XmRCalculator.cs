namespace Lighthouse.Backend.Services.Implementation
{
    public enum SpecialCauseType
    {
        None,
        LargeChange,
        ModerateChange,
        ModerateShift,
        SmallShift,
    }

    public record XmRResult(
        double Average,
        double UpperNaturalProcessLimit,
        double LowerNaturalProcessLimit,
        SpecialCauseType[] SpecialCauseClassifications);

    public static class XmRCalculator
    {
        private const double MovingRangeMultiplier = 2.66;

        public static XmRResult Calculate(int[] baselineValues, int[] displayValues)
        {
            if (baselineValues.Length == 0)
            {
                var emptyClassifications = new SpecialCauseType[displayValues.Length];
                Array.Fill(emptyClassifications, SpecialCauseType.None);
                return new XmRResult(0, 0, 0, emptyClassifications);
            }

            var average = baselineValues.Average();

            if (baselineValues.Length < 2)
            {
                var classifications = new SpecialCauseType[displayValues.Length];
                Array.Fill(classifications, SpecialCauseType.None);
                return new XmRResult(average, average, average, classifications);
            }

            var movingRanges = new double[baselineValues.Length - 1];
            for (var i = 1; i < baselineValues.Length; i++)
            {
                movingRanges[i - 1] = Math.Abs(baselineValues[i] - baselineValues[i - 1]);
            }

            var mrBar = movingRanges.Average();

            var unpl = average + MovingRangeMultiplier * mrBar;
            var lnpl = average - MovingRangeMultiplier * mrBar;

            // Zero-bounded data: clamp negative lower sigma lines to zero.
            // When a sigma line is zero it is considered not drawn and
            // any detection rule that depends on it becomes invalid.
            lnpl = lnpl < 0 ? 0 : lnpl;

            var sigma = mrBar / 1.128;

            var specialCauseClassifications = ClassifyAllPoints(
                displayValues, average, unpl, lnpl, sigma);

            return new XmRResult(average, unpl, lnpl, specialCauseClassifications);
        }

        private static SpecialCauseType[] ClassifyAllPoints(
            int[] values,
            double average,
            double unpl,
            double lnpl,
            double sigma)
        {
            var oneSigmaUpper = average + sigma;
            var twoSigmaUpper = average + 2 * sigma;

            var oneSigmaLower = average - sigma;
            var twoSigmaLower = average - 2 * sigma;

            twoSigmaLower = twoSigmaLower < 0 ? 0 : twoSigmaLower;
            oneSigmaLower = oneSigmaLower < 0 ? 0 : oneSigmaLower;

            // A sigma line at zero is not drawn → the below-side detection rule is invalid
            var isLnplValid = lnpl > 0;
            var isTwoSigmaLowerValid = twoSigmaLower > 0;
            var isOneSigmaLowerValid = oneSigmaLower > 0;

            var classifications = new SpecialCauseType[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                // Rule 1: Large Change — point beyond UNPL or below LNPL (highest priority)
                if (values[i] > unpl || (isLnplValid && values[i] < lnpl))
                {
                    classifications[i] = SpecialCauseType.LargeChange;
                    continue;
                }

                // Rule 2: Moderate Change — 2 of 3 successive points beyond 2σ on same side
                if (IsModerateChange(values, i, twoSigmaUpper, twoSigmaLower, isTwoSigmaLowerValid))
                {
                    classifications[i] = SpecialCauseType.ModerateChange;
                    continue;
                }

                // Rule 3: Moderate Shift — 4 of 5 successive points beyond 1σ on same side
                if (IsModerateShift(values, i, oneSigmaUpper, oneSigmaLower, isOneSigmaLowerValid))
                {
                    classifications[i] = SpecialCauseType.ModerateShift;
                    continue;
                }

                // Rule 4: Small Shift — 8+ successive points on the same side of average
                // Average is never zero for flow data, so this rule is always valid
                if (IsSmallShift(values, i, average))
                {
                    classifications[i] = SpecialCauseType.SmallShift;
                    continue;
                }

                classifications[i] = SpecialCauseType.None;
            }

            return classifications;
        }

        private static bool IsModerateChange(int[] values, int index, double twoSigmaUpper, double twoSigmaLower, bool isTwoSigmaLowerValid)
        {
            if (index < 2)
            {
                return false;
            }

            // Check above: 2 of 3 successive points beyond 2σ above average
            var countAbove = 0;
            for (var j = index - 2; j <= index; j++)
            {
                if (values[j] > twoSigmaUpper)
                {
                    countAbove++;
                }
            }

            if (countAbove >= 2)
            {
                return true;
            }

            if (!isTwoSigmaLowerValid)
            {
                return false;
            }

            // Check below: 2 of 3 successive points beyond 2σ below average
            var countBelow = 0;
            for (var j = index - 2; j <= index; j++)
            {
                if (values[j] < twoSigmaLower)
                {
                    countBelow++;
                }
            }

            return countBelow >= 2;
        }

        private static bool IsModerateShift(int[] values, int index, double oneSigmaUpper, double oneSigmaLower, bool isOneSigmaLowerValid)
        {
            if (index < 4)
            {
                return false;
            }

            // Check above: 4 of 5 successive points beyond 1σ above average
            var countAbove = 0;
            for (var j = index - 4; j <= index; j++)
            {
                if (values[j] > oneSigmaUpper)
                {
                    countAbove++;
                }
            }

            if (countAbove >= 4)
            {
                return true;
            }

            if (!isOneSigmaLowerValid)
            {
                return false;
            }

            // Check below: 4 of 5 successive points beyond 1σ below average
            var countBelow = 0;
            for (var j = index - 4; j <= index; j++)
            {
                if (values[j] < oneSigmaLower)
                {
                    countBelow++;
                }
            }

            return countBelow >= 4;
        }

        private static bool IsSmallShift(int[] values, int index, double average)
        {
            if (index < 7)
            {
                return false;
            }

            // Check 8 consecutive points above average
            var allAbove = true;
            var allBelow = true;

            for (var j = index - 7; j <= index; j++)
            {
                if (values[j] <= average)
                {
                    allAbove = false;
                }

                if (values[j] >= average)
                {
                    allBelow = false;
                }
            }

            return allAbove || allBelow;
        }
    }
}
