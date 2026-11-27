using Lighthouse.Backend.Modules.FlowMetrics.Contracts;

namespace Lighthouse.Backend.API.DTO
{
    public enum CycleTimeOutlierStatus
    {
        Ready,
        NoLimits,
    }

    public sealed record CycleTimeOutliersDto(
        CycleTimeOutlierStatus Status,
        double? UpperNaturalProcessLimit,
        IReadOnlyList<int> OutlierWorkItemIds)
    {
        public static CycleTimeOutliersDto From(OutlierVerdict verdict) => verdict switch
        {
            OutlierVerdict.Limited limited => new CycleTimeOutliersDto(CycleTimeOutlierStatus.Ready, limited.UpperNaturalProcessLimitDays, limited.OutlierWorkItemIds),
            _ => new CycleTimeOutliersDto(CycleTimeOutlierStatus.NoLimits, null, []),
        };
    }
}
