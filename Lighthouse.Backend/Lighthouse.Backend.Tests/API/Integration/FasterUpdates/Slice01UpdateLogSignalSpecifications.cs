using Lighthouse.Backend.Models;
using NUnit.Framework;
using Serilog.Events;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5687 slice 01 — the update log signal.
    /// Backend-observable contract: one Information line per completed update carrying entity, mode,
    /// records seen, records fetched, duration and success; everything the update iterated over demoted
    /// to Debug; and the same three facts persisted on the update's <see cref="RefreshLog"/> row.
    ///
    /// The line's field names are asserted individually rather than as one rendered sentence: they are
    /// what a log pipeline greps for, and the prose around them is free to improve without reding a
    /// scenario.
    /// </summary>
    public partial class Slice01UpdateLogSignalTest : FasterUpdatesAcceptanceTest
    {
        /// <summary>The marker that tells a summary line apart from everything else in the stream.</summary>
        private const string SummaryMarker = "Update completed";

        private const string ModeField = "mode=";
        private const string ScannedField = "scanned=";
        private const string FetchedField = "fetched=";
        private const string DurationField = "duration=";
        private const string SuccessField = "success=";

        /// <summary>The announcement that exists three times over on today's update path (AC-1.5).</summary>
        private const string TeamAnnouncement = "Updating Work Items for Team";

        /// <summary>
        /// Per-record narration: what the update iterated over, rather than what it did. Both fragments
        /// come from the extrapolation pass, which a portfolio update runs over every Feature the tracker
        /// returned without work.
        /// </summary>
        private static readonly string[] PerRecordNarration = ["Extrapolating", "Items to Feature"];

        private readonly record struct SeededTeam(int Id, string Name);

        private readonly record struct SeededPortfolio(int Id, string Name, int TeamId);

        // --- Given ---

        private SeededTeam GivenATeamThatIsRefreshedOnSchedule()
        {
            var connectionId = SeedConnection();
            var teamName = $"Team {Guid.NewGuid():N}";

            return new SeededTeam(SeedTeam(connectionId, teamName), teamName);
        }

        private SeededPortfolio GivenAPortfolioThatIsRefreshedOnSchedule()
        {
            var connectionId = SeedConnection();
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";
            var portfolioId = SeedPortfolio(connectionId, portfolioName);
            var teamId = SeedTeam(connectionId, $"Team {Guid.NewGuid():N}", portfolioId);

            return new SeededPortfolio(portfolioId, portfolioName, teamId);
        }

        private void GivenTheTrackerHolds(int workItems) => TheTrackerReturnsWorkItems(workItems);

        private void GivenTheTrackerHoldsFeatures(int features) => TheTrackerReturnsFeatures(features);

        private void GivenTheTrackerIsUnreachable()
            => TheTrackerIsUnreachable(new InvalidOperationException("The work tracking system is unreachable"));

        // --- When ---

        private Task WhenTheScheduledRefreshRuns(SeededTeam team) => TheTeamRefreshRuns(team.Id);

        private Task WhenTheScheduledRefreshRuns(SeededPortfolio portfolio) => ThePortfolioRefreshRuns(portfolio.Id);

        // --- Then ---

        private void ThenTheOperatorSeesExactlyOneUpdateSummary()
            => Assert.That(TheSummaryLines(), Has.Count.EqualTo(1),
                "One update, one line that says what it did — that is the whole slice. Operator-visible lines: "
                + string.Join(" | ", TheOperatorVisibleLines));

        private void ThenThatSummaryNamesTheTeam(SeededTeam team) => ThenThatSummaryNames("Team", team.Name);

        private void ThenThatSummaryNamesThePortfolio(SeededPortfolio portfolio) => ThenThatSummaryNames("Portfolio", portfolio.Name);

        private void ThenThatSummaryNames(string entityType, string entityName)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheSummaryLine(), Does.Contain(entityType),
                    "An operator reading one stream for both halves of the cycle has to be able to tell them apart.");
                Assert.That(TheSummaryLine(), Does.Contain(entityName),
                    "Which one is expensive is the question the line exists to answer.");
            }
        }

        /// <summary>
        /// AC-1.3: <c>mode</c> reads <c>full</c> for every update in this slice, so the later slices
        /// change the data behind the field rather than the shape of the line.
        /// </summary>
        private void ThenThatSummaryReportsAFullUpdate()
            => Assert.That(TheSummaryLine(), Does.Contain($"{ModeField}full").IgnoreCase,
                "Delta does not exist yet; the field does, so slice 02 has something to change.");

        private void ThenThatSummaryReportsRecordsSeenAndFetched(int scanned, int fetched)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheSummaryLine(), Does.Contain($"{ScannedField}{scanned}"),
                    "How many records the update saw is the denominator every later slice is measured against.");
                Assert.That(TheSummaryLine(), Does.Contain($"{FetchedField}{fetched}"),
                    "How many it actually downloaded is the numerator. In this slice the two are equal by construction.");
            }
        }

        private void ThenThatSummaryReportsHowLongItTook()
            => Assert.That(TheSummaryLine(), Does.Match($@"{DurationField}\d+"),
                "Whether the refresh interval is affordable is unanswerable without the cost.");

        private void ThenThatSummaryReportsItSucceeded()
            => Assert.That(TheSummaryLine(), Does.Contain($"{SuccessField}true").IgnoreCase);

        private void ThenThatSummaryReportsItFailed()
            => Assert.That(TheSummaryLine(), Does.Contain($"{SuccessField}false").IgnoreCase,
                "A failed update is the one an operator most needs to read, and it must not look like a fast one.");

        /// <summary>
        /// AC-1.9: the summary line is added to the error report, never in place of it.
        /// </summary>
        private void ThenTheFailureIsReportedWithItsCause()
            => Assert.That(CapturedLogs.At(LogEventLevel.Error),
                Has.One.Contains("The work tracking system is unreachable"),
                "The summary line says that it failed; only the error line says why.");

        /// <summary>
        /// AC-1.7 / KPI-5. The budget is two: the summary line, and at most one announcement that the
        /// update started (AC-1.5).
        /// </summary>
        private void ThenTheOperatorHadAtMostTwoLinesToRead()
            => Assert.That(TheOperatorVisibleLines, Has.Count.LessThanOrEqualTo(2),
                "Reading the log has to tell an operator what the system is doing, not what it is iterating over. Lines: "
                + string.Join(" | ", TheOperatorVisibleLines));

        private void ThenTheTeamIsAnnouncedAtMostOnce(SeededTeam team)
        {
            var announcements = TheOperatorVisibleLines
                .Where(line => line.Contains(TeamAnnouncement, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(announcements, Has.Count.LessThanOrEqualTo(1),
                $"'{TeamAnnouncement}' is written three times over on today's path. Occurrences: " + string.Join(" | ", announcements));
        }

        private void ThenNoPerRecordLineIsOperatorVisible()
        {
            var narration = TheOperatorVisibleLines
                .Where(line => PerRecordNarration.Any(fragment => line.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Assert.That(narration, Is.Empty,
                "Per-record narration is what buries the one line that matters. Occurrences: " + string.Join(" | ", narration));
        }

        /// <summary>
        /// The other half of AC-1.6, and the positive control for the assertion above: nothing is
        /// deleted. If the narration is missing from Debug too, the scenario never exercised the pass it
        /// is about and the assertion above passed for free.
        /// </summary>
        private void ThenThePerRecordDetailIsStillAvailableToWhoeverAsksForIt()
        {
            var narration = CapturedLogs.At(LogEventLevel.Debug)
                .Where(line => PerRecordNarration.Any(fragment => line.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Assert.That(narration, Is.Not.Empty,
                "Noise is demoted, never dropped — and a scenario that provoked no narration at all proves nothing.");
        }

        private void ThenTheRecordedUpdateReportsAFullUpdateOf(SeededTeam team, int scanned, int fetched)
        {
            var recorded = TheRefreshLogFor(RefreshType.Team, team.Id);

            Assert.That(recorded, Is.Not.Null, "The update recorded nothing at all.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded!.Mode, Is.EqualTo(SyncMode.Full));
                Assert.That(recorded.RecordsScanned, Is.EqualTo(scanned));
                Assert.That(recorded.RecordsFetched, Is.EqualTo(fetched));
            }
        }

        /// <summary>
        /// AC-1.8's second half: the three new fields are additive. Nothing already recorded is dropped
        /// or renamed, because the update-status view reads this row.
        /// </summary>
        private void ThenTheRecordedUpdateStillCarriesEverythingItAlwaysDid(SeededTeam team, int itemCount)
        {
            var recorded = TheRefreshLogFor(RefreshType.Team, team.Id);

            Assert.That(recorded, Is.Not.Null, "The update recorded nothing at all.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded!.EntityName, Is.EqualTo(team.Name));
                Assert.That(recorded.ItemCount, Is.EqualTo(itemCount));
                Assert.That(recorded.Success, Is.True);
                Assert.That(recorded.ExecutedAt, Is.Not.Default);
            }
        }

        // --- Reading the log ---

        private List<string> TheSummaryLines()
            => [.. TheOperatorVisibleLines.Where(line => line.Contains(SummaryMarker, StringComparison.OrdinalIgnoreCase))];

        private string TheSummaryLine()
        {
            var summaries = TheSummaryLines();

            Assert.That(summaries, Is.Not.Empty,
                "No update summary was written. Operator-visible lines: " + string.Join(" | ", TheOperatorVisibleLines));

            return summaries[0];
        }
    }
}
