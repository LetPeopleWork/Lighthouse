namespace Lighthouse.Backend.Models.Metrics
{
    public sealed record AgeInStatePercentilesDto(string State, IReadOnlyList<PercentileValue> Percentiles);
}
