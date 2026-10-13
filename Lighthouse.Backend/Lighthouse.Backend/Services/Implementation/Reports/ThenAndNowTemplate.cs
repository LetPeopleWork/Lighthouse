using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    /// <summary>
    /// Then is frozen at creation: every metric that applies to the owner is captured over Then's window and kept on
    /// the row. Now is never stored; each read computes it over a window of its own length ending today.
    /// </summary>
    public class ThenAndNowTemplate(ReportMetricCatalog catalog, ILighthouseClock clock) : IReportTemplate
    {
        public const string TemplateKey = "then-and-now";

        public string Key => TemplateKey;

        public IReadOnlyList<string> MetricKeysFor(ReportOwnerKind ownerKind) => [.. catalog.For(ownerKind).Select(metric => metric.Key)];

        public ReportOutcome<string> Freeze(ReportOwner owner, NewReport request, ReportCalendar calendar)
        {
            if (request.ThenAndNow is not NewThenAndNow settings)
            {
                return ReportOutcome<string>.Refused(ReportRefusalCodes.ThenTooShort);
            }

            if (ReportWindowPolicy.ThenRefusal(settings.ThenEndDate, settings.ThenLengthDays, calendar.Today) is string thenRefusal)
            {
                return ReportOutcome<string>.Refused(thenRefusal);
            }

            if (settings.NowLengthDays < 1)
            {
                return ReportOutcome<string>.Refused(ReportRefusalCodes.NowTooShort);
            }

            if (ReportWindow.EndingOn(settings.ThenEndDate, settings.ThenLengthDays) is not ReportWindow thenWindow)
            {
                return ReportOutcome<string>.Refused(ReportRefusalCodes.ThenBeforeCutoff);
            }

            var nowLengthDays = settings.NowLengthDays ?? settings.ThenLengthDays;
            if (ReportWindowPolicy.NowWindow(calendar.Today, nowLengthDays) is null)
            {
                return ReportOutcome<string>.Refused(ReportRefusalCodes.NowBeforeCutoff);
            }

            var captures = catalog.For(owner.Kind).ToDictionary(metric => metric.Key, metric => metric.CaptureThen(owner, thenWindow));

            var payload = new ThenAndNowPayload
            {
                ThenWindow = thenWindow,
                NowLengthDays = nowLengthDays,
                Captures = captures,
            };

            return ReportOutcome<string>.Done(payload.ToJson());
        }

        public ReportRead Read(Report report, ReportCalendar calendar)
        {
            var payload = PayloadOf(report);
            var owner = report.OwnedBy();
            var thenWindow = payload.ThenWindow ?? throw new JsonException($"Report {report.Id} holds no Then window");
            var nowLengthDays = payload.NowLengthDays ?? thenWindow.LengthDays;
            var nowWindow = ReportWindowPolicy.NowWindow(calendar.Today, nowLengthDays)
                ?? throw new JsonException($"Report {report.Id} holds a Now of {nowLengthDays} days, longer than the calendar reaches");

            var metrics = new Dictionary<string, MetricRead>();
            foreach (var metric in catalog.For(owner.Kind))
            {
                metrics[metric.Key] = metric.Read(CaptureOf(payload, metric.Key), owner, nowWindow);
            }

            return new ReportRead(report.Id, report.Name, report.TemplateKey, owner.Kind, owner.Id, clock.ToInstanceDay(report.CreatedAt))
            {
                ConcurrencyToken = report.ConcurrencyToken,
                ShownMetricKeys = ShownMetricKeysOf(report),
                ThenAndNow = new ThenAndNowRead(thenWindow, nowWindow, IsRebuildable(thenWindow, calendar), metrics),
            };
        }

        public ReportSummary Summarise(Report report, ReportCalendar calendar)
        {
            var owner = report.OwnedBy();
            var thenWindow = PayloadOf(report).ThenWindow;

            return new ReportSummary(report.Id, report.Name, report.TemplateKey, owner.Kind, owner.Id, clock.ToInstanceDay(report.CreatedAt))
            {
                ThenAndNow = thenWindow is null ? null : new ThenAndNowSummary(IsRebuildable(thenWindow, calendar)),
            };
        }

        private static bool IsRebuildable(ReportWindow thenWindow, ReportCalendar calendar)
            => ReportWindowPolicy.ThenRebuildable(thenWindow.Start, calendar.Today, calendar.DoneItemsCutoffDays);

        private static ThenAndNowPayload PayloadOf(Report report)
            => ThenAndNowPayload.FromJson(report.TemplatePayloadJson) ?? throw new JsonException($"Report {report.Id} holds no Then & Now payload");

        private static JsonObject? CaptureOf(ThenAndNowPayload payload, string metricKey)
            => payload.Captures is not null && payload.Captures.TryGetValue(metricKey, out var capture) ? capture : null;

        private static List<string> ShownMetricKeysOf(Report report)
            => JsonSerializer.Deserialize<List<string>>(report.ShownMetricKeysJson, ThenAndNowPayload.JsonOptions) ?? [];
    }
}
