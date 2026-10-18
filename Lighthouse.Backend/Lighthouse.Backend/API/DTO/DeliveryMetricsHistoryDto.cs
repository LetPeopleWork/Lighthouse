using System.Text.Json;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// One percentile of a recorded day. Points recorded before the horizon marker existed carry neither
    /// field and read as not beyond, with no horizon. A point keeps the horizon it was forecast with, so it
    /// still says what it meant if the horizon is ever changed.
    /// </summary>
    public sealed record WhenDistributionPointDto(double Probability, DateTime ExpectedDate)
    {
        public bool BeyondHorizon { get; init; }

        public int? HorizonYears { get; init; }
    }

    // Likelihood is nullable: a feature that cannot be forecast reports unknown, recorded as null.
    public sealed record DeliveryFeatureMetricDto(string ReferenceId, string Name, double Completion, double? Likelihood)
    {
        public int? TotalItems { get; init; }

        public bool? IsUsingDefaultSize { get; init; }
    }

    public sealed record DeliveryMetricsHistoryPointDto(
        DateTime Date,
        DateTime? TargetDateAtSnapshot,
        int TotalWork,
        int DoneWork,
        int RemainingWork,
        int? EstimatedItemCount,
        double? LikelihoodPercentage,
        IReadOnlyList<WhenDistributionPointDto>? WhenDistribution,
        IReadOnlyList<DeliveryFeatureMetricDto> FeatureBreakdown);

    public sealed record DeliveryMetricsHistoryDto(
        DateTime DeliveryDate,
        DateTime? FirstSnapshotDate,
        IReadOnlyList<DeliveryMetricsHistoryPointDto> Points)
    {
        private static readonly JsonSerializerOptions WhenDistributionJsonOptions = new() { PropertyNameCaseInsensitive = true };

        public static DeliveryMetricsHistoryDto From(DateTime deliveryDate, IEnumerable<DeliveryMetricSnapshot> snapshots)
        {
            var points = snapshots
                .OrderBy(snapshot => snapshot.RecordedDay)
                .Select(ToPoint)
                .ToList();

            var firstSnapshotDate = points.Count == 0 ? (DateTime?)null : points[0].Date;

            return new DeliveryMetricsHistoryDto(deliveryDate, firstSnapshotDate, points);
        }

        private static DeliveryMetricsHistoryPointDto ToPoint(DeliveryMetricSnapshot snapshot)
        {
            // The wire contract still carries the UTC-midnight DateTime the legacy column serialised.
            return new DeliveryMetricsHistoryPointDto(
                InstanceCalendar.AsUtcMidnight(snapshot.RecordedDay),
                snapshot.TargetDateAtSnapshot,
                snapshot.TotalWork,
                snapshot.DoneWork,
                snapshot.RemainingWork,
                snapshot.EstimatedItemCount,
                snapshot.LikelihoodPercentage,
                ParseWhenDistribution(snapshot.WhenDistributionJson),
                ParseFeatureBreakdown(snapshot.FeatureBreakdownJson));
        }

        private static List<WhenDistributionPointDto>? ParseWhenDistribution(string? whenDistributionJson)
        {
            if (string.IsNullOrWhiteSpace(whenDistributionJson))
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<WhenDistributionPointDto>>(whenDistributionJson, WhenDistributionJsonOptions);
        }

        private static List<DeliveryFeatureMetricDto> ParseFeatureBreakdown(string? featureBreakdownJson)
        {
            if (string.IsNullOrWhiteSpace(featureBreakdownJson))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<DeliveryFeatureMetricDto>>(featureBreakdownJson, WhenDistributionJsonOptions) ?? [];
        }
    }
}
