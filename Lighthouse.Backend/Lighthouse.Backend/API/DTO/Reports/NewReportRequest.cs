using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;

namespace Lighthouse.Backend.API.DTO.Reports
{
    public class NewReportRequest
    {
        public const string ThenAndNowIncomplete = "A report needs the day Then ends and how many days it spans.";

        public string? TemplateKey { get; set; }

        public string? Name { get; set; }

        public List<string>? ShownMetricKeys { get; set; }

        public NewThenAndNowRequest? ThenAndNow { get; set; }

        /// <summary>The report to create; null when a Then &amp; Now report lacks the day Then ends or its length.</summary>
        public NewReport? ToNewReport()
        {
            var thenAndNow = ThenAndNow is { ThenEndDate: DateOnly thenEndDate, ThenLengthDays: int thenLengthDays }
                ? new NewThenAndNow(thenEndDate, thenLengthDays, ThenAndNow.NowLengthDays)
                : null;

            if (thenAndNow is null && TemplateKey == ThenAndNowTemplate.TemplateKey)
            {
                return null;
            }

            return new NewReport(TemplateKey ?? string.Empty, Name ?? string.Empty, ShownMetricKeys ?? [], thenAndNow);
        }
    }

    public sealed class NewThenAndNowRequest
    {
        public DateOnly? ThenEndDate { get; set; }

        public int? ThenLengthDays { get; set; }

        public int? NowLengthDays { get; set; }
    }
}
