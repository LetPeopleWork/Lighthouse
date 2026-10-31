using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    public class ReportBulkCommands(IReportCommands commands, IReportWriter writer, IReportOwnerLookup ownerLookup, IEnumerable<IReportTemplate> templates, ILighthouseClock clock, TimeProvider timeProvider, ILogger<ReportBulkCommands> logger) : IReportBulkCommands
    {
        public const double SlowCreationRunMilliseconds = 20_000;

        private const string CreateAction = "create";

        private const string UnknownTemplate = "unknown";

        private readonly HashSet<string> offeredTemplateKeys = templates.Select(template => template.Key).ToHashSet(StringComparer.Ordinal);

        public async Task<IReadOnlyList<ReportOwnerOutcome>> CreateForEach(IReadOnlyList<ReportOwner> owners, NewReport request, IReportOwnerWriteAccess writeAccess)
        {
            var started = timeProvider.GetTimestamp();

            var outcomes = new List<ReportOwnerOutcome>();
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

            if (created.Status == ReportOutcomeStatus.NotFound)
            {
                return ReportOwnerOutcome.NotCreated(owner, ReportRefusalCodes.OwnerNotFound);
            }

            // Looked up after the save, so an owner deleted while its report was being made is not named as if it were still there.
            var facts = ownerLookup.Find(owner);
            var outcome = created switch
            {
                { Status: ReportOutcomeStatus.Done, Value: ReportRead read } => ReportOwnerOutcome.Created(owner, read.Id),
                _ when ReportRefusalCodes.NamesEarliestStart(created.Code) => ReportOwnerOutcome.NotCreated(owner, created.Code, EarliestStartOf(facts)),
                _ => ReportOwnerOutcome.NotCreated(owner, created.Code),
            };

            return outcome with { OwnerName = facts?.Name };
        }

        private ReportOwnerOutcome CreationNotSaved(ReportOwner owner, Exception failure)
        {
            writer.DiscardPendingChanges();

            // A database error's message can quote stored values or connection details, so only the type names are logged.
            logger.LogWarning("Report creation for {OwnerKind} {OwnerId} was not saved with {Reason}: {ExceptionType} ({InnerExceptionType})",
                owner.Kind, owner.Id, ReportRefusalCodes.NotSaved, failure.GetType().Name, failure.InnerException?.GetType().Name);

            return ReportOwnerOutcome.NotCreated(owner, ReportRefusalCodes.NotSaved);
        }

        private DateOnly? EarliestStartOf(ReportOwnerFacts? facts)
            => facts is null ? null : ReportWindowPolicy.EarliestStart(clock.Today, facts.DoneItemsCutoffDays);

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
    }
}
