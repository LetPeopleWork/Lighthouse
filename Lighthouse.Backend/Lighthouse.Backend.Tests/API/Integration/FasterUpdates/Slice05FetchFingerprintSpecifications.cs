using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5687 slice 05 — a setting costs a refetch only
    /// when it changes what is fetched.
    ///
    /// Backend-observable contract: a stored fetch fingerprint covers everything the delta path skips —
    /// what the query asks the tracker for AND how the answer is read into the stored record. An edit to
    /// any of it makes the next cycle download the whole query and say so, naming configuration as the
    /// reason; an edit to anything else costs nothing at all. The same property set decides, at save
    /// time, whether the stored records have to be discarded — and only a connection change does, because
    /// only a connection change makes the same reference id a different item.
    ///
    /// The summary line's field names are slice 01's, asserted individually, and slice 05 adds one:
    /// <c>reason=</c>. Asserting the token rather than the sentence is what lets the prose improve
    /// without reding a test, and what gives a log pipeline something stable to grep.
    /// </summary>
    public partial class Slice05FetchFingerprintTest : FasterUpdatesAcceptanceTest
    {
        private const string SummaryMarker = "Update completed";
        private const string ModeField = "mode=";
        private const string ScannedField = "scanned=";
        private const string FetchedField = "fetched=";
        private const string ReasonField = "reason=";

        /// <summary>
        /// AC-5.2's reason. It is a token, not a sentence: the operator reads it, and so does whatever
        /// greps the container log.
        /// </summary>
        private const string ConfigurationChanged = "configuration-changed";

        private const string AnExtraDoingState = "In Review";
        private const string AMappedRawState = "Test";
        private const string TheReMappedName = "Done";

        private static readonly DateTime AWhileAgo = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        private readonly record struct SeededTeam(int Id, string Name, int ConnectionId);

        private readonly record struct SeededPortfolio(int Id, string Name, int ConnectionId);

        /// <summary>What the tracker is asked for, rendered so that value equality means "the query did not change".</summary>
        private readonly record struct TheQuery(string Rendered);

        /// <summary>One issue's recorded state changes, rendered so that value equality means "its history is intact".</summary>
        private readonly record struct RecordedHistory(int WorkItemId, string ReferenceId, string Rendered);

        // --- Given: the world before the edit ---

        private SeededTeam GivenATeamWhoseTrackerCanBeScanned()
        {
            var connectionId = SeedConnection();
            var teamName = $"Team {Guid.NewGuid():N}";
            var team = new SeededTeam(SeedTeam(connectionId, teamName), teamName, connectionId);

            TheTrackerCanBeScanned();

            return team;
        }

        private SeededPortfolio GivenAPortfolioWhoseTrackerCanBeScanned()
        {
            var connectionId = SeedConnection();
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";
            var portfolio = new SeededPortfolio(SeedPortfolio(connectionId, portfolioName), portfolioName, connectionId);

            TheTrackerCanBeScanned();

            return portfolio;
        }

        /// <summary>
        /// A team and a portfolio on one connection, refreshed independently. AC-5.6 is a promise about
        /// exactly this shape: two entities that share everything except the fingerprint.
        /// </summary>
        private (SeededTeam Team, SeededPortfolio Portfolio) GivenATeamAndAPortfolioOnTheSameConnection()
        {
            var connectionId = SeedConnection();
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";
            var portfolioId = SeedPortfolio(connectionId, portfolioName);
            var teamName = $"Team {Guid.NewGuid():N}";
            var teamId = SeedTeam(connectionId, teamName, portfolioId);

            TheTrackerCanBeScanned();

            return (new SeededTeam(teamId, teamName, connectionId), new SeededPortfolio(portfolioId, portfolioName, connectionId));
        }

        private void GivenTheOperatorAskedForTheCheaperRefresh() => TheOperatorAsksForTheCheaperRefresh();

        private void GivenTheTrackerHoldsThreeIssues()
            => TheTrackerHolds(
                new RemoteRecord("ITEM-1", AWhileAgo),
                new RemoteRecord("ITEM-2", AWhileAgo),
                new RemoteRecord("ITEM-3", AWhileAgo));

        private void GivenTheTrackerHoldsThreeFeatures()
            => TheTrackerHoldsFeatures(
                new RemoteRecord("FEAT-1", AWhileAgo),
                new RemoteRecord("FEAT-2", AWhileAgo),
                new RemoteRecord("FEAT-3", AWhileAgo));

        /// <summary>
        /// Pillar 2: the precondition of every edit scenario is a completed cycle, run through the same
        /// driving port with the same step method — never a hand-built row that happens to look like one.
        /// </summary>
        private Task GivenTheTeamHasAlreadyBeenRefreshed(SeededTeam team) => WhenTheScheduledRefreshRuns(team);

        private Task GivenThePortfolioHasAlreadyBeenRefreshed(SeededPortfolio portfolio) => WhenTheScheduledRefreshRuns(portfolio);

        /// <summary>
        /// The instance that upgraded into this release: its work is stored AND already carries remote
        /// change stamps, so the one thing missing is the fingerprint. Without the stamps this scenario
        /// could not tell AC-5.5 apart from slice 02's "nothing stored has a stamp" branch, which resolves
        /// to a full cycle for a completely different reason.
        /// </summary>
        private void GivenTheTeamsWorkWasStoredByAReleaseThatKnewNothingOfFingerprints(SeededTeam team)
            => SeedStoredWorkItems(
                team.Id,
                new RemoteRecord("ITEM-1", AWhileAgo) { StoredStamp = AWhileAgo },
                new RemoteRecord("ITEM-2", AWhileAgo) { StoredStamp = AWhileAgo },
                new RemoteRecord("ITEM-3", AWhileAgo) { StoredStamp = AWhileAgo });

        /// <summary>
        /// A state change the team recorded before the edit. Transitions are the part of a purge that
        /// never comes back: the tracker can be re-read, the history of how work moved cannot.
        /// </summary>
        private RecordedHistory GivenTheTeamRecordedHowItsWorkMoved(SeededTeam team, string referenceId)
        {
            var stored = TheStoredWorkItemsFor(team.Id).Find(workItem => workItem.ReferenceId == referenceId);
            Assert.That(stored, Is.Not.Null, $"'{referenceId}' is not stored for team '{team.Name}'.");

            SeedStoredTransition(stored!.Id, fromState: "New", toState: "In Progress", AWhileAgo.AddDays(-3));

            var history = TheHistoryOf(stored.Id);
            Assert.That(history, Is.Not.Empty, "positive control: with no recorded history, losing it cannot be observed.");

            return new RecordedHistory(stored.Id, referenceId, history);
        }

        /// <summary>
        /// One more state in the Doing column, written straight to storage because it is a precondition
        /// rather than the edit under test. A single-state column cannot express a re-categorisation with
        /// both columns left non-empty, which is the shape an operator would actually save.
        /// </summary>
        private void GivenTheTeamAlsoTracksAReviewState(SeededTeam team)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var stored = repository.GetById(team.Id)!;
            stored.DoingStates = ["In Progress", AnExtraDoingState];

            repository.Update(stored);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// A Doing column expressed through a mapping: the tracker's raw states are "Dev" and "Test", and
        /// both are stored under the mapped name "In Progress". This is the precondition that makes the
        /// re-mapping edit possible while leaving the query's state set untouched.
        /// </summary>
        private void GivenTheTeamReadsTwoRawStatesAsOneMappedState(SeededTeam team)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var stored = repository.GetById(team.Id)!;
            stored.StateMappings = [new StateMapping { Name = "In Progress", States = ["Dev", AMappedRawState] }];

            repository.Update(stored);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// What the portfolio holds going into the edit. The snapshot exists for its positive control:
        /// "the portfolio started from nothing" is an ABSENCE, and an absence read against a portfolio
        /// that never held anything is true before the edit as well as after it.
        /// </summary>
        private string[] GivenThePortfolioAlreadyStoresItsFeatures(SeededPortfolio portfolio)
        {
            var stored = TheFeaturesInThePortfolio(portfolio.Id).ConvertAll(feature => feature.ReferenceId);

            Assert.That(stored, Is.Not.Empty,
                "positive control: with no Features stored, a later assertion that the portfolio holds none says nothing.");

            return [.. stored];
        }

        private int GivenTheConnectionDefinesAField(SeededTeam team) => SeedAdditionalFieldDefinition(team.ConnectionId, "Story Points");

        private int GivenTheConnectionDefinesAField(SeededPortfolio portfolio) => SeedAdditionalFieldDefinition(portfolio.ConnectionId, "Story Points");

        private TheQuery GivenWhatTheTrackerIsCurrentlyAskedFor(SeededTeam team) => WhatTheTrackerIsAskedFor(team);

        // --- When: the operator edits something ---

        private Task WhenTheScheduledRefreshRuns(SeededTeam team) => TheTeamRefreshRuns(team.Id);

        private Task WhenTheScheduledRefreshRuns(SeededPortfolio portfolio) => ThePortfolioRefreshRuns(portfolio.Id);

        /// <summary>
        /// The edit goes through the Settings screen's own endpoint, not through the repository: the
        /// save-time half of this slice lives in the controller, and an edit applied underneath it would
        /// measure the fingerprint alone.
        /// </summary>
        private void WhenTheOperatorEditsTheTeamsSettings(SeededTeam team, Action<TeamSettingDto> edit)
        {
            var settings = TheTeamsCurrentSettings(team.Id);
            edit(settings);
            TheOperatorSavesTheTeamsSettings(team.Id, settings);
        }

        private void WhenTheOperatorEditsThePortfoliosSettings(SeededPortfolio portfolio, Action<PortfolioSettingDto> edit)
        {
            var settings = ThePortfoliosCurrentSettings(portfolio.Id);
            edit(settings);
            TheOperatorSavesThePortfoliosSettings(portfolio.Id, settings);
        }

        private void WhenTheOperatorChangesTheTeamsQuery(SeededTeam team)
            => WhenTheOperatorEditsTheTeamsSettings(team, settings => settings.DataRetrievalValue = "project = SOMEWHEREELSE");

        private void WhenTheOperatorChangesThePortfoliosQuery(SeededPortfolio portfolio)
            => WhenTheOperatorEditsThePortfoliosSettings(portfolio, settings => settings.DataRetrievalValue = "project = SOMEWHEREELSE");

        /// <summary>
        /// The Doing column reads "Dev" only, and the Done column reads "Done" and "Test". The raw state
        /// set is word-for-word the one the previous mapping produced - only which mapped name, and
        /// therefore which state category, "Test" lands in has moved.
        /// </summary>
        private void WhenTheOperatorReadsOneRawStateAsADifferentState(SeededTeam team)
            => WhenTheOperatorEditsTheTeamsSettings(team, settings => settings.StateMappings =
            [
                new StateMappingDto { Name = "In Progress", States = ["Dev"] },
                new StateMappingDto { Name = TheReMappedName, States = [TheReMappedName, AMappedRawState] },
            ]);

        /// <summary>
        /// A state moves from the Doing column to the Done column. The union the query is built from does
        /// not move with it, so nothing about the request changes — but every stored record in that state
        /// is now in a different state category.
        /// </summary>
        private void WhenTheOperatorMovesAStateToADifferentColumn(SeededTeam team)
            => WhenTheOperatorEditsTheTeamsSettings(team, settings =>
            {
                settings.DoingStates = ["In Progress"];
                settings.DoneStates = ["Done", AnExtraDoingState];
            });

        private void WhenTheOperatorReSavesTheSameStatesInADifferentOrder(SeededTeam team)
            => WhenTheOperatorEditsTheTeamsSettings(team, settings =>
            {
                settings.WorkItemTypes = [.. Enumerable.Reverse(settings.WorkItemTypes)];
                settings.ToDoStates = [.. Enumerable.Reverse(settings.ToDoStates)];
                settings.DoingStates = [.. Enumerable.Reverse(settings.DoingStates)];
                settings.DoneStates = [.. Enumerable.Reverse(settings.DoneStates)];
            });

        /// <summary>
        /// An edit to the CONNECTION, which no team settings save can see. What the connector reads out of
        /// a payload is defined here, so this changes what is stored for every team on the connection
        /// while changing no property of any of them.
        /// </summary>
        private void WhenTheOperatorAddsAFieldToTheConnection(SeededTeam team) => SeedAdditionalFieldDefinition(team.ConnectionId, "Risk");

        private void WhenTheOperatorMovesTheTeamToADifferentConnection(SeededTeam team)
        {
            var elsewhere = SeedConnection();
            WhenTheOperatorEditsTheTeamsSettings(team, settings => settings.WorkTrackingSystemConnectionId = elsewhere);
        }

        private void WhenTheOperatorMovesThePortfolioToADifferentConnection(SeededPortfolio portfolio)
        {
            var elsewhere = SeedConnection();
            WhenTheOperatorEditsThePortfoliosSettings(portfolio, settings => settings.WorkTrackingSystemConnectionId = elsewhere);
        }

        // --- Then: what the tracker was asked for ---

        private void ThenTheWholeQueryWasDownloaded()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FullDownloadsIssued, Is.EqualTo(1),
                    "A cycle that cannot trust what it already stored has to re-read the whole query exactly once. The records "
                    + "it now wants are records whose remote timestamp never moved, so no scan can find them - a delta cycle "
                    + "here downloads nothing and leaves every stored record answering the previous question.");
                Assert.That(PayloadDownloads, Is.Empty,
                    "A full download already asked for everything; a by-reference-id fetch on top of it is the saving handed back.");
            }
        }

        private void ThenTheWholeFeatureQueryWasDownloaded()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FullFeatureDownloadsIssued, Is.EqualTo(1),
                    "The portfolio half of the same promise: a cycle that cannot trust what it already stored re-reads the whole "
                    + "Feature query, because no scan can find a Feature whose remote timestamp never moved.");
                Assert.That(FeaturePayloadDownloads, Is.Empty,
                    "A full Feature download already asked for everything.");
            }
        }

        private void ThenNothingWasDownloaded()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FullDownloadsIssued, Is.Zero,
                    "KPI-4: an edit that changes nothing about what is fetched must cost zero payload downloads. Treating every "
                    + "settings save as an invalidation is the safe answer that wastes the whole win.");
                Assert.That(PayloadDownloads, Is.Empty,
                    "Nothing moved on the tracker either, so there is nothing to fetch by reference id.");
            }
        }

        private void ThenNoFeatureWasDownloaded()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FullFeatureDownloadsIssued, Is.Zero, "The portfolio half of KPI-4.");
                Assert.That(FeaturePayloadDownloads, Is.Empty, "No Feature moved, so nothing is fetched by reference id.");
            }
        }

        private void ThenWhatTheTrackerIsAskedForIsUnchanged(TheQuery before, SeededTeam team)
            => Assert.That(WhatTheTrackerIsAskedFor(team), Is.EqualTo(before),
                "positive control for this scenario: it only says anything if the REQUEST really is identical. The properties "
                + "reachable from PrepareQuery - the work item types, the raw state set, the query text and the cutoff - are "
                + "what a fingerprint built from 'what the query asks for' would cover, and this edit does not touch any of them.");

        // --- Then: what the operator reads and what was recorded ---

        private void ThenTheOperatorIsToldConfigurationIsWhy()
            => Assert.That(TheSummaryLine(), Does.Contain($"{ReasonField}{ConfigurationChanged}"),
                "AC-5.2: an operator who edits a query and sees a full download has to be able to tell it apart from a full "
                + "download the tracker forced. Without the reason the two are the same line, and the admin is left hoping the "
                + "edit took effect. Line: " + TheSummaryLine());

        private void ThenTheOperatorIsNotToldConfigurationIsWhy()
            => Assert.That(TheSummaryLine(), Does.Not.Contain(ConfigurationChanged),
                "This cycle is full because nothing was ever stamped, not because anybody changed a setting. A reason that is "
                + "printed whenever the mode is full names the wrong cause and is worse than no reason at all. Line: "
                + TheSummaryLine());

        private void ThenTheOperatorSeesAFullUpdate(int scanned, int fetched)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheSummaryLine(), Does.Contain($"{ModeField}{SyncMode.Full}").IgnoreCase);
                Assert.That(TheSummaryLine(), Does.Contain($"{ScannedField}{scanned}"));
                Assert.That(TheSummaryLine(), Does.Contain($"{FetchedField}{fetched}"));
            }
        }

        private void ThenTheOperatorSeesACheaperUpdate(int scanned, int fetched)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheSummaryLine(), Does.Contain($"{ModeField}{SyncMode.Delta}").IgnoreCase);
                Assert.That(TheSummaryLine(), Does.Contain($"{ScannedField}{scanned}"));
                Assert.That(TheSummaryLine(), Does.Contain($"{FetchedField}{fetched}"));
            }
        }

        private void ThenTheRefreshReportedAFullUpdateOf(SeededTeam team, int scanned, int fetched)
            => ThenTheRefreshReported(RefreshType.Team, team.Id, SyncMode.Full, scanned, fetched);

        private void ThenTheRefreshReportedACheaperUpdateOf(SeededTeam team, int scanned, int fetched)
            => ThenTheRefreshReported(RefreshType.Team, team.Id, SyncMode.Delta, scanned, fetched);

        private void ThenTheRefreshReportedAFullUpdateOf(SeededPortfolio portfolio, int scanned, int fetched)
            => ThenTheRefreshReported(RefreshType.Portfolio, portfolio.Id, SyncMode.Full, scanned, fetched);

        private void ThenTheRefreshReportedACheaperUpdateOf(SeededPortfolio portfolio, int scanned, int fetched)
            => ThenTheRefreshReported(RefreshType.Portfolio, portfolio.Id, SyncMode.Delta, scanned, fetched);

        private void ThenTheRefreshReported(RefreshType type, int entityId, SyncMode mode, int scanned, int fetched)
        {
            var recorded = TheLastRefreshLogFor(type, entityId);

            Assert.That(recorded, Is.Not.Null, "The refresh recorded nothing at all.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded!.Mode, Is.EqualTo(mode));
                Assert.That(recorded.RecordsScanned, Is.EqualTo(scanned));
                Assert.That(recorded.RecordsFetched, Is.EqualTo(fetched));
                Assert.That(recorded.Success, Is.True,
                    "A refresh that re-read the whole query because configuration moved is a successful refresh, not a failed one.");
            }
        }

        // --- Then: what the fingerprint remembers ---

        private void ThenTheTeamRemembersWhatItAskedFor(SeededTeam team)
            => Assert.That(TheStoredFetchFingerprintForTeam(team.Id), Is.Not.Null.And.Not.Empty,
                "A cycle that records no fingerprint leaves the next cycle with nothing to compare, so every later cycle is full "
                + "and the epic's second promise never arrives.");

        private void ThenThePortfolioRemembersWhatItAskedFor(SeededPortfolio portfolio)
            => Assert.That(TheStoredFetchFingerprintForPortfolio(portfolio.Id), Is.Not.Null.And.Not.Empty,
                "The portfolio half of the same promise.");

        private void ThenTheTeamsFingerprintIsUnchangedBy(string? before, SeededTeam team)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(before, Is.Not.Null.And.Not.Empty,
                    "positive control: with nothing recorded before the edit, 'the fingerprint did not change' is true of two "
                    + "nulls and says nothing about order-insensitivity at all.");

                Assert.That(TheStoredFetchFingerprintForTeam(team.Id), Is.EqualTo(before),
                    "The collections are sets, not sequences: re-saving the same states in a different order is not a change, "
                    + "and a fingerprint that says it is turns every visit to the Settings screen into a full re-download.");
            }
        }

        // --- Then: what was kept and what was discarded ---

        private void ThenTheTeamKeptTheHistoryItRecorded(RecordedHistory history)
            => Assert.That(TheHistoryOf(history.WorkItemId), Is.EqualTo(history.Rendered),
                $"'{history.ReferenceId}' is the same item on the same tracker - only the question being asked about it changed. "
                + "Discarding stored work to answer 'the query changed' throws away transition history the tracker cannot give "
                + "back, to achieve what removed = stored - fetched already achieves on the very next full cycle.");

        private void ThenTheTeamStartedFromNothing(SeededTeam team, RecordedHistory history)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredWorkItemsFor(team.Id), Is.Empty,
                    "The same reference id on a different tracker is a different item. Updating the stored copy in place merges "
                    + "two systems' work into one record, and nothing later in the cycle can tell them apart again.");
                Assert.That(TheStoredTransitionsFor(history.WorkItemId), Is.Empty,
                    "And the old system's history goes with it - keeping it would attribute the previous tracker's state changes "
                    + "to whatever the new one now calls the same id.");
            }
        }

        private void ThenThePortfolioStartedFromNothing(SeededPortfolio portfolio, string[] whatItHeld)
            => Assert.That(TheFeaturesInThePortfolio(portfolio.Id), Is.Empty,
                "The portfolio has no equivalent of the team's purge today, which is the asymmetry this slice resolves: one "
                + "question, two entities, one answer. A Feature carried across a connection change is the same reference id "
                + "claimed on a tracker that never issued it. Held before the edit: " + string.Join(",", whatItHeld));

        private void ThenThePortfolioStillHas(SeededPortfolio portfolio, params string[] referenceIds)
            => Assert.That(TheFeaturesInThePortfolio(portfolio.Id).ConvertAll(feature => feature.ReferenceId),
                Is.EquivalentTo(referenceIds),
                "The portfolio still reads the same tracker; only which Features it asks for changed. Discarding what is stored "
                + "loses every Feature's history to answer a question the next full cycle answers by itself.");

        // --- Reading storage and the log ---

        /// <summary>
        /// Everything <c>PrepareQuery</c> is handed, rendered. Order-normalised on purpose: the point of
        /// the comparison is whether the tracker is asked a different question, and a set re-saved in a
        /// different order is the same question.
        /// </summary>
        private TheQuery WhatTheTrackerIsAskedFor(SeededTeam team)
        {
            using var scope = Factory.Services.CreateScope();
            var stored = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(team.Id)!;

            return new TheQuery(string.Join(
                " | ",
                stored.DataRetrievalValue,
                string.Join(",", stored.WorkItemTypes.OrderBy(type => type, StringComparer.Ordinal)),
                string.Join(",", stored.AllStates.OrderBy(state => state, StringComparer.Ordinal)),
                stored.DoneItemsCutoffDays.ToString()));
        }

        private string TheHistoryOf(int workItemId)
            => string.Join(
                " | ",
                TheStoredTransitionsFor(workItemId).ConvertAll(
                    transition => $"{transition.FromState}->{transition.ToState}@{transition.TransitionedAt:O}"));

        private string TheSummaryLine()
        {
            var summaries = TheOperatorVisibleLines
                .Where(line => line.Contains(SummaryMarker, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(summaries, Is.Not.Empty,
                "No update summary was written. Operator-visible lines: " + string.Join(" | ", TheOperatorVisibleLines));

            return summaries[0];
        }

        // --- The edits, as a table (AC-5.2 is a promise about every property, not about one) ---

        /// <summary>
        /// One editable setting, named the way an operator would describe it. <c>Apply</c> is handed a
        /// field-definition id that already exists on the entity's connection, because three of these
        /// settings are references to one.
        /// </summary>
        public sealed record TeamEdit(string What, Action<TeamSettingDto, int> Apply)
        {
            public override string ToString() => What;
        }

        public sealed record PortfolioEdit(string What, Action<PortfolioSettingDto, int> Apply)
        {
            public override string ToString() => What;
        }

        /// <summary>
        /// The team half of the widened fingerprint set. The connection reference is deliberately absent:
        /// it has its own scenario, because it is the one edit that also discards what is stored - which
        /// would leave "nothing stored" as a second, independent reason for the next cycle to be full and
        /// make this table stop discriminating.
        /// </summary>
        private static TeamEdit[] EveryTeamSettingThatChangesWhatIsFetched() =>
        [
            new("the query itself", (settings, _) => settings.DataRetrievalValue = "project = SOMEWHEREELSE"),
            new("which kinds of work count", (settings, _) => settings.WorkItemTypes = ["Story", "Bug"]),
            new("which states mean not started", (settings, _) => settings.ToDoStates = ["New", "Proposed"]),
            new("which states mean in progress", (settings, _) => settings.DoingStates = ["In Progress", "Committed"]),
            new("which states mean finished", (settings, _) => settings.DoneStates = ["Done", "Closed"]),
            new("how far back finished work still counts", (settings, _) => settings.DoneItemsCutoffDays = 90),
            new("which field names the parent", (settings, fieldId) => settings.ParentOverrideAdditionalFieldDefinitionId = fieldId),
            new("how one tracker state is read", (settings, _) => settings.StateMappings =
                [new StateMappingDto { Name = "In Progress", States = ["Dev", "Test"] }]),
        ];

        private static PortfolioEdit[] EveryPortfolioSettingThatChangesWhatIsFetched() =>
        [
            new("the query itself", (settings, _) => settings.DataRetrievalValue = "project = SOMEWHEREELSE"),
            new("which kinds of work count", (settings, _) => settings.WorkItemTypes = ["Epic", "Initiative"]),
            new("which states mean not started", (settings, _) => settings.ToDoStates = ["New", "Proposed"]),
            new("which states mean in progress", (settings, _) => settings.DoingStates = ["In Progress", "Committed"]),
            new("which states mean finished", (settings, _) => settings.DoneStates = ["Done", "Closed"]),
            new("how far back finished work still counts", (settings, _) => settings.DoneItemsCutoffDays = 90),
            new("which field names the parent", (settings, fieldId) => settings.ParentOverrideAdditionalFieldDefinitionId = fieldId),
            new("which field names the owning team", (settings, fieldId) => settings.FeatureOwnerAdditionalFieldDefinitionId = fieldId),
            new("which field carries the size estimate", (settings, fieldId) => settings.SizeEstimateAdditionalFieldDefinitionId = fieldId),
            new("how one tracker state is read", (settings, _) => settings.StateMappings =
                [new StateMappingDto { Name = "In Progress", States = ["Dev", "Test"] }]),
        ];

        /// <summary>AC-5.3's free list: everything an admin can tune without asking the tracker anything.</summary>
        private static TeamEdit[] EveryTeamSettingThatChangesNothingAboutWhatIsFetched() =>
        [
            new("a wait state", (settings, _) => settings.WaitStates = ["In Progress"]),
            new("a blocked rule", (settings, _) => settings.BlockedRuleSetJson = ABlockedRuleSetMatching("In Progress")),
            new("the staleness threshold", (settings, _) => settings.StalenessThresholdDays = 7),
            new("the blocked staleness threshold", (settings, _) => settings.BlockedStalenessThresholdDays = 14),
            new("a named cycle time", (settings, _) => settings.CycleTimeDefinitions =
                [new CycleTimeDefinitionDto { Name = "Delivery", StartState = "New", EndState = "Done" }]),
            new("the service level expectation", (settings, _) =>
            {
                settings.ServiceLevelExpectationProbability = 85;
                settings.ServiceLevelExpectationRange = 10;
            }),
            new("the system WIP limit", (settings, _) => settings.SystemWIPLimit = 5),
            new("the throughput window", (settings, _) => settings.ThroughputHistory = 60),
            new("how many features the team works on at once", (settings, _) => settings.FeatureWIP = 3),
            new("how estimates are read", (settings, fieldId) =>
            {
                settings.EstimationAdditionalFieldDefinitionId = fieldId;
                settings.EstimationUnit = "points";
            }),
        ];

        private static PortfolioEdit[] EveryPortfolioSettingThatChangesNothingAboutWhatIsFetched() =>
        [
            new("a wait state", (settings, _) => settings.WaitStates = ["In Progress"]),
            new("the staleness threshold", (settings, _) => settings.StalenessThresholdDays = 7),
            new("how big an unbroken-down feature is assumed to be", (settings, _) => settings.DefaultAmountOfWorkItemsPerFeature = 40),
            new("which states are sized by that assumption anyway", (settings, _) => settings.OverrideRealChildCountStates = ["In Progress"]),
            new("whether that assumption comes from a percentile", (settings, _) =>
            {
                settings.UsePercentileToCalculateDefaultAmountOfWorkItems = true;
                settings.DefaultWorkItemPercentile = 70;
            }),
            new("the service level expectation", (settings, _) =>
            {
                settings.ServiceLevelExpectationProbability = 85;
                settings.ServiceLevelExpectationRange = 10;
            }),
        ];

        private static string ABlockedRuleSetMatching(string state)
            => System.Text.Json.JsonSerializer.Serialize(new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = "workitem.state", Operator = RuleOperators.Equals, Value = state }],
            });
    }
}
