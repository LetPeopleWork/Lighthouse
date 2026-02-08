using System.Text.Json.Serialization;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.API.DTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BaselineStatus
    {
        BaselineMissing,
        BaselineInvalid,
        InsufficientData,
        Ready,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum XAxisKind
    {
        Date,
        DateTime,
    }

    public record ProcessBehaviourChartDataPoint(
        string XValue,
        double YValue,
        IEnumerable<SpecialCauseType> SpecialCauses,
        int[] WorkItemIds);

    public record ProcessBehaviourChart
    {
        public BaselineStatus Status { get; init; }

        public string StatusReason { get; init; } = string.Empty;

        public XAxisKind XAxisKind { get; init; }

        public double Average { get; init; }

        public double UpperNaturalProcessLimit { get; init; }

        public double LowerNaturalProcessLimit { get; init; }

        public ProcessBehaviourChartDataPoint[] DataPoints { get; init; } = [];

        public static ProcessBehaviourChart NotReady(BaselineStatus status, string reason)
        {
            return new ProcessBehaviourChart
            {
                Status = status,
                StatusReason = reason,
                XAxisKind = XAxisKind.Date,
                Average = 0,
                UpperNaturalProcessLimit = 0,
                LowerNaturalProcessLimit = 0,
                DataPoints = [],
            };
        }
    }
}
