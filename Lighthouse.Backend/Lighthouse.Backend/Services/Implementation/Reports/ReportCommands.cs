using System.Text.Json;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    public class ReportCommands(IReportWriter writer, IReportOwnerLookup owners, IEnumerable<IReportTemplate> templates, ILighthouseClock clock, TimeProvider timeProvider, ILogger<ReportCommands> logger) : IReportCommands
    {
        public const double SlowCreationMilliseconds = 10_000;

        private readonly List<IReportTemplate> templates = [.. templates];

        public async Task<ReportOutcome<ReportRead>> Create(ReportOwner owner, NewReport request)
        {
            var started = timeProvider.GetTimestamp();

            if (owners.Find(owner) is not ReportOwnerFacts facts)
            {
                return ReportOutcome<ReportRead>.NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Refuse(owner, TemplateFor(request)?.Key, ReportRefusalCodes.NameMissing);
            }

            if (request.Name.Trim().Length > Report.LongestName)
            {
                return Refuse(owner, TemplateFor(request)?.Key, ReportRefusalCodes.NameTooLong);
            }

            if (TemplateFor(request) is not IReportTemplate template)
            {
                return Refuse(owner, null, ReportRefusalCodes.TemplateNotSupported);
            }

            var calendar = new ReportCalendar(clock.Today, facts.DoneItemsCutoffDays);
            var frozen = template.Freeze(owner, request, calendar);
            if (frozen.Status != ReportOutcomeStatus.Done || frozen.Value is null)
            {
                return Refuse(owner, template.Key, frozen.Code ?? ReportRefusalCodes.TemplateNotSupported);
            }

            var report = Report.For(owner, template.Key, request.Name.Trim(), clock.Now.UtcDateTime);
            report.ShownMetricKeysJson = JsonSerializer.Serialize(request.ShownMetricKeys, ThenAndNowPayload.JsonOptions);
            report.TemplatePayloadJson = frozen.Value;

            writer.Add(report);
            await writer.Save();

            LogCreated(report, request, timeProvider.GetElapsedTime(started).TotalMilliseconds);
            return ReportOutcome<ReportRead>.Done(template.Read(report, calendar));
        }

        private IReportTemplate? TemplateFor(NewReport request) => templates.Find(candidate => candidate.Key == request.TemplateKey);

        // An unknown template key is what the caller typed, so only a key this version offers is ever logged.
        private ReportOutcome<ReportRead> Refuse(ReportOwner owner, string? templateKey, string reason)
        {
            logger.LogInformation("Report creation refused with {Reason} for {OwnerKind} {OwnerId} with template {TemplateKey}",
                reason, owner.Kind, owner.Id, templateKey);
            return ReportOutcome<ReportRead>.Refused(reason);
        }

        private void LogCreated(Report report, NewReport request, double elapsedMs)
        {
            var owner = report.OwnedBy();
            var thenLengthDays = request.ThenAndNow?.ThenLengthDays;
            var nowLengthDays = request.ThenAndNow?.NowLengthDays ?? thenLengthDays;

            if (elapsedMs > SlowCreationMilliseconds)
            {
                logger.LogWarning("report-creation-slow: report {ReportId} of {OwnerKind} {OwnerId} with template {TemplateKey}, Then {ThenLengthDays} days and Now {NowLengthDays} days, took {ElapsedMs} ms to create",
                    report.Id, owner.Kind, owner.Id, report.TemplateKey, thenLengthDays, nowLengthDays, elapsedMs);
                return;
            }

            logger.LogInformation("Report {ReportId} of {OwnerKind} {OwnerId} with template {TemplateKey}, Then {ThenLengthDays} days and Now {NowLengthDays} days, was created in {ElapsedMs} ms",
                report.Id, owner.Kind, owner.Id, report.TemplateKey, thenLengthDays, nowLengthDays, elapsedMs);
        }
    }
}
