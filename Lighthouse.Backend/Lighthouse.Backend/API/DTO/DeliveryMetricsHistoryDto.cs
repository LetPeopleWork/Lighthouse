using System.Text.Json;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public sealed record WhenDistributionPointDto(double Probability, DateTime ExpectedDate);

    public sealed record DeliveryFeatureMetricDto(string ReferenceId, string Name, double Completion, double Likelihood);

    public sealed record DeliveryMetricsHistoryPointDto(
        DateTime Date,
        int TotalWork,
        int DoneWork,
        int RemainingWork,
        int? EstimatedItemCount,
        int? ForecastHowMany,
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
                .OrderBy(snapshot => snapshot.RecordedAt)
                .Select(ToPoint)
                .ToList();

            var firstSnapshotDate = points.Count == 0 ? (DateTime?)null : points[0].Date;

            return new DeliveryMetricsHistoryDto(deliveryDate, firstSnapshotDate, points);
        }

        private static DeliveryMetricsHistoryPointDto ToPoint(DeliveryMetricSnapshot snapshot)
        {
            return new DeliveryMetricsHistoryPointDto(
                snapshot.RecordedAt,
                snapshot.TotalWork,
                snapshot.DoneWork,
                snapshot.RemainingWork,
                snapshot.EstimatedItemCount,
                snapshot.ForecastHowMany,
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
