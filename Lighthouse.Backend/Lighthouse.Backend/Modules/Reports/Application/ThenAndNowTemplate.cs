using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Application.Metrics;
using Lighthouse.Backend.Modules.Reports.Domain;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    /// <summary>
    /// Then is frozen at creation: every metric that applies to the owner is captured over Then's window and kept on
    /// the row. Now is never stored: each read computes it afresh, as long as Then and ending today.
    /// </summary>
    public class ThenAndNowTemplate(ReportMetricCatalog catalog, ILighthouseClock clock, ILogger<ThenAndNowTemplate> logger) : IReportTemplate
    {
        public const string TemplateKey = "then-and-now";

        public string Key => TemplateKey;

        public IReadOnlyList<string> MetricKeysFor(ReportOwnerKind ownerKind) => [.. catalog.For(ownerKind).Select(metric => metric.Key)];

        private HashSet<string> KnownMetricKeys(ReportOwnerKind ownerKind) => [.. MetricKeysFor(ownerKind)];

        public ReportOutcome<string> Freeze(ReportOwner owner, NewReport request, ReportCalendar calendar)
        {
            if (ShownMetricsRefusal(request.ShownMetricKeys, owner.Kind) is string shownRefusal)
            {
                return ReportOutcome<string>.Refused(shownRefusal);
            }

            if (request.ThenAndNow is not NewThenAndNow settings)
            {
                return ReportOutcome<string>.Refused(ReportRefusalCodes.ThenTooShort);
            }

            if (ReportWindowPolicy.ThenRefusal(settings.ThenEndDate, settings.ThenLengthDays, calendar.Today, calendar.DoneItemsCutoffDays) is string thenRefusal)
            {
                return ReportOutcome<string>.Refused(thenRefusal);
            }

            if (ReportWindow.EndingOn(settings.ThenEndDate, settings.ThenLengthDays) is not ReportWindow thenWindow)
            {
                return ReportOutcome<string>.Refused(ReportRefusalCodes.ThenTooLong);
            }

            if (ReportWindowPolicy.NowLengthRefusal(settings.NowLengthDays, settings.ThenLengthDays) is string nowRefusal)
            {
                return ReportOutcome<string>.Refused(nowRefusal);
            }

            var captures = catalog.For(owner.Kind).ToDictionary(metric => metric.Key, metric => metric.CaptureThen(owner, thenWindow));

            var payload = new ThenAndNowPayload
            {
                ThenWindow = thenWindow,
                Captures = captures,
            };

            return ReportOutcome<string>.Done(payload.ToJson());
        }

        // The stored payload is edited as a JSON tree rather than through the payload record, so members a later
        // version wrote, which this one cannot name, survive the edit untouched.
        public ReportOutcome<string> Edit(Report report, ReportEdit edit, ReportCalendar calendar)
        {
            if (ShownMetricsRefusal(edit.ShownMetricKeys, report.OwnedBy().Kind) is string shownRefusal)
            {
                return ReportOutcome<string>.Refused(shownRefusal);
            }

            var stored = PayloadOf(report);
            var captured = stored.Captures ?? [];
            if (!edit.ShownMetricKeys.All(captured.ContainsKey))
            {
                return ReportOutcome<string>.Refused(ReportRefusalCodes.MetricNotCaptured);
            }

            var payload = JsonNode.Parse(report.TemplatePayloadJson) as JsonObject
                ?? throw new JsonException($"Report {report.Id} holds no Then & Now payload");

            var thenLengthDays = (stored.ThenWindow ?? throw new JsonException($"Report {report.Id} holds no Then window")).LengthDays;
            if (ReportWindowPolicy.NowLengthRefusal(edit.RequestedNowLengthDays, thenLengthDays) is string nowRefusal)
            {
                return ReportOutcome<string>.Refused(nowRefusal);
            }

            return ReportOutcome<string>.Done(payload.ToJsonString());
        }

        public ReportRead Read(Report report, ReportCalendar calendar)
        {
            var payload = PayloadOf(report);
            var owner = report.OwnedBy();
            var thenWindow = payload.ThenWindow ?? throw new JsonException($"Report {report.Id} holds no Then window");
            var nowWindow = thenWindow.NowEndingOn(calendar.Today)
                ?? throw new JsonException($"Report {report.Id} holds a Then of {thenWindow.LengthDays} days, longer than the calendar reaches");

            var known = KnownMetricKeys(owner.Kind);
            var storedShown = ShownMetricKeysOf(report);
            LogKeysNotInTheCatalog(report, owner, known, [.. storedShown, .. payload.Captures?.Keys.ToList() ?? []]);

            var metrics = new Dictionary<string, MetricRead>();
            foreach (var metric in catalog.For(owner.Kind))
            {
                if (CaptureOf(payload, metric.Key) is JsonObject capture)
                {
                    metrics[metric.Key] = metric.Read(capture, owner, nowWindow);
                }
            }

            return new ReportRead(report.Id, report.Name, report.TemplateKey, owner.Kind, owner.Id, clock.ToInstanceDay(report.CreatedAt))
            {
                ConcurrencyToken = report.ConcurrencyToken,
                ShownMetricKeys = [.. storedShown.Where(known.Contains).Distinct()],
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

        private string? ShownMetricsRefusal(IReadOnlyList<string>? shown, ReportOwnerKind ownerKind)
        {
            if (shown is not { Count: > 0 })
            {
                return ReportRefusalCodes.NoMetricShown;
            }

            if (!shown.All(KnownMetricKeys(ownerKind).Contains))
            {
                return ReportRefusalCodes.UnknownMetric;
            }

            return shown.Distinct().Count() == shown.Count ? null : ReportRefusalCodes.DuplicateMetric;
        }

        // A newer version may have stored a metric this one has never heard of; the report still opens without it.
        private void LogKeysNotInTheCatalog(Report report, ReportOwner owner, HashSet<string> known, List<string> stored)
        {
            foreach (var key in stored.Distinct().Where(key => !known.Contains(key)))
            {
                logger.LogWarning("Stored report {ReportId} of {OwnerKind} {OwnerId} holds metric {MetricKey}, which this version does not know; it is skipped",
                    report.Id, owner.Kind, owner.Id, key);
            }
        }

        private static bool IsRebuildable(ReportWindow thenWindow, ReportCalendar calendar)
            => ReportWindowPolicy.ThenRebuildable(thenWindow.Start, calendar.Today, calendar.DoneItemsCutoffDays);

        private static ThenAndNowPayload PayloadOf(Report report)
            => ThenAndNowPayload.FromJson(report.TemplatePayloadJson) ?? throw new JsonException($"Report {report.Id} holds no Then & Now payload");

        private static JsonObject? CaptureOf(ThenAndNowPayload payload, string metricKey)
            => payload.Captures?.GetValueOrDefault(metricKey);

        private static List<string> ShownMetricKeysOf(Report report)
            => JsonSerializer.Deserialize<List<string>>(report.ShownMetricKeysJson, ThenAndNowPayload.JsonOptions) ?? [];
    }
}
