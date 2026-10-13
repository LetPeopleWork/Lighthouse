using System.Text.Json;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    public class ReportQueries(IReportReader reader, IReportOwnerLookup owners, IEnumerable<IReportTemplate> templates, ILighthouseClock clock, ILogger<ReportQueries> logger) : IReportQueries
    {
        public const int CommunityCap = 2;

        private const string TemplateNotSupported = "template-not-supported";

        private readonly List<IReportTemplate> templates = [.. templates];

        public ReportOutcome<ReportTemplatesRead> Templates(ReportOwner owner)
        {
            if (CalendarOf(owner) is not ReportCalendar calendar)
            {
                return ReportOutcome<ReportTemplatesRead>.NotFound();
            }

            var offered = templates
                .Select(template => new ReportTemplateRead(template.Key, [.. template.MetricKeysFor(owner.Kind).Select(key => new ReportTemplateMetricRead(key))]))
                .ToList();

            return ReportOutcome<ReportTemplatesRead>.Done(new ReportTemplatesRead(
                calendar.Today,
                ReportWindowPolicy.EarliestStart(calendar.Today, calendar.DoneItemsCutoffDays),
                ReportWindowPolicy.MinimumLengthDays,
                offered));
        }

        public ReportOutcome<ReportList> List(ReportOwner owner)
        {
            if (CalendarOf(owner) is not ReportCalendar calendar)
            {
                return ReportOutcome<ReportList>.NotFound();
            }

            var summaries = reader.ListForOwner(owner).Select(report => Summarise(report, owner, calendar)).ToList();
            return ReportOutcome<ReportList>.Done(new ReportList(summaries, false, CommunityCap));
        }

        public ReportOutcome<ReportRead> Get(ReportOwner owner, int reportId)
        {
            if (CalendarOf(owner) is not ReportCalendar calendar || reader.GetForOwner(owner, reportId) is not Report report)
            {
                return ReportOutcome<ReportRead>.NotFound();
            }

            if (TemplateOf(report) is not IReportTemplate template)
            {
                LogUnreadable(report, null);
                return ReportOutcome<ReportRead>.Unsupported(TemplateNotSupported);
            }

            try
            {
                return ReportOutcome<ReportRead>.Done(template.Read(report, calendar));
            }
            catch (JsonException exception)
            {
                LogUnreadable(report, exception);
                return ReportOutcome<ReportRead>.Unsupported(TemplateNotSupported);
            }
        }

        private ReportSummary Summarise(Report report, ReportOwner owner, ReportCalendar calendar)
        {
            if (TemplateOf(report) is IReportTemplate template)
            {
                try
                {
                    return template.Summarise(report, calendar);
                }
                catch (JsonException exception)
                {
                    LogUnreadable(report, exception);
                }
            }

            return new ReportSummary(report.Id, report.Name, report.TemplateKey, owner.Kind, owner.Id, clock.ToInstanceDay(report.CreatedAt));
        }

        private ReportCalendar? CalendarOf(ReportOwner owner)
            => owners.Find(owner) is ReportOwnerFacts facts ? new ReportCalendar(clock.Today, facts.DoneItemsCutoffDays) : null;

        private IReportTemplate? TemplateOf(Report report) => templates.Find(template => template.Key == report.TemplateKey);

        private void LogUnreadable(Report report, JsonException? exception)
        {
            logger.LogWarning(exception, "Stored report {ReportId} of template {TemplateKey} with payload schema version {PayloadSchemaVersion} cannot be read by this version",
                report.Id, report.TemplateKey, report.PayloadSchemaVersion);
        }
    }
}
