namespace Lighthouse.Backend.API.DTO.Reports
{
    public sealed class NewReportRequest
    {
        public string? TemplateKey { get; set; }

        public string? Name { get; set; }

        public List<string>? ShownMetricKeys { get; set; }

        public NewThenAndNowRequest? ThenAndNow { get; set; }
    }

    public sealed class NewThenAndNowRequest
    {
        public DateOnly? ThenEndDate { get; set; }

        public int? ThenLengthDays { get; set; }

        public int? NowLengthDays { get; set; }
    }
}
