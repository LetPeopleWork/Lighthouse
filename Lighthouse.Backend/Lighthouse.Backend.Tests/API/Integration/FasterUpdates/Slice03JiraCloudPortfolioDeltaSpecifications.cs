using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using NUnit.Framework;
using Serilog.Events;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5687 slice 03 — a Jira Cloud portfolio refresh
    /// fetches only the Features that moved.
    ///
    /// Backend-observable contract: the Feature query is still enumerated in full every cycle, so
    /// <c>removed = stored − scanned</c> keeps today's meaning (D2); full Feature payloads are downloaded
    /// only for records whose remote change stamp differs from the stored one (D12); the parent-Feature
    /// path sweeps the keys the portfolio STORES and downloads only the parents that moved; a Feature that
    /// did not move keeps its portfolio claim, its history and its open blocked spells; and remaining
    /// work, extrapolation, the percentile default size and the forecast trigger recompute every cycle
    /// regardless of mode (D9).
    ///
    /// The summary line's field names are slice 01's, asserted individually: they are what a log pipeline
    /// greps for, and only the value behind <c>mode</c> changes here.
    /// </summary>
    public partial class Slice03JiraCloudPortfolioDeltaTest : FasterUpdatesAcceptanceTest
    {
        private const string SummaryMarker = "Update completed";
        private const string ModeField = "mode=";
        private const string ScannedField = "scanned=";
        private const string FetchedField = "fetched=";

        private const string TheScansRefusal = "The identity scan was refused by the work tracking system";
        private const string TheParentScansRefusal = "The parent identity scan was refused by the work tracking system";

        private const string TheParentFeature = "PARENT-1";
        private const string TheParentsNewName = "The parent feature, renamed";
        private const string TheDeliveredFeature = "FEAT-1";
        private const string TheDepartingFeature = "FEAT-3";

        private static readonly DateTime AWhileAgo = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        private readonly record struct SeededPortfolio(int Id, string Name, int TeamId);

        /// <summary>A Feature as storage knows it, once a cycle has given it an id.</summary>
        private readonly record struct StoredFeature(int Id, string ReferenceId);

        // --- Given ---

        private SeededPortfolio GivenAPortfolioWhoseTrackerCanBeScanned()
        {
            var portfolio = SeedAPortfolio();
            TheTrackerCanBeScanned();

            return portfolio;
        }

        /// <summary>
        /// A Jira Data Center portfolio, in effect: the connector answers that it cannot be swept, so the
        /// cheap path is refused per connector no matter what the instance volunteered (A1).
        /// </summary>
        private SeededPortfolio GivenAPortfolioWhoseTrackerRefusesToBeScanned() => SeedAPortfolio();

        private (SeededPortfolio First, SeededPortfolio Second) GivenTwoPortfoliosThatTrackTheSameFeatures()
        {
            var connectionId = SeedConnection();
            var first = SeedAPortfolio(connectionId);
            var second = SeedAPortfolio(connectionId);
            TheTrackerCanBeScanned();

            return (first, second);
        }

        /// <summary>
        /// A portfolio a team delivers work for. The team is what the rollup and the extrapolation split
        /// work across, and it is the team's own refresh - on its own schedule - that changes what is left
        /// under a Feature whose own record never moved.
        /// </summary>
        private SeededPortfolio GivenAPortfolioDeliveredByOneTeam()
        {
            var connectionId = SeedConnection();
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";
            var portfolioId = SeedPortfolio(connectionId, portfolioName);
            var teamId = SeedTeam(connectionId, $"Team {Guid.NewGuid():N}", portfolioId);

            TheTrackerCanBeScanned();

            return new SeededPortfolio(portfolioId, portfolioName, teamId);
        }

        private SeededPortfolio SeedAPortfolio(int? connectionId = null)
        {
            var connection = connectionId ?? SeedConnection();
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";

            return new SeededPortfolio(SeedPortfolio(connection, portfolioName), portfolioName, TeamId: 0);
        }

        private void GivenTheTrackerHoldsThreeFeatures()
            => TheTrackerHoldsFeatures(
                new RemoteRecord("FEAT-1", AWhileAgo),
                new RemoteRecord("FEAT-2", AWhileAgo),
                new RemoteRecord("FEAT-3", AWhileAgo));

        private void GivenTheTrackerHoldsTwoFeatures()
            => TheTrackerHoldsFeatures(
                new RemoteRecord("FEAT-1", AWhileAgo),
                new RemoteRecord("FEAT-2", AWhileAgo));

        /// <summary>
        /// A third Feature starts being returned by the query the portfolios share - and only that. The
        /// tracker is re-stated from one whole picture rather than nudged, so the two Features already
        /// there keep the change stamp they had: the arrival is the only thing that can have moved.
        /// </summary>
        private void GivenAThirdFeatureStartsBeingReturnedByTheQuery() => GivenTheTrackerHoldsThreeFeatures();

        private void GivenTheTrackerHoldsTwoFeaturesUnderOneParent()
        {
            TheTrackerHoldsFeatures(
                new RemoteRecord("FEAT-1", AWhileAgo) { ParentReferenceId = TheParentFeature },
                new RemoteRecord("FEAT-2", AWhileAgo) { ParentReferenceId = TheParentFeature });

            TheTrackerHoldsParentFeatures(new RemoteRecord(TheParentFeature, AWhileAgo) { Name = "The parent feature" });
        }

        private void GivenTheTrackerHoldsOneFeatureWithNoWorkOnItYet()
            => TheTrackerHoldsFeatures(new RemoteRecord(TheDeliveredFeature, AWhileAgo) { Name = "The feature being delivered" });

        /// <summary>
        /// The upgrade case (D8): the portfolio's Features are already stored, and none of them carries a
        /// remote change stamp, because the release that records one is the one being installed.
        /// </summary>
        private void GivenThePortfoliosFeaturesWereStoredBeforeThisRelease(SeededPortfolio portfolio, params string[] referenceIds)
            => SeedStoredFeatures(
                portfolio.Id,
                [.. referenceIds.Select(referenceId => new RemoteRecord(referenceId, AWhileAgo) { StoredStamp = null })]);

        /// <summary>
        /// Pillar 2: the second cycle's precondition is the first cycle, run through the same driving port
        /// with the same step method — not a hand-built row that happens to look like its result.
        /// </summary>
        private Task GivenThePortfolioHasAlreadyBeenRefreshed(SeededPortfolio portfolio) => WhenTheScheduledRefreshRuns(portfolio);

        private void GivenOneFeatureMovedOnTheTracker(string referenceId)
            => OnTheTrackerTheFeatureChanges(referenceId, AWhileAgo.AddHours(1), state: "Done");

        private void GivenOneFeatureLeftTheQuery(string referenceId) => OnTheTrackerTheFeatureIsGone(referenceId);

        private void GivenTheFeatureScanFails() => TheFeatureScanFails(new InvalidOperationException(TheScansRefusal));

        private void GivenTheParentFeatureScanFails() => TheParentFeatureScanFails(new InvalidOperationException(TheParentScansRefusal));

        /// <summary>
        /// The parent's own record moves while both its children stay exactly where they were. Nothing on
        /// the Feature side of the cycle can report this, which is why the parent half sweeps at all.
        /// </summary>
        private void GivenTheParentFeatureWasRenamedOnTheTracker()
            => OnTheTrackerTheParentFeatureChanges(TheParentFeature, AWhileAgo.AddHours(1), name: TheParentsNewName);

        private void GivenTheParentFeatureQueryStoppedAnsweringForIt() => OnTheTrackerTheParentFeatureIsGone(TheParentFeature);

        private void GivenTheOperatorAskedForTheCheaperRefresh() => TheOperatorAsksForTheCheaperRefresh();

        /// <summary>
        /// The positive control the gate scenario leans on. It asserts the option is OFFERED as well as
        /// off: "no row" and "a row that is off" answer the same to a refresh, so a null-tolerant check
        /// would let the seeder entry disappear without a single scenario turning red.
        /// </summary>
        private void GivenNobodyAskedForTheCheaperRefresh()
        {
            var option = TheCheaperRefreshOption();

            Assert.That(option, Is.Not.Null,
                "The cheaper refresh is not offered at all, so 'nobody asked for it' is not the default being tested - it is an absent option.");
            Assert.That(option!.Enabled, Is.False,
                "The default has to be off, or the scenario is not testing the default.");
        }

        /// <summary>
        /// A Feature that has been blocked in this portfolio since before the cycle under test. A spell is
        /// what the departed-spell sweep closes for every Feature missing from the refreshed list, so an
        /// open one is the only way to see a cycle close spells for Features it merely did not refetch.
        /// </summary>
        private StoredFeature GivenOneFeatureHasBeenBlockedForAWhile(SeededPortfolio portfolio, string referenceId)
        {
            var feature = TheFeature(referenceId);
            AFeatureIsAlreadyBlockedInThePortfolio(portfolio.Id, feature.Id, AWhileAgo.AddDays(-3));

            return feature;
        }

        private string GivenHowTheUntouchedFeaturesHistoryLooksNow(StoredFeature feature) => TheHistoryOf(feature);

        private void GivenTheTeamHasSinceBrokenTheFeatureDownIntoThreeItems(SeededPortfolio portfolio)
            => SeedStoredWorkItems(
                portfolio.TeamId,
                new RemoteRecord("ITEM-1", AWhileAgo) { ParentReferenceId = TheDeliveredFeature },
                new RemoteRecord("ITEM-2", AWhileAgo) { ParentReferenceId = TheDeliveredFeature },
                new RemoteRecord("ITEM-3", AWhileAgo) { ParentReferenceId = TheDeliveredFeature, State = "Done", StateCategory = StateCategories.Done });

        // --- When ---

        /// <summary>
        /// The portfolio is a parameter of every step that needs it, never a field one step writes and
        /// another reads: a Given that reads what a When assigns runs before the assignment and sees
        /// nothing (slice 02, DT2-13).
        /// </summary>
        private Task WhenTheScheduledRefreshRuns(SeededPortfolio portfolio) => ThePortfolioRefreshRuns(portfolio.Id);

        // --- Then: what the tracker was asked for ---

        private void ThenTheWholeFeatureQueryWasDownloaded()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FullFeatureDownloadsIssued, Is.EqualTo(1),
                    "A full update downloads the whole Feature query exactly once.");
                Assert.That(FeaturePayloadDownloads, Is.Empty,
                    "A full update has nothing to fetch by reference id - it already asked for everything.");
            }
        }

        private void ThenTheWholeFeatureQueryWasScannedForIdentitiesOnly()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FeatureScansIssued, Is.EqualTo(1),
                    "The cheap scan is the whole point, and it enumerates the same query the full download does.");
                Assert.That(FullFeatureDownloadsIssued, Is.Zero,
                    "Scanning and then downloading every Feature anyway costs more than not scanning at all.");
            }
        }

        private void ThenTheTrackersFeaturesWereNeverScanned()
            => Assert.That(FeatureScansIssued, Is.Zero,
                "Nobody asked for the cheaper refresh, so the Feature query must not be scanned at all - and slice 03 adds no "
                + "second opt-in, so the gate the team half already has is the one that has to cover this.");

        private void ThenOnlyTheFeaturesThatMovedWereDownloaded(params string[] referenceIds)
        {
            Assert.That(FeaturePayloadDownloads, Has.Count.EqualTo(1),
                "One cycle asks for the changed Feature payloads once. Requests: " + RenderFeatureDownloads());

            Assert.That(FeaturePayloadDownloads[0], Is.EquivalentTo(referenceIds),
                "Downloading a Feature whose timestamp did not move is the cost this slice exists to remove. Requested: "
                + RenderFeatureDownloads());
        }

        private void ThenTheSecondPortfolioDidNotDownloadTheFeatureAgain()
            => Assert.That(FeaturePayloadDownloads, Is.Empty,
                "A Feature two portfolios claim is one stored record carrying one remote change stamp. Once the first "
                + "portfolio's cycle has refreshed it, the second portfolio's cycle finds it current and must not pay for it "
                + "a second time. Requested: " + RenderFeatureDownloads());

        private void ThenTheParentFeaturesWereScannedFor(params string[] parentReferenceIds)
        {
            Assert.That(ParentFeatureScans, Has.Count.EqualTo(1),
                "The parent path is a keyed query, so its scan is that same query asking only for identity. Scans: "
                + RenderParentScans());

            Assert.That(ParentFeatureScans[0], Is.EquivalentTo(parentReferenceIds),
                "The parent key list is derived from what the portfolio STORES. Derived from what this cycle fetched it "
                + "shrinks to nothing on a quiet cycle, and the parents drop out without a single other assertion noticing. "
                + "Scanned: " + RenderParentScans());
        }

        private void ThenNoParentFeatureWasDownloaded()
            => Assert.That(ParentFeatureDownloads, Is.Empty,
                "No child moved, so no parent moved either - and a keyed query for an empty key set still costs a remote "
                + "round trip on every quiet cycle, which is the cost this epic exists to remove. Requested: "
                + RenderParentDownloads());

        private void ThenTheParentFeaturesWereNeverScanned()
            => Assert.That(ParentFeatureScans, Is.Empty,
                "The parent half carries no opt-in of its own - it rides the one the Feature half already reads. A scan "
                + "issued here is a remote round trip nobody volunteered for, and it costs the same whether or not the "
                + "answer is then used. Scans: " + RenderParentScans());

        private void ThenTheParentFeaturesWereDownloaded(params string[] parentReferenceIds)
        {
            Assert.That(ParentFeatureDownloads, Has.Count.EqualTo(1),
                "One cycle asks for the parent payloads once. Requests: " + RenderParentDownloads());

            Assert.That(ParentFeatureDownloads[0], Is.EquivalentTo(parentReferenceIds),
                "A parent this cycle had to fetch and did not is a parent whose stored copy is now wrong, and nothing "
                + "later in the cycle re-reads it. Requested: " + RenderParentDownloads());
        }

        // --- Then: what the operator reads and what was recorded ---

        private void ThenTheOperatorSeesACheaperUpdate(int scanned, int fetched)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheSummaryLine(), Does.Contain($"{ModeField}delta").IgnoreCase,
                    "Slice 01 shipped the field so the later slices could change the value behind it.");
                Assert.That(TheSummaryLine(), Does.Contain($"{ScannedField}{scanned}"),
                    "How much of the Feature query was still enumerated is what tells the operator removals are still caught.");
                Assert.That(TheSummaryLine(), Does.Contain($"{FetchedField}{fetched}"),
                    "How little was downloaded is the number that answers whether portfolios can share the team's refresh interval.");
            }
        }

        private void ThenTheRefreshReportedAFullUpdateOf(SeededPortfolio portfolio, int scanned, int fetched)
            => ThenTheRefreshReported(portfolio, SyncMode.Full, scanned, fetched);

        private void ThenTheRefreshReportedACheaperUpdateOf(SeededPortfolio portfolio, int scanned, int fetched)
            => ThenTheRefreshReported(portfolio, SyncMode.Delta, scanned, fetched);

        private void ThenTheRefreshReported(SeededPortfolio portfolio, SyncMode mode, int scanned, int fetched)
        {
            var recorded = TheLastRefreshLogFor(RefreshType.Portfolio, portfolio.Id);

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

        private void ThenTheOperatorIsToldTheParentScanFailed()
            => Assert.That(CapturedLogs.AtOrAbove(LogEventLevel.Warning), Has.One.Contains(TheParentScansRefusal),
                "The parent half falls back on its own and has to say so on its own - the Feature half's line says nothing "
                + "about the parents, so a silent parent fallback is a saving quietly handed back. Lines: "
                + string.Join(" | ", CapturedLogs.AtOrAbove(LogEventLevel.Warning)));

        // --- Then: what the portfolio still holds ---

        private void ThenThePortfolioStillHas(SeededPortfolio portfolio, params string[] referenceIds)
            => Assert.That(TheFeaturesInThePortfolio(portfolio.Id).ConvertAll(feature => feature.ReferenceId),
                Is.EquivalentTo(referenceIds),
                "The portfolio is rebuilt from the list the refresh hands it, so under the cheaper refresh the Features at "
                + "risk are the ones that did NOT move. A Feature that loses its last portfolio claim is deleted outright by "
                + "the orphaned-Feature cleanup - data loss on a green sync.");

        private void ThenThePortfolioNoLongerHas(SeededPortfolio portfolio, string referenceId)
            => Assert.That(TheFeaturesInThePortfolio(portfolio.Id).Exists(feature => feature.ReferenceId == referenceId), Is.False,
                $"'{referenceId}' left the query, so it must not outlive it - removal does not change under the cheaper refresh (D2).");

        private void ThenTheUntouchedFeatureIsStillStored(string referenceId)
            => Assert.That(TheStoredFeature(referenceId), Is.Not.Null,
                $"'{referenceId}' did not move on the tracker and never left the query, so it must still exist. A Feature "
                + "dropped from the portfolio is not merely hidden - the cleanup pass deletes the row.");

        private void ThenTheDepartedFeatureIsStillStored(string referenceId)
            => Assert.That(TheStoredFeature(referenceId), Is.Not.Null,
                $"'{referenceId}' left ONE portfolio's query, not every portfolio's. The orphaned-Feature cleanup deletes what "
                + "no portfolio claims AT ALL, so a Feature another portfolio still holds has to survive losing this one's "
                + "claim - and losing it is a hard DELETE, not a hidden row.");

        private void ThenEveryFeatureInThePortfolioRemembersWhenItLastChanged(SeededPortfolio portfolio)
        {
            var stored = TheFeaturesInThePortfolio(portfolio.Id);

            Assert.That(stored, Is.Not.Empty, "positive control: the portfolio holds nothing, so the assertion below cannot fail.");

            foreach (var feature in stored)
            {
                Assert.That(feature.LastChangedRemote, Is.EqualTo(TheTrackersChangeStampForFeature(feature.ReferenceId)),
                    $"'{feature.ReferenceId}' has no remote change stamp, so the next cycle has nothing to compare and downloads every Feature again.");
            }
        }

        private void ThenTheParentFeatureIsStillStoredAndCurrent(string referenceId)
        {
            var parent = TheStoredFeature(referenceId);

            Assert.That(parent, Is.Not.Null,
                $"'{referenceId}' is a parent of Features the portfolio still holds, so a quiet cycle must not lose it.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(parent!.IsParentFeature, Is.True,
                    "A parent that stops being marked as one re-enters blocked-spell edge detection, which it is deliberately excluded from.");
                Assert.That(parent.LastChangedRemote, Is.EqualTo(TheTrackersChangeStampForFeature(referenceId)),
                    "A parent whose stamp is lost is refetched in full on every later cycle - the saving handed straight back.");
            }
        }

        private void ThenTheParentFeatureShowsWhatTheTrackerNowSays(string referenceId, string name)
        {
            var parent = TheStoredFeature(referenceId);

            Assert.That(parent, Is.Not.Null, $"'{referenceId}' is not stored at all.");
            Assert.That(parent!.Name, Is.EqualTo(name),
                "A parent whose own record moved is the one thing the children can never report, so a cycle that skips it "
                + "leaves the portfolio showing a name, a state and an owner the tracker stopped agreeing with - and every "
                + "later cheap cycle agrees with the stale copy all over again.");
        }

        private void ThenTheParentFeatureIsStillStored(string referenceId)
            => Assert.That(TheStoredFeature(referenceId), Is.Not.Null,
                $"The keyed query stopped answering for '{referenceId}', which is not the same as the tracker saying it is "
                + "gone. Parents are excluded from the orphaned-Feature cleanup precisely so that silence cannot delete "
                + "one, and the Feature half's 'stored minus swept' rule is inverted here for the same reason.");

        private void ThenTheUntouchedFeaturesHistoryIsIdenticalTo(string before, StoredFeature feature)
            => Assert.That(TheHistoryOf(feature), Is.EqualTo(before),
                $"'{feature.ReferenceId}' did not move on the tracker, so its recorded history may not be rewritten.");

        private void ThenNothingWasDeclaredUnblocked()
            => Assert.That(CapturedEvents.Of<FeatureUnblocked>(), Is.Empty,
                "The departed-spell sweep closes a spell for every Feature missing from the list the refresh hands it. Under "
                + "the cheaper refresh that list holds only the Features that moved, so every still-blocked Feature that "
                + "happened to be quiet is declared unblocked - a blocked-time history rewritten by a fetch optimisation. "
                + "Declared unblocked: " + string.Join(",", CapturedEvents.Of<FeatureUnblocked>().Select(raised => raised.FeatureId)));

        private void ThenTheFeatureIsStillBlockedInThePortfolio(SeededPortfolio portfolio, StoredFeature feature)
            => Assert.That(TheOpenBlockedSpellsInThePortfolio(portfolio.Id).Keys, Does.Contain(feature.Id),
                $"'{feature.ReferenceId}' was blocked before this cycle and nothing about it changed, so its spell is still open.");

        private void ThenTheDepartedFeaturesBlockedSpellWasClosed(SeededPortfolio portfolio, StoredFeature feature)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CapturedEvents.Of<FeatureUnblocked>().ConvertAll(raised => raised.FeatureId),
                    Does.Contain(feature.Id),
                    $"'{feature.ReferenceId}' left this portfolio's query while it was blocked. Nothing later in the cycle "
                    + "visits a Feature the refresh no longer holds, so the sweep over what departed is the only thing that "
                    + "can end the spell - and a spell nobody ends keeps accruing blocked time against a Feature this "
                    + "portfolio stopped tracking. Declared unblocked: "
                    + string.Join(",", CapturedEvents.Of<FeatureUnblocked>().Select(raised => raised.FeatureId)));

                Assert.That(TheOpenBlockedSpellsInThePortfolio(portfolio.Id).Keys, Does.Not.Contain(feature.Id),
                    "The signal has to be acted on, not merely raised: an open spell is what every blocked-time number reads.");
            }
        }

        private void ThenBothPortfoliosShowTheFeatureAsFinished(SeededPortfolio first, SeededPortfolio second, string referenceId)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheFeatureAsSeenBy(first, referenceId).State, Is.EqualTo("Done"),
                    "The portfolio whose cycle downloaded it shows what the tracker now says.");
                Assert.That(TheFeatureAsSeenBy(second, referenceId).State, Is.EqualTo("Done"),
                    "So does the portfolio that did not have to download it - one stored record, claimed by both.");
            }
        }

        // --- Then: the work that is left (D9) ---

        private void ThenTheFeatureWasSizedByTheDefaultBecauseItHasNoWork()
        {
            var feature = TheStoredFeature(TheDeliveredFeature);

            Assert.That(feature, Is.Not.Null, "The feature being delivered is not stored at all.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature!.IsUsingDefaultFeatureSize, Is.True,
                    "positive control: the baseline this scenario measures against is a Feature sized by the default.");
                Assert.That(feature.FeatureWork.Sum(work => work.TotalWorkItems), Is.GreaterThan(0),
                    "positive control: a default size of nothing would make the later comparison vacuous.");
            }
        }

        private void ThenTheFeatureReportsTheWorkThatIsLeft(int remainingItems)
        {
            var feature = TheStoredFeature(TheDeliveredFeature);

            Assert.That(feature, Is.Not.Null, "The feature being delivered is not stored at all.");
            Assert.That(feature!.FeatureWork.Sum(work => work.RemainingWorkItems), Is.EqualTo(remainingItems),
                "What is left under a Feature is derived from the whole stored set of work items, which the delivering team's "
                + "own refresh changes on its own schedule. 'The Feature record did not move remotely' says nothing about its "
                + "rollup, and confusing the two leaves the portfolio's numbers stale while every Feature row looks fresh (D9). "
                + "Work entries: "
                + string.Join(" | ", feature.FeatureWork.Select(work => $"team={work.TeamId} remaining={work.RemainingWorkItems} total={work.TotalWorkItems}")));
        }

        private void ThenTheFeatureIsNoLongerSizedByTheDefault()
            => Assert.That(TheStoredFeature(TheDeliveredFeature)!.IsUsingDefaultFeatureSize, Is.False,
                "A Feature that has since been broken down is no longer an estimate, and the extrapolation that says so runs "
                + "every cycle regardless of how much was fetched (D9).");

        private void ThenTheForecastsWereAskedForAgain(SeededPortfolio portfolio)
            => Assert.That(CapturedEvents.Of<PortfolioForecastsUpdated>().ConvertAll(raised => raised.PortfolioId),
                Does.Contain(portfolio.Id),
                "A cheaper cycle still has to ask for a new forecast - forecasts depend on wall clock and on other teams' throughput.");

        // --- Reading storage and the log ---

        private StoredFeature TheFeature(string referenceId)
        {
            var feature = TheStoredFeature(referenceId);
            Assert.That(feature, Is.Not.Null, $"'{referenceId}' is not stored.");

            return new StoredFeature(feature!.Id, referenceId);
        }

        private Feature TheFeatureAsSeenBy(SeededPortfolio portfolio, string referenceId)
        {
            var feature = TheFeaturesInThePortfolio(portfolio.Id).Find(candidate => candidate.ReferenceId == referenceId);
            Assert.That(feature, Is.Not.Null, $"Portfolio '{portfolio.Name}' does not hold '{referenceId}'.");

            return feature!;
        }

        private string TheHistoryOf(StoredFeature feature)
            => string.Join(
                " | ",
                TheStoredTransitionsForFeature(feature.Id).ConvertAll(
                    transition => $"{transition.FromState}->{transition.ToState}@{transition.TransitionedAt:O}"));

        private string RenderFeatureDownloads()
            => string.Join(" / ", FeaturePayloadDownloads.ConvertAll(request => string.Join(",", request)));

        private string RenderParentScans()
            => string.Join(" / ", ParentFeatureScans.ConvertAll(request => string.Join(",", request)));

        private string RenderParentDownloads()
            => string.Join(" / ", ParentFeatureDownloads.ConvertAll(request => string.Join(",", request)));

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
