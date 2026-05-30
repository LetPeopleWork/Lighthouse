namespace Lighthouse.Backend.Models.Metrics
{
    public record FeatureEstimationDataPoint(
        int FeatureId,
        double EstimationNumericValue,
        string EstimationDisplayValue);

    public record FeatureSizeEstimationResponse(
        EstimationVsCycleTimeStatus Status,
        string? EstimationUnit,
        bool UseNonNumericEstimation,
        List<string> CategoryValues,
        List<FeatureEstimationDataPoint> FeatureEstimations);
}
