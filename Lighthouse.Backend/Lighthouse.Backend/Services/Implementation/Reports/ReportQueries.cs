using System.Text.Json;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    public class ReportQueries(IReportReader reader, IReportOwnerLookup owners, IEnumerable<IReportTemplate> templates, ILicenseService licenseService, ILighthouseClock clock, TimeProvider timeProvider, ILogger<ReportQueries> logger) : IReportQueries
    {
        public const double SlowReadMilliseconds = 2_000;

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

            var reports = reader.ListForOwner(owner);
            var summaries = reports.Select(report => Summarise(report, owner, calendar)).ToList();
            var premium = licenseService.CanUsePremiumFeatures();
            var creationBlocked = templates.TrueForAll(template =>
                !ReportCapPolicy.MayCreate(premium, reports.Count(report => report.TemplateKey == template.Key)));

            return ReportOutcome<ReportList>.Done(new ReportList(summaries, creationBlocked, ReportCapPolicy.CommunityCap));
        }

        public ReportOutcome<ReportsAcrossOwners> ListAcross(IReadOnlyCollection<ReportOwner> readable)
        {
            var named = owners.All().Where(row => readable.Contains(row.Owner)).ToDictionary(row => row.Owner);
            if (named.Count == 0)
            {
                return ReportOutcome<ReportsAcrossOwners>.Done(new ReportsAcrossOwners([]));
            }

            var started = timeProvider.GetTimestamp();
            var today = clock.Today;
            var summaries = reader.ListForOwners([.. named.Keys])
                .Where(report => named.ContainsKey(report.OwnedBy()))
                .Select(report => (Row: named[report.OwnedBy()], Report: report))
                .OrderBy(pair => pair.Row.Owner.Kind)
                .ThenBy(pair => pair.Row.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(pair => pair.Report.Id)
                .Select(pair => Summarise(pair.Report, pair.Row.Owner, new ReportCalendar(today, pair.Row.DoneItemsCutoffDays)) with { OwnerName = pair.Row.Name })
                .ToList();

            var elapsedMs = timeProvider.GetElapsedTime(started).TotalMilliseconds;
            if (elapsedMs > SlowReadMilliseconds)
            {
                logger.LogWarning("report-list-slow: {Owners} owners with {Rows} reports took {ElapsedMs} ms to list", named.Count, summaries.Count, elapsedMs);
            }

            return ReportOutcome<ReportsAcrossOwners>.Done(new ReportsAcrossOwners(summaries));
        }

        public ReportOutcome<ReportCreationOptions> CreationOptions(IReadOnlyCollection<ReportOwner> readable)
        {
            var started = timeProvider.GetTimestamp();
            var today = clock.Today;
            var offered = templates
                .Select(template => new ReportCreationTemplate(template.Key, Enum.GetValues<ReportOwnerKind>().ToDictionary(kind => kind, template.MetricKeysFor)))
                .ToList();
            var readableOwners = owners.All()
                .Where(row => readable.Contains(row.Owner))
                .OrderBy(row => row.Owner.Kind)
                .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .Select(row => new ReportCreationOwner(row.Owner.Kind, row.Owner.Id, row.Name, ReportWindowPolicy.EarliestStart(today, row.DoneItemsCutoffDays)))
                .ToList();

            var elapsedMs = timeProvider.GetElapsedTime(started).TotalMilliseconds;
            if (elapsedMs > SlowReadMilliseconds)
            {
                logger.LogWarning("report-creation-options-slow: {Owners} owners took {ElapsedMs} ms to gather", readableOwners.Count, elapsedMs);
            }

            return ReportOutcome<ReportCreationOptions>.Done(new ReportCreationOptions(today, ReportWindowPolicy.MinimumLengthDays, offered, readableOwners));
        }

        public ReportOutcome<ReportRead> Get(ReportOwner owner, int reportId)
        {
            var started = timeProvider.GetTimestamp();
            if (CalendarOf(owner) is not ReportCalendar calendar || reader.GetForOwner(owner, reportId) is not Report report)
            {
                return ReportOutcome<ReportRead>.NotFound();
            }

            var outcome = Read(report, calendar);
            LogIfSlow(report, owner, timeProvider.GetElapsedTime(started).TotalMilliseconds);
            return outcome;
        }

        private ReportOutcome<ReportRead> Read(Report report, ReportCalendar calendar)
        {
            if (TemplateOf(report) is not IReportTemplate template)
            {
                LogUnreadable(report, null);
                return ReportOutcome<ReportRead>.Unsupported(ReportRefusalCodes.TemplateNotSupported);
            }

            try
            {
                return ReportOutcome<ReportRead>.Done(template.Read(report, calendar));
            }
            catch (JsonException exception)
            {
                LogUnreadable(report, exception);
                return ReportOutcome<ReportRead>.Unsupported(ReportRefusalCodes.TemplateNotSupported);
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

        private void LogIfSlow(Report report, ReportOwner owner, double elapsedMs)
        {
            if (elapsedMs > SlowReadMilliseconds)
            {
                logger.LogWarning("report-read-slow: report {ReportId} of {OwnerKind} {OwnerId} with template {TemplateKey} took {ElapsedMs} ms to read",
                    report.Id, owner.Kind, owner.Id, report.TemplateKey, elapsedMs);
            }
        }

        private void LogUnreadable(Report report, JsonException? exception)
        {
            logger.LogWarning(exception, "Stored report {ReportId} of template {TemplateKey} with payload schema version {PayloadSchemaVersion} cannot be read by this version",
                report.Id, report.TemplateKey, report.PayloadSchemaVersion);
        }
    }
}
