using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    public class ReportBulkCommands(IReportCommands commands, TimeProvider timeProvider, ILogger<ReportBulkCommands> logger) : IReportBulkCommands
    {
        public const double SlowCreationRunMilliseconds = 20_000;

        private const string CreateAction = "create";

        public async Task<IReadOnlyList<ReportOwnerOutcome>> CreateForEach(IReadOnlyList<ReportOwner> owners, NewReport request, IReportOwnerWriteAccess writeAccess)
        {
            var started = timeProvider.GetTimestamp();

            var outcomes = new List<ReportOwnerOutcome>(owners.Count);
            foreach (var owner in owners)
            {
                outcomes.Add(await CreateFor(owner, request, writeAccess));
            }

            LogCreationRun(request, outcomes, timeProvider.GetElapsedTime(started).TotalMilliseconds);
            return outcomes;
        }

        private async Task<ReportOwnerOutcome> CreateFor(ReportOwner owner, NewReport request, IReportOwnerWriteAccess writeAccess)
        {
            if (!await writeAccess.MayWriteAsync(owner))
            {
                return ReportOwnerOutcome.NotCreated(owner, ReportRefusalCodes.NotAdmin);
            }

            var created = await commands.Create(owner, request);
            return created is { Status: ReportOutcomeStatus.Done, Value: ReportRead read }
                ? ReportOwnerOutcome.Created(owner, read.Id)
                : ReportOwnerOutcome.NotCreated(owner, created.Code);
        }

        private void LogCreationRun(NewReport request, List<ReportOwnerOutcome> outcomes, double elapsedMs)
        {
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
                    CreateAction, request.TemplateKey, thenLengthDays, nowLengthDays, outcomes.Count, done, outcomes.Count - done, refusedByCode, elapsedMs);
                return;
            }

            logger.LogInformation("report-bulk-run: {Action} with template {TemplateKey}, Then {ThenLengthDays} days and Now {NowLengthDays} days, {Asked} asked, {Done} done, {NotDone} not done, refused {RefusedByCode}, in {ElapsedMs} ms",
                CreateAction, request.TemplateKey, thenLengthDays, nowLengthDays, outcomes.Count, done, outcomes.Count - done, refusedByCode, elapsedMs);
        }
    }
}
