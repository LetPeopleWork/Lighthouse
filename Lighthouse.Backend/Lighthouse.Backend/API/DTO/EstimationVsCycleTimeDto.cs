using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.API.DTO
{
    public enum EstimationVsCycleTimeStatus
    {
        NotConfigured,
        NoData,
        Ready,
    }

    public record EstimationVsCycleTimeDiagnostics(
        int TotalCount,
        int MappedCount,
        int UnmappedCount,
        int InvalidCount);

    public record EstimationVsCycleTimeDataPoint(
        int[] WorkItemIds,
        double EstimationNumericValue,
        string EstimationDisplayValue,
        int CycleTime);

    public record EstimationVsCycleTimeResponse(
        EstimationVsCycleTimeStatus Status,
        EstimationVsCycleTimeDiagnostics Diagnostics,
        string? EstimationUnit,
        bool UseNonNumericEstimation,
        IReadOnlyList<string> CategoryValues,
        IReadOnlyList<EstimationVsCycleTimeDataPoint> DataPoints);
}
