using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;

namespace Lighthouse.Backend.Modules.FlowMetrics.Domain
{
    public record XmRResult(
        int Average,
        int UpperNaturalProcessLimit,
        int LowerNaturalProcessLimit,
        IReadOnlyList<IReadOnlyList<SpecialCauseType>> SpecialCauseClassifications);
}
