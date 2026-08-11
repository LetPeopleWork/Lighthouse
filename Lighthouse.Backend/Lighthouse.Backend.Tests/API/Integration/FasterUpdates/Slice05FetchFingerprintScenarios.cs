using Lighthouse.Backend.API.DTO;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5687 — Faster Updates), slice 05: a setting costs a refetch
    /// only when it changes what is fetched. Driving ports: the Settings screen's own save endpoint, and
    /// the scheduled refresh. US-05, AC-5.1 … AC-5.6, plus amendment A2.
    ///
    /// This slice is a correctness gate over everything delta has shipped, not an optimisation. Until it
    /// lands, an operator on an opted-in instance can widen a query and get no refetch at all, because
    /// <c>fetchShapeChanged</c> is hard-coded false in three places.
    ///
    /// Two things here are wider than the brief, and both are why the fixture is shaped as it is:
    ///
    /// 1. <b>The fingerprint covers what shapes the stored RECORD, not only what shapes the query.</b>
    ///    Delta skips the whole of an unchanged record's derivation, so a state mapping, a connection
    ///    field definition or a portfolio's owner/size field reference is every bit as fetch-shaping as
    ///    the query text — and none of them is reachable from <c>PrepareQuery</c>. Scenarios 5, 6 and 7
    ///    are the three edits that prove it: each one leaves the request byte-identical.
    /// 2. <b>The purge is narrowed to a connection change.</b> A query edit already reconciles itself
    ///    through <c>removed = stored − fetched</c> on the next full cycle, which is exactly how the
    ///    portfolio side copes today without a purge. A connection change does not: the same reference id
    ///    on a different tracker is a different item, and the update path merges it into the stored copy
    ///    in place. Scenarios 14-17 are that pair, once per entity.
    ///
    /// Every scenario asserts its headline claim BEFORE the mode, so the failure a reader sees is the
    /// failure the scenario is named after (slice 03, DT3-11).
    ///
    /// Every scenario ships [Ignore]d. DELIVER un-ignores one at a time; each is one TDD cycle.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5687-faster-updates")]
    [Category("slice-05")]
    public partial class Slice05FetchFingerprintTest
    {
        private const string Pending = "DISTILL scaffold — slice 05 is not implemented yet.";

        // @walking_skeleton @driving_port @real-io @AC-5.1 @AC-5.2 @contract-shape:bounded-change
        // The promise the slice is named after, in the one shape an operator recognises: edit the query,
        // save, and the next cycle really does go and get the new answer.
        [Test]
        public async Task A_query_edit_makes_the_next_refresh_download_everything_again()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            WhenTheOperatorChangesTheTeamsQuery(team);
            await WhenTheScheduledRefreshRuns(team);

            ThenTheWholeQueryWasDownloaded();
            ThenTheOperatorSeesAFullUpdate(scanned: 3, fetched: 3);
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
            ThenTheTeamRemembersWhatItAskedFor(team);
        }

        // @driving_port @real-io @AC-5.2 @contract-shape:bounded-change
        // Without the reason, a configuration-forced full download and a tracker-forced one are the same
        // line, and the admin is left hoping the edit took effect rather than reading that it did.
        [Test]
        public async Task The_operator_is_told_that_a_configuration_change_is_why_everything_was_downloaded()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            WhenTheOperatorChangesTheTeamsQuery(team);
            await WhenTheScheduledRefreshRuns(team);

            ThenTheOperatorIsToldConfigurationIsWhy();
            ThenTheOperatorSeesAFullUpdate(scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @AC-5.1 @AC-5.2 @contract-shape:bounded-change
        // AC-5.2 is a promise about EVERY fetch-shaping property, and a fingerprint that covers six of
        // seven fails silently on the seventh: delta serves a stale result set with every test green.
        [Test]
        [TestCaseSource(nameof(EveryTeamSettingThatChangesWhatIsFetched))]
        public async Task An_edit_to_a_fetch_shaping_team_setting_makes_the_next_refresh_download_everything(TeamEdit edit)
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            var fieldId = GivenTheConnectionDefinesAField(team);
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            WhenTheOperatorEditsTheTeamsSettings(team, settings => edit.Apply(settings, fieldId));
            await WhenTheScheduledRefreshRuns(team);

            ThenTheWholeQueryWasDownloaded();
            ThenTheOperatorIsToldConfigurationIsWhy();
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @AC-5.1 @AC-5.2 @AC-5.6 @contract-shape:bounded-change
        // The portfolio carries three fetch-shaping settings the team does not, and today it carries no
        // change detection at all - the asymmetry A2 says this slice resolves.
        [Test]
        [Ignore(Pending)]
        [TestCaseSource(nameof(EveryPortfolioSettingThatChangesWhatIsFetched))]
        public async Task An_edit_to_a_fetch_shaping_portfolio_setting_makes_the_next_refresh_download_every_feature(PortfolioEdit edit)
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            var fieldId = GivenTheConnectionDefinesAField(portfolio);
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            WhenTheOperatorEditsThePortfoliosSettings(portfolio, settings => edit.Apply(settings, fieldId));
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheWholeFeatureQueryWasDownloaded();
            ThenTheOperatorIsToldConfigurationIsWhy();
            ThenTheRefreshReportedAFullUpdateOf(portfolio, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @AC-5.1 @contract-shape:bounded-change
        // The edit that falsifies "the fetch-shaping set is what PrepareQuery is handed". Moving a raw
        // state from one mapping to another leaves the query's state set identical and changes the state
        // - and the state category - stored against every record in it. Under delta those records are
        // never re-derived, so the portfolio quietly shows a state the tracker stopped agreeing with.
        [Test]
        public async Task Reading_a_tracker_state_as_a_different_state_makes_the_next_refresh_download_everything_even_though_the_query_is_unchanged()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheTeamReadsTwoRawStatesAsOneMappedState(team);
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);
            var history = GivenTheTeamRecordedHowItsWorkMoved(team, "ITEM-1");
            var query = GivenWhatTheTrackerIsCurrentlyAskedFor(team);

            WhenTheOperatorReadsOneRawStateAsADifferentState(team);

            ThenWhatTheTrackerIsAskedForIsUnchanged(query, team);
            ThenTheTeamKeptTheHistoryItRecorded(history);
            await WhenTheScheduledRefreshRuns(team);
            ThenTheWholeQueryWasDownloaded();
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @AC-5.1 @contract-shape:bounded-change
        // The same falsification from the other side: AllStates is a UNION, so moving a state between the
        // three columns leaves it identical while changing the state category of every stored record in
        // that state. A fingerprint built on AllStates - which is what AC-5.1 names - cannot see this.
        [Test]
        public async Task Moving_a_state_to_a_different_column_makes_the_next_refresh_download_everything_even_though_the_query_is_unchanged()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheTeamAlsoTracksAReviewState(team);
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);
            var history = GivenTheTeamRecordedHowItsWorkMoved(team, "ITEM-1");
            var query = GivenWhatTheTrackerIsCurrentlyAskedFor(team);

            WhenTheOperatorMovesAStateToADifferentColumn(team);

            ThenWhatTheTrackerIsAskedForIsUnchanged(query, team);
            ThenTheTeamKeptTheHistoryItRecorded(history);
            await WhenTheScheduledRefreshRuns(team);
            ThenTheWholeQueryWasDownloaded();
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @AC-5.1 @contract-shape:bounded-change
        // The third falsification, and the one no save-time comparison can ever catch: the field lives on
        // the CONNECTION. Nothing about the team changed, and every record the team stores now carries a
        // value it did not carry before - for every team on that connection.
        [Test]
        public async Task Adding_a_field_to_the_connection_makes_the_next_refresh_download_everything_for_the_teams_that_use_it()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);
            var query = GivenWhatTheTrackerIsCurrentlyAskedFor(team);

            WhenTheOperatorAddsAFieldToTheConnection(team);
            await WhenTheScheduledRefreshRuns(team);

            ThenWhatTheTrackerIsAskedForIsUnchanged(query, team);
            ThenTheWholeQueryWasDownloaded();
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @AC-5.1 @contract-shape:unbounded-preservation
        // The collections are sets. A fingerprint that is order-sensitive turns opening the Settings
        // screen and pressing Save into a full re-download of the whole query.
        [Test]
        public async Task Re_saving_the_same_states_in_a_different_order_costs_no_download()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);
            var fingerprint = TheStoredFetchFingerprintForTeam(team.Id);

            WhenTheOperatorReSavesTheSameStatesInADifferentOrder(team);
            await WhenTheScheduledRefreshRuns(team);

            ThenNothingWasDownloaded();
            ThenTheTeamsFingerprintIsUnchangedBy(fingerprint, team);
            ThenTheRefreshReportedACheaperUpdateOf(team, scanned: 3, fetched: 0);
        }

        // @driving_port @real-io @AC-5.3 @kpi @contract-shape:unbounded-preservation
        // KPI-4, and the half of the epic that is easiest to lose: "any settings save invalidates" is a
        // safe answer that spends the entire win. Every one of these is a local derivation.
        [Test]
        [Ignore(Pending)]
        [TestCaseSource(nameof(EveryTeamSettingThatChangesNothingAboutWhatIsFetched))]
        public async Task An_edit_that_changes_nothing_about_what_is_fetched_costs_no_download(TeamEdit edit)
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            var fieldId = GivenTheConnectionDefinesAField(team);
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            WhenTheOperatorEditsTheTeamsSettings(team, settings => edit.Apply(settings, fieldId));
            await WhenTheScheduledRefreshRuns(team);

            ThenNothingWasDownloaded();
            ThenTheOperatorSeesACheaperUpdate(scanned: 3, fetched: 0);
            ThenTheRefreshReportedACheaperUpdateOf(team, scanned: 3, fetched: 0);
        }

        // @driving_port @real-io @AC-5.3 @kpi @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        [TestCaseSource(nameof(EveryPortfolioSettingThatChangesNothingAboutWhatIsFetched))]
        public async Task A_portfolio_edit_that_changes_nothing_about_what_is_fetched_costs_no_download(PortfolioEdit edit)
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            var fieldId = GivenTheConnectionDefinesAField(portfolio);
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            WhenTheOperatorEditsThePortfoliosSettings(portfolio, settings => edit.Apply(settings, fieldId));
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenNoFeatureWasDownloaded();
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 3, fetched: 0);
        }

        // @driving_port @real-io @AC-5.5 @D8 @contract-shape:bounded-change
        // The upgrade case. The stored work already carries remote change stamps, so slice 02's "nothing
        // is stamped" branch cannot be what makes this cycle full - the ONLY thing missing is the
        // fingerprint, which is what makes the scenario capable of failing for the reason it names.
        [Test]
        public async Task An_instance_that_upgraded_into_this_release_downloads_everything_on_its_first_refresh()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            GivenTheTeamsWorkWasStoredByAReleaseThatKnewNothingOfFingerprints(team);

            await WhenTheScheduledRefreshRuns(team);

            ThenTheWholeQueryWasDownloaded();
            ThenTheOperatorSeesAFullUpdate(scanned: 3, fetched: 3);
            ThenTheOperatorIsNotToldConfigurationIsWhy();
            ThenTheTeamRemembersWhatItAskedFor(team);
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @AC-5.6 @contract-shape:unbounded-preservation
        // Stored per entity, so an edit to one is not an invalidation of the other. A single shared
        // fingerprint - or one hung off the connection - would make every team edit cost every portfolio
        // on the same connection a full re-download.
        [Test]
        [Ignore(Pending)]
        public async Task A_team_edit_does_not_cost_its_portfolio_a_full_download()
        {
            var (team, portfolio) = GivenATeamAndAPortfolioOnTheSameConnection();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            WhenTheOperatorChangesTheTeamsQuery(team);
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenNoFeatureWasDownloaded();
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 3, fetched: 0);
        }

        // @driving_port @real-io @AC-5.6 @contract-shape:unbounded-preservation
        [Test]
        [Ignore(Pending)]
        public async Task A_portfolio_edit_does_not_cost_its_team_a_full_download()
        {
            var (team, portfolio) = GivenATeamAndAPortfolioOnTheSameConnection();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            WhenTheOperatorChangesThePortfoliosQuery(portfolio);
            await WhenTheScheduledRefreshRuns(team);

            ThenNothingWasDownloaded();
            ThenTheRefreshReportedACheaperUpdateOf(team, scanned: 3, fetched: 0);
        }

        // @error @driving_port @real-io @A2 @contract-shape:bounded-change
        // The one edit the purge is FOR: the same reference id on a different tracker is a different
        // item, and SyncWorkItem updates the stored copy in place - so without a purge the old system's
        // transition history silently becomes the new system's.
        [Test]
        public async Task A_team_that_moves_to_a_different_connection_starts_from_nothing()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);
            var history = GivenTheTeamRecordedHowItsWorkMoved(team, "ITEM-1");

            WhenTheOperatorMovesTheTeamToADifferentConnection(team);

            ThenTheTeamStartedFromNothing(team, history);
            await WhenTheScheduledRefreshRuns(team);
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @A2 @contract-shape:unbounded-preservation
        // And the edit the purge is NOT for. Same tracker, different question - removed = stored - fetched
        // reconciles it on the very next full cycle, which is exactly how the portfolio side has always
        // coped without a purge. Paying for it in transition history is a cost with nothing bought.
        [Test]
        public async Task A_team_whose_query_changed_keeps_the_history_it_already_recorded()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);
            var history = GivenTheTeamRecordedHowItsWorkMoved(team, "ITEM-1");

            WhenTheOperatorChangesTheTeamsQuery(team);

            ThenTheTeamKeptTheHistoryItRecorded(history);
            await WhenTheScheduledRefreshRuns(team);
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
        }

        // @error @driving_port @real-io @A2 @contract-shape:bounded-change
        // The portfolio half of the same rule. Today PortfolioController has no change detection at all,
        // so a portfolio repointed at a different tracker carries every Feature - and every Feature's
        // recorded history - across with it.
        [Test]
        public async Task A_portfolio_that_moves_to_a_different_connection_starts_from_nothing()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);
            var whatItHeld = GivenThePortfolioAlreadyStoresItsFeatures(portfolio);

            WhenTheOperatorMovesThePortfolioToADifferentConnection(portfolio);

            ThenThePortfolioStartedFromNothing(portfolio, whatItHeld);
            await WhenTheScheduledRefreshRuns(portfolio);
            ThenTheRefreshReportedAFullUpdateOf(portfolio, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @A2 @contract-shape:unbounded-preservation
        [Test]
        public async Task A_portfolio_whose_query_changed_keeps_the_features_it_already_stored()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            WhenTheOperatorChangesThePortfoliosQuery(portfolio);

            ThenThePortfolioStillHas(portfolio, "FEAT-1", "FEAT-2", "FEAT-3");
            await WhenTheScheduledRefreshRuns(portfolio);
            ThenThePortfolioRemembersWhatItAskedFor(portfolio);
            ThenTheRefreshReportedAFullUpdateOf(portfolio, scanned: 3, fetched: 3);
        }
    }
}
