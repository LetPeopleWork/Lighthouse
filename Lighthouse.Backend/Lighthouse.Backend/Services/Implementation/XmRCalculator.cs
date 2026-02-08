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
        IReadOnlyList<IReadOnlyList<SpecialCauseType>> SpecialCauseClassifications);

    public static class XmRCalculator
    {
        private const double MovingRangeMultiplier = 2.66;

        public static XmRResult Calculate(int[] baselineValues, int[] displayValues)
        {
            if (baselineValues.Length == 0)
            {
                var emptyClassifications = CreateEmptyClassifications(displayValues.Length);
                return new XmRResult(0, 0, 0, emptyClassifications);
            }

            var average = baselineValues.Average();

            if (baselineValues.Length < 2)
            {
                var classifications = CreateEmptyClassifications(displayValues.Length);
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

        private static SpecialCauseType[][] CreateEmptyClassifications(int length)
        {
            var classifications = new SpecialCauseType[length][];
            for (var i = 0; i < length; i++)
            {
                classifications[i] = [];
            }

            return classifications;
        }

        private static List<List<SpecialCauseType>> ClassifyAllPoints(
            int[] values,
            double average,
            double unpl,
            double lnpl,
            double sigma)
        {
            var oneSigmaUpper = average + sigma;
            var twoSigmaUpper = average + 2 * sigma;
            var oneSigmaLower = Math.Max(0, average - sigma);
            var twoSigmaLower = Math.Max(0, average - 2 * sigma);

            var causeSets = InitializeCauseSets(values.Length);

            ApplyLargeChangeRule(values, unpl, lnpl, causeSets);
            ApplyModerateChangeRule(values, twoSigmaUpper, twoSigmaLower, average, causeSets);
            ApplyModerateShiftRule(values, oneSigmaUpper, oneSigmaLower, average, causeSets);
            ApplySmallShiftRule(values, average, causeSets);

            return ConvertCauseSetsToResult(causeSets);
        }

        private static HashSet<SpecialCauseType>[] InitializeCauseSets(int length)
        {
            var causeSets = new HashSet<SpecialCauseType>[length];
            for (var i = 0; i < length; i++)
            {
                causeSets[i] = [];
            }
            return causeSets;
        }

        private static void ApplyLargeChangeRule(
            int[] values,
            double unpl,
            double lnpl,
            HashSet<SpecialCauseType>[] causeSets)
        {
            var isLnplValid = lnpl > 0;

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] > unpl || (isLnplValid && values[i] < lnpl))
                {
                    causeSets[i].Add(SpecialCauseType.LargeChange);
                }
            }
        }

        private static void ApplyModerateChangeRule(
            int[] values,
            double twoSigmaUpper,
            double twoSigmaLower,
            double average,
            HashSet<SpecialCauseType>[] causeSets)
        {
            var isTwoSigmaLowerValid = twoSigmaLower > 0;

            for (var i = 2; i < values.Length; i++)
            {
                CheckModerateChangeAbove(values, twoSigmaUpper, average, causeSets, i);

                if (isTwoSigmaLowerValid)
                {
                    CheckModerateChangeBelow(values, twoSigmaLower, average, causeSets, i);
                }
            }
        }

        private static void CheckModerateChangeAbove(
            int[] values,
            double twoSigmaUpper,
            double average,
            HashSet<SpecialCauseType>[] causeSets,
            int endIndex)
        {
            // Count points that are beyond 2-sigma upper
            var countBeyondThreshold = 0;
            for (var j = endIndex - 2; j <= endIndex; j++)
            {
                if (values[j] > twoSigmaUpper)
                {
                    countBeyondThreshold++;
                }
            }

            // Rule requires 2 of 3 beyond threshold
            if (countBeyondThreshold >= 2)
            {
                // Mark only points that are above average (same side as the signal)
                for (var j = endIndex - 2; j <= endIndex; j++)
                {
                    if (values[j] > average)
                    {
                        causeSets[j].Add(SpecialCauseType.ModerateChange);
                    }
                }
            }
        }

        private static void CheckModerateChangeBelow(
            int[] values,
            double twoSigmaLower,
            double average,
            HashSet<SpecialCauseType>[] causeSets,
            int endIndex)
        {
            // Count points that are beyond 2-sigma lower
            var countBeyondThreshold = 0;
            for (var j = endIndex - 2; j <= endIndex; j++)
            {
                if (values[j] < twoSigmaLower)
                {
                    countBeyondThreshold++;
                }
            }

            // Rule requires 2 of 3 beyond threshold
            if (countBeyondThreshold >= 2)
            {
                // Mark only points that are below average (same side as the signal)
                for (var j = endIndex - 2; j <= endIndex; j++)
                {
                    if (values[j] < average)
                    {
                        causeSets[j].Add(SpecialCauseType.ModerateChange);
                    }
                }
            }
        }

        private static void ApplyModerateShiftRule(
            int[] values,
            double oneSigmaUpper,
            double oneSigmaLower,
            double average,
            HashSet<SpecialCauseType>[] causeSets)
        {
            var isOneSigmaLowerValid = oneSigmaLower > 0;

            for (var i = 4; i < values.Length; i++)
            {
                CheckModerateShiftAbove(values, oneSigmaUpper, average, causeSets, i);

                if (isOneSigmaLowerValid)
                {
                    CheckModerateShiftBelow(values, oneSigmaLower, average, causeSets, i);
                }
            }
        }

        private static void CheckModerateShiftAbove(
            int[] values,
            double oneSigmaUpper,
            double average,
            HashSet<SpecialCauseType>[] causeSets,
            int endIndex)
        {
            // Count points that are beyond 1-sigma upper
            var countBeyondThreshold = 0;
            for (var j = endIndex - 4; j <= endIndex; j++)
            {
                if (values[j] > oneSigmaUpper)
                {
                    countBeyondThreshold++;
                }
            }

            // Rule requires 4 of 5 beyond threshold
            if (countBeyondThreshold >= 4)
            {
                // Mark only points that are above average (same side as the signal)
                for (var j = endIndex - 4; j <= endIndex; j++)
                {
                    if (values[j] > average)
                    {
                        causeSets[j].Add(SpecialCauseType.ModerateShift);
                    }
                }
            }
        }

        private static void CheckModerateShiftBelow(
            int[] values,
            double oneSigmaLower,
            double average,
            HashSet<SpecialCauseType>[] causeSets,
            int endIndex)
        {
            // Count points that are beyond 1-sigma lower
            var countBeyondThreshold = 0;
            for (var j = endIndex - 4; j <= endIndex; j++)
            {
                if (values[j] < oneSigmaLower)
                {
                    countBeyondThreshold++;
                }
            }

            // Rule requires 4 of 5 beyond threshold
            if (countBeyondThreshold >= 4)
            {
                // Mark only points that are below average (same side as the signal)
                for (var j = endIndex - 4; j <= endIndex; j++)
                {
                    if (values[j] < average)
                    {
                        causeSets[j].Add(SpecialCauseType.ModerateShift);
                    }
                }
            }
        }

        private static void ApplySmallShiftRule(
            int[] values,
            double average,
            HashSet<SpecialCauseType>[] causeSets)
        {
            for (var i = 7; i < values.Length; i++)
            {
                if (AreAllPointsOnSameSideOfAverage(values, i - 7, i, average))
                {
                    MarkPointsInWindow(causeSets, i - 7, i, SpecialCauseType.SmallShift);
                }
            }
        }

        private static bool AreAllPointsOnSameSideOfAverage(
            int[] values,
            int startIndex,
            int endIndex,
            double average)
        {
            var allAbove = true;
            var allBelow = true;

            for (var j = startIndex; j <= endIndex; j++)
            {
                if (values[j] < average)
                {
                    allAbove = false;
                }
                else if (values[j] > average)
                {
                    allBelow = false;
                }
                else  // values[j] == average
                {
                    // Point exactly on average breaks both runs
                    return false;
                }
            }

            return allAbove || allBelow;
        }

        private static void MarkPointsInWindow(
            HashSet<SpecialCauseType>[] causeSets,
            int startIndex,
            int endIndex,
            SpecialCauseType causeType)
        {
            for (var j = startIndex; j <= endIndex; j++)
            {
                causeSets[j].Add(causeType);
            }
        }

        private static List<List<SpecialCauseType>> ConvertCauseSetsToResult(
            HashSet<SpecialCauseType>[] causeSets)
        {
            var result = new List<List<SpecialCauseType>>(causeSets.Length);
            foreach (var cause in causeSets)
            {
                var list = cause.Count > 0
                    ? cause.OrderBy(c => c).ToList()
                    : [];
                result.Add(list);
            }

            return result;
        }
    }
}
