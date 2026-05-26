using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.API.DTO
{
    public sealed record AgeInStatePercentilesDto(string State, IReadOnlyList<PercentileValue> Percentiles);
}
