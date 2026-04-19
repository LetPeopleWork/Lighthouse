namespace Lighthouse.Backend.API.DTO
{
    public record TrendDetailRowDto(
        string Label,
        string CurrentValue,
        string PreviousValue);

    public record InfoWidgetComparisonDto(
        string Direction,
        string MetricLabel,
        string? CurrentLabel,
        string? CurrentValue,
        string? PreviousLabel,
        string? PreviousValue,
        string? PercentageDelta,
        TrendDetailRowDto[]? DetailRows);

    public record PercentileValueDto(
        int Percentile,
        int Value);

    public record ThroughputInfoDto(
        int Total,
        double DailyAverage,
        InfoWidgetComparisonDto Comparison);

    public record ArrivalsInfoDto(
        int Total,
        double DailyAverage,
        InfoWidgetComparisonDto Comparison);

    public record FeatureSizePercentilesInfoDto(
        PercentileValueDto[] Percentiles,
        InfoWidgetComparisonDto Comparison);

    public record WipOverviewInfoDto(
        int Count,
        InfoWidgetComparisonDto Comparison);

    public record FeaturesWorkedOnInfoDto(
        int Count,
        InfoWidgetComparisonDto Comparison);

    public record TotalWorkItemAgeInfoDto(
        int TotalAge,
        InfoWidgetComparisonDto Comparison);

    public record PredictabilityScoreInfoDto(
        double Score,
        InfoWidgetComparisonDto Comparison);

    public record CycleTimePercentilesInfoDto(
        PercentileValueDto[] Percentiles,
        InfoWidgetComparisonDto Comparison);
}
