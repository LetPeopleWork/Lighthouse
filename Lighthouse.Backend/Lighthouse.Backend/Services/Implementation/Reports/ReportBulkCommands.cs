using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
#pragma warning disable S107 // Removal finds each report's owner through the read port, on top of what creation already needs; grouping unrelated collaborators into one object only to stay under the threshold would hide which run uses which.
    public class ReportBulkCommands(IReportCommands commands, IReportReader reader, IReportWriter writer, IReportOwnerLookup ownerLookup, IEnumerable<IReportTemplate> templates, ILighthouseClock clock, TimeProvider timeProvider, ILogger<ReportBulkCommands> logger) : IReportBulkCommands
#pragma warning restore S107
    {
        public const double SlowCreationRunMilliseconds = 20_000;

        public const double SlowRemovalRunMilliseconds = 10_000;

        private const string CreateAction = "create";

        private const string RemoveAction = "remove";

        private const string UnknownTemplate = "unknown";

        private readonly HashSet<string> offeredTemplateKeys = templates.Select(template => template.Key).ToHashSet(StringComparer.Ordinal);

        public async Task<IReadOnlyList<ReportOwnerOutcome>> CreateForEach(IReadOnlyList<ReportOwner> owners, NewReport request, IReportOwnerWriteAccess writeAccess)
        {
            var started = timeProvider.GetTimestamp();

            var outcomes = new List<ReportOwnerOutcome>(owners.Count);
            try
            {
                foreach (var owner in owners)
                {
                    outcomes.Add(await CreateFor(owner, request, writeAccess));
                }

                return outcomes;
            }
            finally
            {
                LogCreationRun(request, owners.Count, outcomes, timeProvider.GetElapsedTime(started).TotalMilliseconds);
            }
        }

        public async Task<IReadOnlyList<ReportRemovalOutcome>> RemoveEach(IReadOnlyList<int> reportIds, IReportOwnerWriteAccess writeAccess)
        {
            var started = timeProvider.GetTimestamp();

            var distinctIds = reportIds.Distinct().ToList();
            var outcomes = new List<ReportRemovalOutcome>();
            try
            {
                foreach (var reportId in distinctIds)
                {
                    outcomes.Add(await Remove(reportId, writeAccess));
                }

                return outcomes;
            }
            finally
            {
                LogRemovalRun(distinctIds.Count, outcomes, timeProvider.GetElapsedTime(started).TotalMilliseconds);
            }
        }

        private async Task<ReportRemovalOutcome> Remove(int reportId, IReportOwnerWriteAccess writeAccess)
        {
            // A report already gone has no owner left whose Admin right could be checked, and removing it changes nothing.
            if (reader.OwnerOf(reportId) is not ReportOwner owner)
            {
                return ReportRemovalOutcome.Removed(reportId, ReportRefusalCodes.AlreadyGone);
            }

            if (!await writeAccess.MayWriteAsync(owner))
            {
                return ReportRemovalOutcome.NotRemoved(reportId, ReportRefusalCodes.NotAdmin);
            }

            ReportOutcomeStatus deleted;
            try
            {
                deleted = await commands.Delete(owner, reportId);
            }
            catch (DbUpdateException failure)
            {
                return RemovalNotSaved(owner, reportId, failure);
            }

            return deleted == ReportOutcomeStatus.Done
                ? ReportRemovalOutcome.Removed(reportId)
                : ReportRemovalOutcome.Removed(reportId, ReportRefusalCodes.AlreadyGone);
        }

        private ReportRemovalOutcome RemovalNotSaved(ReportOwner owner, int reportId, DbUpdateException failure)
        {
            writer.DiscardPendingChanges();

            // A database error's message can quote stored values or connection details, so only the type names are logged.
            logger.LogWarning("Removal of report {ReportId} of {OwnerKind} {OwnerId} was not saved with {Reason}: {ExceptionType} ({InnerExceptionType})",
                reportId, owner.Kind, owner.Id, ReportRefusalCodes.NotSaved, failure.GetType().Name, failure.InnerException?.GetType().Name);

            return ReportRemovalOutcome.NotRemoved(reportId, ReportRefusalCodes.NotSaved);
        }

        private async Task<ReportOwnerOutcome> CreateFor(ReportOwner owner, NewReport request, IReportOwnerWriteAccess writeAccess)
        {
            if (!await writeAccess.MayWriteAsync(owner))
            {
                return ReportOwnerOutcome.NotCreated(owner, ReportRefusalCodes.NotAdmin);
            }

            ReportOutcome<ReportRead> created;
            try
            {
                created = await commands.Create(owner, request);
            }
            catch (Exception failure) when (failure is DbUpdateException or ReportOwnerGoneException)
            {
                return CreationNotSaved(owner, failure);
            }

            return created switch
            {
                { Status: ReportOutcomeStatus.Done, Value: ReportRead read } => ReportOwnerOutcome.Created(owner, read.Id),
                { Status: ReportOutcomeStatus.NotFound } => ReportOwnerOutcome.NotCreated(owner, ReportRefusalCodes.OwnerNotFound),
                _ when ReportRefusalCodes.NamesEarliestStart(created.Code) => ReportOwnerOutcome.NotCreated(owner, created.Code, EarliestStartOf(owner)),
                _ => ReportOwnerOutcome.NotCreated(owner, created.Code),
            };
        }

        private ReportOwnerOutcome CreationNotSaved(ReportOwner owner, Exception failure)
        {
            writer.DiscardPendingChanges();

            // A database error's message can quote stored values or connection details, so only the type names are logged.
            logger.LogWarning("Report creation for {OwnerKind} {OwnerId} was not saved with {Reason}: {ExceptionType} ({InnerExceptionType})",
                owner.Kind, owner.Id, ReportRefusalCodes.NotSaved, failure.GetType().Name, failure.InnerException?.GetType().Name);

            return ReportOwnerOutcome.NotCreated(owner, ReportRefusalCodes.NotSaved);
        }

        private DateOnly? EarliestStartOf(ReportOwner owner)
            => ownerLookup.Find(owner) is ReportOwnerFacts facts ? ReportWindowPolicy.EarliestStart(clock.Today, facts.DoneItemsCutoffDays) : null;

        private void LogCreationRun(NewReport request, int asked, List<ReportOwnerOutcome> outcomes, double elapsedMs)
        {
            var templateKey = offeredTemplateKeys.Contains(request.TemplateKey) ? request.TemplateKey : UnknownTemplate;
            var thenLengthDays = request.ThenAndNow?.ThenLengthDays;
            var nowLengthDays = request.ThenAndNow?.NowLengthDays ?? thenLengthDays;
            var done = outcomes.Count(outcome => outcome.Status == ReportCreationStatus.Created);
            var refusedByCode = outcomes
                .Where(outcome => outcome.Code is not null)
                .GroupBy(outcome => outcome.Code!, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

            if (elapsedMs > SlowCreationRunMilliseconds)
            {
                logger.LogWarning("report-bulk-run-slow: {Action} with template {TemplateKey}, Then {ThenLengthDays} days and Now {NowLengthDays} days, {Asked} asked, {Done} done, {NotDone} not done, refused {RefusedByCode}, took {ElapsedMs} ms",
                    CreateAction, templateKey, thenLengthDays, nowLengthDays, asked, done, asked - done, refusedByCode, elapsedMs);
                return;
            }

            logger.LogInformation("report-bulk-run: {Action} with template {TemplateKey}, Then {ThenLengthDays} days and Now {NowLengthDays} days, {Asked} asked, {Done} done, {NotDone} not done, refused {RefusedByCode}, in {ElapsedMs} ms",
                CreateAction, templateKey, thenLengthDays, nowLengthDays, asked, done, asked - done, refusedByCode, elapsedMs);
        }

        private void LogRemovalRun(int asked, List<ReportRemovalOutcome> outcomes, double elapsedMs)
        {
            var done = outcomes.Count(outcome => outcome.Status == ReportRemovalStatus.Removed);
            var refusedByCode = outcomes
                .Where(outcome => outcome.Status == ReportRemovalStatus.NotRemoved && outcome.Code is not null)
                .GroupBy(outcome => outcome.Code!, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

            if (elapsedMs > SlowRemovalRunMilliseconds)
            {
                logger.LogWarning("report-bulk-run-slow: {Action}, {Asked} asked, {Done} done, {NotDone} not done, refused {RefusedByCode}, took {ElapsedMs} ms",
                    RemoveAction, asked, done, asked - done, refusedByCode, elapsedMs);
                return;
            }

            logger.LogInformation("report-bulk-run: {Action}, {Asked} asked, {Done} done, {NotDone} not done, refused {RefusedByCode}, in {ElapsedMs} ms",
                RemoveAction, asked, done, asked - done, refusedByCode, elapsedMs);
        }
    }
}
