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
        /// <summary>Only read to refuse a Now that is not as long as Then; Now's length is never written.</summary>
        public int? NowLengthDays { get; set; }
    }
}
