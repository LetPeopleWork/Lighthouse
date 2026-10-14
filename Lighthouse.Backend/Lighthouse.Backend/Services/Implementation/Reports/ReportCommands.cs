using System.Text.Json;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    public class ReportCommands(IReportWriter writer, IReportOwnerLookup owners, IEnumerable<IReportTemplate> templates, ILicenseService licenseService, ILighthouseClock clock, TimeProvider timeProvider, ILogger<ReportCommands> logger) : IReportCommands
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

            var template = TemplateKeyed(request.TemplateKey);
            if (NameRefusal(request.Name) is string nameRefusal)
            {
                return Refuse(owner, template?.Key, nameRefusal);
            }

            if (template is null)
            {
                return Refuse(owner, null, ReportRefusalCodes.TemplateNotSupported);
            }

            if (!ReportCapPolicy.MayCreate(licenseService.CanUsePremiumFeatures(), writer.CountOfTemplate(owner, template.Key)))
            {
                return Refuse(owner, template.Key, ReportRefusalCodes.ReportCapReached);
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

        public async Task<ReportOutcome<ReportRead>> Edit(ReportOwner owner, int reportId, ReportEdit edit)
        {
            if (owners.Find(owner) is not ReportOwnerFacts facts || writer.FindForEdit(owner, reportId) is not Report report)
            {
                return ReportOutcome<ReportRead>.NotFound();
            }

            if (TemplateKeyed(report.TemplateKey) is not IReportTemplate template)
            {
                return ReportOutcome<ReportRead>.Unsupported(ReportRefusalCodes.TemplateNotSupported);
            }

            if (NameRefusal(edit.Name) is string nameRefusal)
            {
                return RefuseEdit(report, nameRefusal);
            }

            var calendar = new ReportCalendar(clock.Today, facts.DoneItemsCutoffDays);
            var edited = template.Edit(report, edit, calendar);
            if (edited.Status != ReportOutcomeStatus.Done || edited.Value is null)
            {
                return RefuseEdit(report, edited.Code ?? ReportRefusalCodes.TemplateNotSupported);
            }

            writer.ExpectUnchangedSince(report, edit.ConcurrencyToken);
            report.Name = edit.Name.Trim();
            report.ShownMetricKeysJson = JsonSerializer.Serialize(edit.ShownMetricKeys, ThenAndNowPayload.JsonOptions);
            report.TemplatePayloadJson = edited.Value;
            await writer.Save();

            logger.LogDebug("Report {ReportId} of {OwnerKind} {OwnerId} was edited", report.Id, owner.Kind, owner.Id);
            return ReportOutcome<ReportRead>.Done(template.Read(report, calendar));
        }

        public async Task<ReportOutcomeStatus> Delete(ReportOwner owner, int reportId)
        {
            if (writer.Remove(owner, reportId) is not Report removed)
            {
                return ReportOutcomeStatus.NotFound;
            }

            await writer.Save();

            logger.LogInformation("Report {ReportId} of {OwnerKind} {OwnerId} with template {TemplateKey} was deleted",
                removed.Id, owner.Kind, owner.Id, removed.TemplateKey);
            return ReportOutcomeStatus.Done;
        }

        private IReportTemplate? TemplateKeyed(string templateKey) => templates.Find(candidate => candidate.Key == templateKey);

        // An unknown template key is what the caller typed, so only a key this version offers is ever logged.
        private ReportOutcome<ReportRead> Refuse(ReportOwner owner, string? templateKey, string reason)
        {
            logger.LogInformation("Report creation refused with {Reason} for {OwnerKind} {OwnerId} with template {TemplateKey}",
                reason, owner.Kind, owner.Id, templateKey);
            return ReportOutcome<ReportRead>.Refused(reason);
        }

        private ReportOutcome<ReportRead> RefuseEdit(Report report, string reason)
        {
            var owner = report.OwnedBy();
            logger.LogInformation("Edit of report {ReportId} of {OwnerKind} {OwnerId} refused with {Reason}",
                report.Id, owner.Kind, owner.Id, reason);
            return ReportOutcome<ReportRead>.Refused(reason);
        }

        private static string? NameRefusal(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ReportRefusalCodes.NameMissing;
            }

            return name.Trim().Length > Report.LongestName ? ReportRefusalCodes.NameTooLong : null;
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
