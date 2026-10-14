namespace Lighthouse.Backend.API.DTO.Reports
{
    public sealed class ReportEditRequest
    {
        public string? Name { get; set; }

        public List<string>? ShownMetricKeys { get; set; }

        public Guid? ConcurrencyToken { get; set; }

        public ReportEditThenAndNowRequest? ThenAndNow { get; set; }
    }

    public sealed class ReportEditThenAndNowRequest
    {
        public int? NowLengthDays { get; set; }
    }
}
