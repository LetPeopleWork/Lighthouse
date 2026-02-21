using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation
{
    public enum EstimateNormalizationStatus
    {
        Mapped,
        Unmapped,
        Invalid,
    }

    public record EstimateNormalizationResult(
        EstimateNormalizationStatus Status,
        double NumericValue,
        string DisplayValue);

    public record EstimateNormalizationBatchResult(
        IReadOnlyList<EstimateNormalizationResult> Results,
        int TotalCount,
        int MappedCount,
        int UnmappedCount,
        int InvalidCount);

    public static class EstimateNormalizer
    {
        private static readonly EstimateNormalizationResult InvalidResult =
            new(EstimateNormalizationStatus.Invalid, 0, string.Empty);

        public static EstimateNormalizationResult Normalize(
            string? estimate,
            bool useNonNumeric,
            IReadOnlyList<string> categoryValues)
        {
            if (string.IsNullOrWhiteSpace(estimate))
            {
                return InvalidResult;
            }

            return useNonNumeric
                ? NormalizeCategorical(estimate.Trim(), categoryValues)
                : NormalizeNumeric(estimate.Trim());
        }

        public static EstimateNormalizationBatchResult NormalizeBatch(
            IReadOnlyList<string?> estimates,
            bool useNonNumeric,
            IReadOnlyList<string> categoryValues)
        {
            var results = new List<EstimateNormalizationResult>(estimates.Count);
            var mapped = 0;
            var unmapped = 0;
            var invalid = 0;

            foreach (var estimate in estimates)
            {
                var result = Normalize(estimate, useNonNumeric, categoryValues);
                results.Add(result);

                switch (result.Status)
                {
                    case EstimateNormalizationStatus.Mapped:
                        mapped++;
                        break;
                    case EstimateNormalizationStatus.Unmapped:
                        unmapped++;
                        break;
                    case EstimateNormalizationStatus.Invalid:
                        invalid++;
                        break;
                }
            }

            return new EstimateNormalizationBatchResult(results, estimates.Count, mapped, unmapped, invalid);
        }

        private static EstimateNormalizationResult NormalizeNumeric(string estimate)
        {
            if (double.TryParse(estimate, CultureInfo.InvariantCulture, out var value))
            {
                return new EstimateNormalizationResult(
                    EstimateNormalizationStatus.Mapped,
                    value,
                    estimate);
            }

            return InvalidResult;
        }

        private static EstimateNormalizationResult NormalizeCategorical(
            string estimate,
            IReadOnlyList<string> categoryValues)
        {
            for (var i = 0; i < categoryValues.Count; i++)
            {
                if (string.Equals(categoryValues[i], estimate, StringComparison.OrdinalIgnoreCase))
                {
                    return new EstimateNormalizationResult(
                        EstimateNormalizationStatus.Mapped,
                        i,
                        categoryValues[i]);
                }
            }

            return new EstimateNormalizationResult(
                EstimateNormalizationStatus.Unmapped,
                0,
                estimate);
        }
    }
}
