using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    public class ReportBulkCommands(IReportCommands commands, IReportOwnerLookup ownerLookup, IEnumerable<IReportTemplate> templates, ILighthouseClock clock, TimeProvider timeProvider, ILogger<ReportBulkCommands> logger) : IReportBulkCommands
    {
        public const double SlowCreationRunMilliseconds = 20_000;

        private const string CreateAction = "create";

        private const string UnknownTemplate = "unknown";

        private readonly HashSet<string> offeredTemplateKeys = templates.Select(template => template.Key).ToHashSet(StringComparer.Ordinal);

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
            return created switch
            {
                { Status: ReportOutcomeStatus.Done, Value: ReportRead read } => ReportOwnerOutcome.Created(owner, read.Id),
                { Status: ReportOutcomeStatus.NotFound } => ReportOwnerOutcome.NotCreated(owner, ReportRefusalCodes.OwnerNotFound),
                { Code: ReportRefusalCodes.ThenBeforeCutoff or ReportRefusalCodes.NowBeforeCutoff } => ReportOwnerOutcome.NotCreated(owner, created.Code, EarliestStartOf(owner)),
                _ => ReportOwnerOutcome.NotCreated(owner, created.Code),
            };
        }

        private DateOnly? EarliestStartOf(ReportOwner owner)
            => ownerLookup.Find(owner) is ReportOwnerFacts facts ? ReportWindowPolicy.EarliestStart(clock.Today, facts.DoneItemsCutoffDays) : null;

        private void LogCreationRun(NewReport request, List<ReportOwnerOutcome> outcomes, double elapsedMs)
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
                    CreateAction, templateKey, thenLengthDays, nowLengthDays, outcomes.Count, done, outcomes.Count - done, refusedByCode, elapsedMs);
                return;
            }

            logger.LogInformation("report-bulk-run: {Action} with template {TemplateKey}, Then {ThenLengthDays} days and Now {NowLengthDays} days, {Asked} asked, {Done} done, {NotDone} not done, refused {RefusedByCode}, in {ElapsedMs} ms",
                CreateAction, templateKey, thenLengthDays, nowLengthDays, outcomes.Count, done, outcomes.Count - done, refusedByCode, elapsedMs);
        }
    }
}
