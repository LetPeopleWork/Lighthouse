using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    public class ReportBulkRemover(IReportCommands commands, IReportReader reader, IReportWriter writer, TimeProvider timeProvider, ILogger<ReportBulkRemover> logger) : IReportBulkRemover
    {
        public const double SlowRemovalRunMilliseconds = 10_000;

        private const string RemoveAction = "remove";

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
            catch (DbUpdateConcurrencyException)
            {
                // Someone else deleted the report between loading and saving, so it is gone as asked. The stale delete
                // is dropped so it cannot fail the save of the next report in the run.
                writer.DiscardPendingChanges();
                return ReportRemovalOutcome.Removed(reportId, ReportRefusalCodes.AlreadyGone);
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
