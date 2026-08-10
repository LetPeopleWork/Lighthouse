using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Serilog.Events;
using System.Collections;
using System.Reflection;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5687 slice 02 — a Jira Cloud team refresh
    /// fetches only what moved.
    ///
    /// Backend-observable contract: the same query is still enumerated in full every cycle, so
    /// <c>removed = stored − scanned</c> keeps today's meaning (D2); full payloads are downloaded only
    /// for records whose remote change stamp differs from the stored one (D12); an untouched record is
    /// left byte-identical; staleness is evaluated over the stored set rather than the fetched one
    /// (D10); anything ambiguous — never scanned, no stored stamp, scan failed, nobody opted in —
    /// resolves to a full download (D8, A1).
    ///
    /// The summary line's field names are slice 01's, asserted individually: they are what a log
    /// pipeline greps for, and only the value behind <c>mode</c> changes in this slice.
    /// </summary>
    public partial class Slice02JiraCloudTeamDeltaTest : FasterUpdatesAcceptanceTest
    {
        private const string SummaryMarker = "Update completed";
        private const string ModeField = "mode=";
        private const string ScannedField = "scanned=";
        private const string FetchedField = "fetched=";

        private const string TheScansRefusal = "The identity scan was refused by the work tracking system";

        private const string DeliveredFeature = "FEAT-1";

        private static readonly DateTime AWhileAgo = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        private readonly record struct SeededTeam(int Id, string Name);

        /// <summary>
        /// Every stored value of one issue, plus its transitions, rendered so that value equality means
        /// "nothing about it changed". Reflection over the whole property surface on purpose: AC-2.4 is
        /// the assertion that catches "the tracker's <c>updated</c> is not trustworthy", and a spot check
        /// of two fields cannot catch a third one being rewritten.
        /// </summary>
        private readonly record struct StoredIssue(string Values, string Transitions);

        // --- Given ---

        private SeededTeam GivenATeamWhoseTrackerCanBeScanned()
        {
            var team = SeedATeam();
            TheTrackerCanBeScanned();

            return team;
        }

        private SeededTeam GivenATeamThatCallsWorkStaleAfterFiveDays()
        {
            var team = SeedATeam(stalenessThresholdDays: 5);
            TheTrackerCanBeScanned();

            return team;
        }

        private SeededTeam GivenATeamDeliveringOneFeature()
        {
            var connectionId = SeedConnection();
            var portfolioId = SeedPortfolio(connectionId, $"Portfolio {Guid.NewGuid():N}");
            var teamName = $"Team {Guid.NewGuid():N}";
            var teamId = SeedTeam(connectionId, teamName, portfolioId);

            SeedFeature(portfolioId, DeliveredFeature, "The feature being delivered", teamId, workAlreadyCounted: 3);
            TheTrackerCanBeScanned();

            return new SeededTeam(teamId, teamName);
        }

        private SeededTeam SeedATeam(int stalenessThresholdDays = 0)
        {
            var connectionId = SeedConnection();
            var teamName = $"Team {Guid.NewGuid():N}";

            return new SeededTeam(SeedTeam(connectionId, teamName, portfolioId: null, stalenessThresholdDays), teamName);
        }

        private void GivenTheTrackerHoldsThreeIssues()
            => TheTrackerHolds(
                new RemoteRecord("ITEM-1", AWhileAgo),
                new RemoteRecord("ITEM-2", AWhileAgo),
                new RemoteRecord("ITEM-3", AWhileAgo));

        private void GivenTheTrackerHoldsThreeIssuesOnThatFeature()
            => TheTrackerHolds(
                new RemoteRecord("ITEM-1", AWhileAgo) { ParentReferenceId = DeliveredFeature },
                new RemoteRecord("ITEM-2", AWhileAgo) { ParentReferenceId = DeliveredFeature },
                new RemoteRecord("ITEM-3", AWhileAgo) { ParentReferenceId = DeliveredFeature });

        private void GivenTheTrackerHoldsAnIssueNobodyHasTouchedInWeeks()
            => TheTrackerHolds(new RemoteRecord("ITEM-1", AWhileAgo.AddDays(-30)));

        /// <summary>
        /// The upgrade case (D8): the team's work is already stored, and none of it carries a remote
        /// change stamp, because the release that records one is the one being installed.
        /// </summary>
        private void GivenTheTeamsIssuesWereStoredBeforeThisRelease(SeededTeam team, params string[] referenceIds)
            => SeedStoredWorkItems(
                team.Id,
                [.. referenceIds.Select(referenceId => new RemoteRecord(referenceId, AWhileAgo) { StoredStamp = null })]);

        private void GivenThatIssueWasAlreadyStoredWithTheDayItEnteredItsState(SeededTeam team)
            => SeedStoredWorkItems(
                team.Id,
                new RemoteRecord("ITEM-1", AWhileAgo.AddDays(-30))
                {
                    StoredStamp = TheTrackersChangeStampFor("ITEM-1"),
                    StateEnteredAt = DateTime.UtcNow.AddDays(-30),
                    StartedDate = DateTime.UtcNow.AddDays(-30),
                });

        /// <summary>
        /// Pillar 2: the second cycle's precondition is the first cycle, run through the same driving
        /// port with the same step method — not a hand-built row that happens to look like its result.
        /// </summary>
        private Task GivenTheTeamHasAlreadyBeenRefreshed(SeededTeam team) => WhenTheScheduledRefreshRuns(team);

        private void GivenOneIssueMovedOnTheTracker(string referenceId)
            => OnTheTrackerTheIssueChanges(referenceId, AWhileAgo.AddHours(1), state: "Done");

        private void GivenOneIssueLeftTheQuery(string referenceId) => OnTheTrackerTheIssueIsGone(referenceId);

        private void GivenTheScanFails() => TheScanFails(new InvalidOperationException(TheScansRefusal));

        private void GivenTheOperatorAskedForTheCheaperRefresh() => TheOperatorAsksForTheCheaperRefresh();

        private void GivenNobodyAskedForTheCheaperRefresh()
            => Assert.That(TheCheaperRefreshOption()?.Enabled, Is.Not.True,
                "The default has to be off, or the scenario is not testing the default.");

        private StoredIssue GivenHowTheUntouchedIssueLooksNow(SeededTeam team, string referenceId)
            => TheStoredIssue(team, referenceId);

        // --- When ---

        /// <summary>
        /// The team is a parameter of every step that needs it, never a field one step writes and another
        /// reads: a Given that reads what a When assigns runs before the assignment and sees nothing.
        /// </summary>
        private Task WhenTheScheduledRefreshRuns(SeededTeam team) => TheTeamRefreshRuns(team.Id);

        private void WhenTheInstanceIsUpgradedAgain() => TheInstanceIsUpgradedAgain();

        // --- Then: what the tracker was asked for ---

        private void ThenTheWholeQueryWasDownloaded()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FullDownloadsIssued, Is.EqualTo(1),
                    "A full update downloads the whole query exactly once.");
                Assert.That(PayloadDownloads, Is.Empty,
                    "A full update has nothing to fetch by reference id - it already asked for everything.");
            }
        }

        private void ThenTheWholeQueryWasScannedForIdentitiesOnly()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ScansIssued, Is.EqualTo(1),
                    "The cheap scan is the whole point, and it enumerates the same query the full download does.");
                Assert.That(FullDownloadsIssued, Is.Zero,
                    "Scanning and then downloading everything anyway costs more than not scanning at all.");
            }
        }

        private void ThenTheTrackerWasNeverScanned()
            => Assert.That(ScansIssued, Is.Zero,
                "Nobody asked for the cheaper refresh, so the tracker must not be scanned at all - "
                + "the removal rule this epic relies on is what makes an unasked-for scan a data-loss risk.");

        private void ThenOnlyTheIssuesThatMovedWereDownloaded(params string[] referenceIds)
        {
            Assert.That(PayloadDownloads, Has.Count.EqualTo(1),
                "One cycle asks for the changed payloads once. Requests: " + RenderPayloadDownloads());

            Assert.That(PayloadDownloads[0], Is.EquivalentTo(referenceIds),
                "Downloading an issue whose timestamp did not move is the cost this slice exists to remove. Requested: "
                + RenderPayloadDownloads());
        }

        private void ThenTheIssueWasNeverDownloaded()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FullDownloadsIssued, Is.Zero,
                    "Nothing moved, so nothing had to be downloaded.");
                Assert.That(PayloadDownloads.SelectMany(request => request), Is.Empty,
                    "An issue that stopped changing is exactly the issue that stops being fetched - "
                    + "which is why staleness cannot be evaluated off the fetch loop (D10).");
            }
        }

        // --- Then: what the operator reads and what was recorded ---

        private void ThenTheOperatorSeesACheaperUpdate(int scanned, int fetched)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheSummaryLine(), Does.Contain($"{ModeField}delta").IgnoreCase,
                    "Slice 01 shipped the field so this slice could change the value behind it.");
                Assert.That(TheSummaryLine(), Does.Contain($"{ScannedField}{scanned}"),
                    "How much of the query was still enumerated is what tells the operator removals are still caught.");
                Assert.That(TheSummaryLine(), Does.Contain($"{FetchedField}{fetched}"),
                    "How little was downloaded is the number the operator shows their Jira admin.");
            }
        }

        private void ThenTheRefreshReportedAFullUpdateOf(SeededTeam team, int scanned, int fetched)
            => ThenTheRefreshReported(team, SyncMode.Full, scanned, fetched);

        private void ThenTheRefreshReportedACheaperUpdateOf(SeededTeam team, int scanned, int fetched)
            => ThenTheRefreshReported(team, SyncMode.Delta, scanned, fetched);

        private void ThenTheRefreshReported(SeededTeam team, SyncMode mode, int scanned, int fetched)
        {
            var recorded = TheLastRefreshLogFor(RefreshType.Team, team.Id);

            Assert.That(recorded, Is.Not.Null, "The refresh recorded nothing at all.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded!.Mode, Is.EqualTo(mode));
                Assert.That(recorded.RecordsScanned, Is.EqualTo(scanned));
                Assert.That(recorded.RecordsFetched, Is.EqualTo(fetched));
                Assert.That(recorded.Success, Is.True,
                    "A refresh that resolved its own ambiguity is a successful refresh, not a failed one.");
            }
        }

        private void ThenTheOperatorIsToldTheScanFailed()
            => Assert.That(CapturedLogs.AtOrAbove(LogEventLevel.Warning), Has.One.Contains(TheScansRefusal),
                "Falling back silently means nobody ever learns the cheaper path stopped working. Lines: "
                + string.Join(" | ", CapturedLogs.AtOrAbove(LogEventLevel.Warning)));

        // --- Then: what is stored ---

        private void ThenTheTeamStillHas(SeededTeam team, params string[] referenceIds)
            => Assert.That(TheStoredWorkItemsFor(team.Id).ConvertAll(issue => issue.ReferenceId),
                Is.EquivalentTo(referenceIds),
                "Every issue the query still returns has to survive the cycle - losing one is data loss the user cannot undo.");

        private void ThenTheTeamNoLongerHas(string referenceId, SeededTeam team)
            => Assert.That(TheStoredWorkItemsFor(team.Id).Exists(issue => issue.ReferenceId == referenceId), Is.False,
                $"'{referenceId}' left the query, so it must not outlive it - removal does not change under the cheaper refresh (D2).");

        private void ThenEveryStoredIssueRemembersWhenItLastChanged(SeededTeam team)
        {
            var stored = TheStoredWorkItemsFor(team.Id);

            Assert.That(stored, Is.Not.Empty, "positive control: nothing was stored, so the assertion below cannot fail.");

            foreach (var issue in stored)
            {
                Assert.That(issue.LastChangedRemote, Is.EqualTo(TheTrackersChangeStampFor(issue.ReferenceId)),
                    $"'{issue.ReferenceId}' has no remote change stamp, so the next cycle has nothing to compare and downloads everything again.");
            }
        }

        private void ThenTheUntouchedIssueIsIdenticalTo(StoredIssue before, SeededTeam team, string referenceId)
        {
            var after = TheStoredIssue(team, referenceId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(after.Values, Is.EqualTo(before.Values),
                    $"'{referenceId}' did not move on the tracker, so not one of its stored values may differ.");
                Assert.That(after.Transitions, Is.EqualTo(before.Transitions),
                    $"'{referenceId}' did not move on the tracker, so its recorded history may not be rewritten either.");
            }
        }

        private void ThenTheIssueWasReportedAsStale(SeededTeam team)
        {
            var issue = TheStoredWorkItemsFor(team.Id).Find(candidate => candidate.ReferenceId == "ITEM-1");
            var issueId = issue?.Id ?? -1;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue, Is.Not.Null, "positive control: the issue is not stored, so nothing could go stale.");
                Assert.That(CapturedEvents.Of<WorkItemBecameStale>().ConvertAll(raised => raised.WorkItemId),
                    Does.Contain(issueId),
                    "An item that stops changing is exactly the item that goes stale, and under the cheaper refresh it is "
                    + "exactly the item that stops being fetched. Evaluating staleness on the fetch loop loses it silently.");
            }
        }

        private void ThenTheFeatureReportsTheWorkThatIsLeft(SeededTeam team, int remainingItems)
        {
            var feature = TheStoredFeature(DeliveredFeature);

            Assert.That(feature, Is.Not.Null, "The feature being delivered is not stored at all.");
            Assert.That(feature!.FeatureWork.Sum(work => work.RemainingWorkItems), Is.EqualTo(remainingItems),
                "The rollup depends on the whole stored set, not on what this cycle happened to download (D9). Work entries: "
                + string.Join(" | ", feature.FeatureWork.Select(work => $"team={work.TeamId} remaining={work.RemainingWorkItems} total={work.TotalWorkItems}"))
                + " ; stored issues: "
                + string.Join(",", TheStoredWorkItemsFor(team.Id).ConvertAll(issue => $"{issue.ReferenceId}->{issue.ParentReferenceId}")));
        }

        private void ThenTheTeamsDataWasAnnouncedAsRefreshed(SeededTeam team)
            => Assert.That(CapturedEvents.Of<TeamDataRefreshed>().ConvertAll(raised => raised.TeamId),
                Does.Contain(team.Id),
                "A cheaper cycle still has to ask for a new forecast - forecasts depend on wall clock and on other teams' data.");

        // --- Then: the opt-in gate ---

        private void ThenTheCheaperRefreshIsOfferedButSwitchedOff()
        {
            var option = TheCheaperRefreshOption();

            Assert.That(option, Is.Not.Null,
                "The cheaper refresh has to be offered, or nobody can volunteer for it.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(option!.Enabled, Is.False,
                    "It ships dark: only an instance that asked for it can be hurt by a scan that loses an id.");
                Assert.That(option.IsPreview, Is.True,
                    "It is preview scaffolding with a defined end, and the screen has to say so.");
            }
        }

        // --- Reading storage and the log ---

        /// <summary>
        /// Every readable property of the stored issue except the two that are not stored with it, plus
        /// its ordered transitions. The exclusion list is a deny-list on purpose: a property added to the
        /// work item later joins the comparison on its own, which is what makes AC-2.4 a whole-surface
        /// assertion rather than a spot check that quietly stops covering the surface it names.
        /// </summary>
        private StoredIssue TheStoredIssue(SeededTeam team, string referenceId)
        {
            var issue = TheStoredWorkItemsFor(team.Id).Find(candidate => candidate.ReferenceId == referenceId);
            Assert.That(issue, Is.Not.Null, $"'{referenceId}' is not stored.");

            var values = string.Join(
                " | ",
                typeof(WorkItem)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.CanRead && !NotStoredWithTheIssue.Contains(property.Name))
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => $"{property.Name}={Render(property.GetValue(issue))}"));

            var transitions = string.Join(
                " | ",
                TheStoredTransitionsFor(issue!.Id).ConvertAll(
                    transition => $"{transition.FromState}->{transition.ToState}@{Render(transition.TransitionedAt)}"));

            return new StoredIssue(values, transitions);
        }

        /// <summary>
        /// <c>Team</c> is a navigation whose identity is already covered by <c>TeamId</c>, and rendering it
        /// would walk back into the whole graph. <c>SyncedTransitions</c> is <c>[NotMapped]</c> - it is what
        /// the connector handed over, not what is stored, and the stored history is compared separately.
        /// </summary>
        private static readonly string[] NotStoredWithTheIssue = ["Team", "SyncedTransitions"];

        private static string Render(object? value) => value switch
        {
            null => "<null>",
            string text => text,
            DateTime instant => instant.ToString("O"),
            IDictionary entries => "{" + string.Join(",", entries.Cast<DictionaryEntry>()
                .Select(entry => $"{entry.Key}:{Render(entry.Value)}")
                .OrderBy(entry => entry, StringComparer.Ordinal)) + "}",
            IEnumerable sequence => "[" + string.Join(",", sequence.Cast<object>().Select(Render)) + "]",
            _ => value.ToString() ?? "<null>",
        };

        private string RenderPayloadDownloads()
            => string.Join(" / ", PayloadDownloads.ConvertAll(request => string.Join(",", request)));

        private string TheSummaryLine()
        {
            var summaries = TheOperatorVisibleLines
                .Where(line => line.Contains(SummaryMarker, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(summaries, Is.Not.Empty,
                "No update summary was written. Operator-visible lines: " + string.Join(" | ", TheOperatorVisibleLines));

            return summaries[0];
        }
    }
}
