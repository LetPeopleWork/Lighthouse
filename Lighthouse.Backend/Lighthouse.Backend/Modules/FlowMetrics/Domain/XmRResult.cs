using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.FlowMetrics.Domain
{
    public record XmRResult(
        int Average,
        int UpperNaturalProcessLimit,
        int LowerNaturalProcessLimit,
        IReadOnlyList<IReadOnlyList<SpecialCauseType>> SpecialCauseClassifications);
}
