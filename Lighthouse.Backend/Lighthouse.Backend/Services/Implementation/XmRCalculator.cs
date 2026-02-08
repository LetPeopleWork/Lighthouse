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

        private static IReadOnlyList<IReadOnlyList<SpecialCauseType>> ClassifyAllPoints(
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
            ApplyModerateChangeRule(values, twoSigmaUpper, twoSigmaLower, causeSets);
            ApplyModerateShiftRule(values, oneSigmaUpper, oneSigmaLower, causeSets);
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
            HashSet<SpecialCauseType>[] causeSets)
        {
            var isTwoSigmaLowerValid = twoSigmaLower > 0;

            for (var i = 2; i < values.Length; i++)
            {
                CheckModerateChangeAbove(values, twoSigmaUpper, causeSets, i);

                if (isTwoSigmaLowerValid)
                {
                    CheckModerateChangeBelow(values, twoSigmaLower, causeSets, i);
                }
            }
        }

        private static void CheckModerateChangeAbove(
            int[] values,
            double twoSigmaUpper,
            HashSet<SpecialCauseType>[] causeSets,
            int endIndex)
        {
            var countAbove = CountPointsBeyondThreshold(values, endIndex - 2, endIndex, twoSigmaUpper, above: true);

            if (countAbove >= 2)
            {
                MarkPointsInWindow(causeSets, endIndex - 2, endIndex, SpecialCauseType.ModerateChange);
            }
        }

        private static void CheckModerateChangeBelow(
            int[] values,
            double twoSigmaLower,
            HashSet<SpecialCauseType>[] causeSets,
            int endIndex)
        {
            var countBelow = CountPointsBeyondThreshold(values, endIndex - 2, endIndex, twoSigmaLower, above: false);

            if (countBelow >= 2)
            {
                MarkPointsInWindow(causeSets, endIndex - 2, endIndex, SpecialCauseType.ModerateChange);
            }
        }

        private static void ApplyModerateShiftRule(
            int[] values,
            double oneSigmaUpper,
            double oneSigmaLower,
            HashSet<SpecialCauseType>[] causeSets)
        {
            var isOneSigmaLowerValid = oneSigmaLower > 0;

            for (var i = 4; i < values.Length; i++)
            {
                CheckModerateShiftAbove(values, oneSigmaUpper, causeSets, i);

                if (isOneSigmaLowerValid)
                {
                    CheckModerateShiftBelow(values, oneSigmaLower, causeSets, i);
                }
            }
        }

        private static void CheckModerateShiftAbove(
            int[] values,
            double oneSigmaUpper,
            HashSet<SpecialCauseType>[] causeSets,
            int endIndex)
        {
            var countAbove = CountPointsBeyondThreshold(values, endIndex - 4, endIndex, oneSigmaUpper, above: true);

            if (countAbove >= 4)
            {
                MarkPointsInWindow(causeSets, endIndex - 4, endIndex, SpecialCauseType.ModerateShift);
            }
        }

        private static void CheckModerateShiftBelow(
            int[] values,
            double oneSigmaLower,
            HashSet<SpecialCauseType>[] causeSets,
            int endIndex)
        {
            var countBelow = CountPointsBeyondThreshold(values, endIndex - 4, endIndex, oneSigmaLower, above: false);

            if (countBelow >= 4)
            {
                MarkPointsInWindow(causeSets, endIndex - 4, endIndex, SpecialCauseType.ModerateShift);
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

        private static int CountPointsBeyondThreshold(
            int[] values,
            int startIndex,
            int endIndex,
            double threshold,
            bool above)
        {
            var count = 0;
            for (var j = startIndex; j <= endIndex; j++)
            {
                if (above ? values[j] > threshold : values[j] < threshold)
                {
                    count++;
                }
            }
            return count;
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
            for (var i = 0; i < causeSets.Length; i++)
            {
                result[i] = causeSets[i].Count > 0
                    ? causeSets[i].OrderBy(c => c).ToList()
                    : new List<SpecialCauseType>();
            }

            return result;
        }
    }
}
