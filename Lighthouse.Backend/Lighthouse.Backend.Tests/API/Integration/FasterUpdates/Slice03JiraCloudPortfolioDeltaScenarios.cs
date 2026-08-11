using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5687 — Faster Updates), slice 03: a portfolio refresh downloads
    /// full Feature payloads — and parent-Feature payloads — only for the Features that moved, while still
    /// enumerating the whole query so removals keep meaning what they mean today.
    /// Driving port: the scheduled portfolio refresh. US-03, AC-3.1 … AC-3.6.
    ///
    /// The Feature is not a leaf, and that is what makes this slice its own design rather than slice 02
    /// pointed at a different method. Two failure modes have their own scenario each:
    ///
    /// 1. <b>The portfolio is rebuilt from what was fetched.</b> Today <c>RefreshFeatures</c> hands
    ///    <c>Portfolio.UpdateFeatures</c> the list it downloaded, and the departed-spell sweep the same
    ///    list. Under delta that list holds only the Features that moved — so every unchanged Feature
    ///    loses its portfolio claim, is deleted by the orphaned-Feature cleanup the updater runs, and has
    ///    its open blocked spells closed. Data loss on a green sync.
    /// 2. <b>The parent key list shrinks.</b> The parent path derives its keys from what the portfolio
    ///    stores, which is only safe as long as (1) holds. A cycle in which no child Feature moved must
    ///    still leave every parent present and current.
    ///
    /// AC-3.5 (D9) is the guard against confusing "the Feature record did not move remotely" with "the
    /// Feature's rollup did not change": remaining work, extrapolation, the percentile default size and
    /// the forecast trigger depend on wall clock and on other teams' work, so they recompute every cycle
    /// regardless of mode.
    ///
    /// The Feature-side copy path (the remote change stamp surviving <c>Feature.Update(…)</c>) lives in
    /// <c>Models/Slice03FeatureRemoteChangeStampSurvivesUpdateTest</c> — it is a promise about the copy
    /// itself, and its failure mode is a silent degradation back to full downloads that every other test
    /// tolerates.
    ///
    /// Every scenario ships [Ignore]d. DELIVER un-ignores one at a time; each is one TDD cycle.
    ///
    /// The two AC-3.4 amendment scenarios were added after the slice was green, and they differ from the
    /// rest only in why they exist. Every shared-Feature scenario above pre-refreshes BOTH portfolios over
    /// the same Features, so nothing here ever sent a Feature into a portfolio's query for the FIRST time
    /// while another portfolio already stored it - nor took a Feature out of one portfolio's query while
    /// another portfolio still claimed it. Both are guards that pass on arrival; both have a recorded
    /// probe that reds them and nothing else in this fixture.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5687-faster-updates")]
    [Category("slice-03")]
    public partial class Slice03JiraCloudPortfolioDeltaTest
    {
        // @driving_port @real-io @AC-3.1 @D6 @contract-shape:bounded-change
        // The upgrade case: the portfolio's Features exist, none of them carries a stamp yet.
        [Test]
        public async Task The_first_portfolio_refresh_downloads_every_feature_and_remembers_when_each_one_last_changed()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            GivenThePortfoliosFeaturesWereStoredBeforeThisRelease(portfolio, "FEAT-1", "FEAT-2");

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheWholeFeatureQueryWasDownloaded();
            ThenTheRefreshReportedAFullUpdateOf(portfolio, scanned: 3, fetched: 3);
            ThenEveryFeatureInThePortfolioRemembersWhenItLastChanged(portfolio);
        }

        // @walking_skeleton @driving_port @real-io @AC-3.1 @AC-3.6 @contract-shape:bounded-change
        // The half of the cycle the epic was still paying for in full.
        [Test]
        public async Task A_later_portfolio_refresh_downloads_only_the_features_that_moved()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            GivenOneFeatureMovedOnTheTracker("FEAT-2");
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheWholeFeatureQueryWasScannedForIdentitiesOnly();
            ThenOnlyTheFeaturesThatMovedWereDownloaded("FEAT-2");
            ThenTheOperatorSeesACheaperUpdate(scanned: 3, fetched: 1);
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 3, fetched: 1);
        }

        // @driving_port @real-io @AC-3.1 @AC-3.2 @D2 @contract-shape:unbounded-preservation
        // Failure mode 1, and the reason this slice is not slice 02 pointed at another method: the
        // portfolio is rebuilt from the fetched list, so under delta the Features that did NOT move are
        // the ones at risk. Losing a Feature here deletes it outright.
        [Test]
        public async Task A_feature_that_did_not_move_is_still_part_of_the_portfolio_after_a_cheaper_refresh()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            GivenOneFeatureMovedOnTheTracker("FEAT-2");
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenThePortfolioStillHas(portfolio, "FEAT-1", "FEAT-2", "FEAT-3");
            ThenTheUntouchedFeatureIsStillStored("FEAT-1");
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 3, fetched: 1);
        }

        // @driving_port @real-io @AC-3.3 @contract-shape:unbounded-preservation
        // The second half of failure mode 1: what a dropped Feature costs beyond the row itself.
        [Test]
        public async Task A_feature_that_did_not_move_keeps_its_history_and_stays_blocked_if_it_was()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);
            var blockedFeature = GivenOneFeatureHasBeenBlockedForAWhile(portfolio, "FEAT-1");

            var before = GivenHowTheUntouchedFeaturesHistoryLooksNow(blockedFeature);
            GivenOneFeatureMovedOnTheTracker("FEAT-2");
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheUntouchedFeaturesHistoryIsIdenticalTo(before, blockedFeature);
            ThenNothingWasDeclaredUnblocked();
            ThenTheFeatureIsStillBlockedInThePortfolio(portfolio, blockedFeature);
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 3, fetched: 1);
        }

        // @error @driving_port @real-io @AC-3.2 @D2 @contract-shape:bounded-change
        // Removal is a set difference against the whole query, exactly as it is today.
        [Test]
        public async Task A_feature_that_left_the_query_is_gone_from_the_portfolio_on_the_very_next_cycle()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            GivenOneFeatureLeftTheQuery("FEAT-3");
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenThePortfolioNoLongerHas(portfolio, "FEAT-3");
            ThenThePortfolioStillHas(portfolio, "FEAT-1", "FEAT-2");
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 2, fetched: 0);
        }

        // @driving_port @real-io @AC-3.4 @contract-shape:bounded-change
        // A Feature two portfolios both claim is one stored record with one stamp, so the second
        // portfolio's own cycle finds it already current — and must still show it.
        [Test]
        public async Task A_feature_shared_by_two_portfolios_is_downloaded_once_and_shown_in_both()
        {
            var (first, second) = GivenTwoPortfoliosThatTrackTheSameFeatures();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(first);
            await GivenThePortfolioHasAlreadyBeenRefreshed(second);

            GivenOneFeatureMovedOnTheTracker("FEAT-2");
            await WhenTheScheduledRefreshRuns(first);
            await WhenTheScheduledRefreshRuns(second);

            ThenTheSecondPortfolioDidNotDownloadTheFeatureAgain();
            ThenThePortfolioStillHas(second, "FEAT-1", "FEAT-2", "FEAT-3");
            ThenBothPortfoliosShowTheFeatureAsFinished(first, second, "FEAT-2");
            ThenTheRefreshReportedACheaperUpdateOf(second, scanned: 3, fetched: 0);
        }

        // @driving_port @real-io @AC-3.4 @contract-shape:bounded-change
        // The other half of AC-3.4, and the one no scenario reached: every shared-Feature scenario above
        // pre-refreshes BOTH portfolios over the same Features, so a Feature never arrives in a
        // portfolio's query for the first time while another portfolio already stores it stamped. What
        // this portfolio has stored is the only thing its own cycle compares against, so a Feature it has
        // never held has always moved as far as it is concerned - and the claim it takes out is what
        // keeps the row alive when the other portfolio later lets go.
        [Test]
        public async Task A_feature_another_portfolio_already_stores_joins_this_portfolio_the_first_time_its_query_returns_it()
        {
            var (first, second) = GivenTwoPortfoliosThatTrackTheSameFeatures();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsTwoFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(second);

            GivenAThirdFeatureStartsBeingReturnedByTheQuery();
            await GivenThePortfolioHasAlreadyBeenRefreshed(first);
            await WhenTheScheduledRefreshRuns(second);

            ThenThePortfolioStillHas(second, "FEAT-1", "FEAT-2", "FEAT-3");
            ThenThePortfolioStillHas(first, "FEAT-1", "FEAT-2", "FEAT-3");
            ThenTheRefreshReportedACheaperUpdateOf(second, scanned: 3, fetched: 1);
        }

        // @error @driving_port @real-io @AC-3.2 @AC-3.4 @contract-shape:bounded-change
        // The mirror of the scenario above: letting go is per portfolio, deletion is not. A Feature that
        // leaves one portfolio's query loses that portfolio's claim and nothing else, because the
        // orphaned-Feature cleanup deletes only what no portfolio claims at all.
        [Test]
        public async Task A_feature_that_left_one_portfolios_query_survives_because_the_other_portfolio_still_claims_it()
        {
            var (first, second) = GivenTwoPortfoliosThatTrackTheSameFeatures();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(first);
            await GivenThePortfolioHasAlreadyBeenRefreshed(second);

            GivenOneFeatureLeftTheQuery("FEAT-3");
            await WhenTheScheduledRefreshRuns(second);

            ThenThePortfolioNoLongerHas(second, "FEAT-3");
            ThenTheDepartedFeatureIsStillStored("FEAT-3");
            ThenThePortfolioStillHas(first, "FEAT-1", "FEAT-2", "FEAT-3");
            ThenThePortfolioStillHas(second, "FEAT-1", "FEAT-2");
            ThenTheRefreshReportedACheaperUpdateOf(second, scanned: 2, fetched: 0);
        }

        // @driving_port @real-io @AC-3.1 @contract-shape:unbounded-preservation
        // Failure mode 2: the parent key list is derived from what the portfolio STORES. Derived from
        // what this cycle fetched, it shrinks to nothing on a quiet cycle and the parents drop out.
        [Test]
        public async Task The_parent_features_survive_a_cycle_in_which_no_child_feature_moved()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsTwoFeaturesUnderOneParent();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheParentFeatureIsStillStoredAndCurrent("PARENT-1");
            ThenTheParentFeaturesWereScannedFor("PARENT-1");
            ThenNoParentFeatureWasDownloaded();
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 2, fetched: 0);
        }

        // @driving_port @real-io @AC-3.5 @D9 @contract-shape:bounded-change
        // "The Feature record did not move remotely" is not "the Feature's rollup did not change". The
        // work under a Feature belongs to a team whose own refresh has its own schedule.
        [Test]
        public async Task A_cheaper_portfolio_refresh_still_counts_the_work_that_is_left_and_still_asks_for_a_new_forecast()
        {
            var portfolio = GivenAPortfolioDeliveredByOneTeam();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsOneFeatureWithNoWorkOnItYet();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);
            ThenTheFeatureWasSizedByTheDefaultBecauseItHasNoWork();

            GivenTheTeamHasSinceBrokenTheFeatureDownIntoThreeItems(portfolio);
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheFeatureReportsTheWorkThatIsLeft(remainingItems: 2);
            ThenTheFeatureIsNoLongerSizedByTheDefault();
            ThenTheForecastsWereAskedForAgain(portfolio);
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 1, fetched: 0);
        }

        // @error @driving_port @real-io @AC-3.1 @D8 @contract-shape:bounded-change
        // No partial results, ever — the portfolio half of D8.
        [Test]
        public async Task A_portfolio_refresh_whose_scan_fails_downloads_every_feature_rather_than_half()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            GivenTheFeatureScanFails();
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheWholeFeatureQueryWasDownloaded();
            ThenTheRefreshReportedAFullUpdateOf(portfolio, scanned: 3, fetched: 3);
            ThenThePortfolioStillHas(portfolio, "FEAT-1", "FEAT-2", "FEAT-3");
            ThenTheOperatorIsToldTheScanFailed();
        }

        // @driving_port @real-io @A1 @contract-shape:unbounded-preservation
        // Slice 03 adds no second gate: the portfolio half is covered by the opt-in the team half already
        // has. The defining claim is an absence.
        [Test]
        public async Task A_portfolio_refresh_never_scans_unless_an_operator_asked_for_it()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenNobodyAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheTrackersFeaturesWereNeverScanned();
            ThenTheWholeFeatureQueryWasDownloaded();
            ThenTheRefreshReportedAFullUpdateOf(portfolio, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @A1 @contract-shape:unbounded-preservation
        // The parent half of the gate above. It is a separate scenario because it is a separate decision
        // in the code: the parent path reads the opt-in for itself, and the Feature half's scenario holds
        // no parents at all, so nothing there says whether the parent query is left alone too.
        [Test]
        public async Task A_portfolio_refresh_nobody_asked_for_never_scans_the_parent_features_either()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenNobodyAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsTwoFeaturesUnderOneParent();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheParentFeaturesWereNeverScanned();
            ThenTheParentFeaturesWereDownloaded(TheParentFeature);
            ThenTheParentFeatureIsStillStoredAndCurrent(TheParentFeature);
        }

        // @driving_port @real-io @AC-3.1 @contract-shape:bounded-change
        // The other direction of the quiet-cycle scenario above: a parent whose OWN record moved has to be
        // refetched even though not one child did. Nothing on the Feature side of the cycle can report
        // this, which is the whole reason the parent half sweeps at all.
        [Test]
        public async Task A_parent_feature_that_moved_on_the_tracker_is_refetched_by_the_cheaper_cycle()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsTwoFeaturesUnderOneParent();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            GivenTheParentFeatureWasRenamedOnTheTracker();
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheParentFeaturesWereScannedFor(TheParentFeature);
            ThenTheParentFeaturesWereDownloaded(TheParentFeature);
            ThenTheParentFeatureShowsWhatTheTrackerNowSays(TheParentFeature, TheParentsNewName);
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 2, fetched: 0);
        }

        // @error @driving_port @real-io @AC-3.1 @contract-shape:bounded-change
        // The Feature half's rule inverted. A Feature the sweep does not answer for has left the query and
        // is dropped; a PARENT the sweep does not answer for is downloaded, because parents are excluded
        // from the orphaned-Feature cleanup and silence must never be read as departure.
        [Test]
        public async Task A_parent_feature_the_sweep_did_not_answer_for_is_asked_for_rather_than_assumed_gone()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsTwoFeaturesUnderOneParent();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            GivenTheParentFeatureQueryStoppedAnsweringForIt();
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheParentFeaturesWereDownloaded(TheParentFeature);
            ThenTheParentFeatureIsStillStored(TheParentFeature);
        }

        // @error @driving_port @real-io @AC-3.1 @D8 @contract-shape:bounded-change
        // The parent half of D8, and the half no scenario reached: the parent path scans and falls back on
        // its own, so the Feature half's fallback says nothing about it.
        [Test]
        public async Task A_portfolio_refresh_whose_parent_scan_fails_downloads_every_parent_rather_than_half()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsTwoFeaturesUnderOneParent();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            GivenTheParentFeatureScanFails();
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheParentFeaturesWereDownloaded(TheParentFeature);
            ThenTheParentFeatureIsStillStoredAndCurrent(TheParentFeature);
            ThenTheOperatorIsToldTheParentScanFailed();
        }

        // @driving_port @real-io @AC-3.1 @A1 @contract-shape:unbounded-preservation
        // The opt-in is per instance, the capability is per connector, and the parent half honours both.
        // An operator who volunteered a Jira Data Center portfolio must still get every parent.
        [Test]
        public async Task A_portfolio_whose_tracker_refuses_to_be_scanned_still_gets_every_parent_feature()
        {
            var portfolio = GivenAPortfolioWhoseTrackerRefusesToBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsTwoFeaturesUnderOneParent();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheParentFeaturesWereNeverScanned();
            ThenTheParentFeaturesWereDownloaded(TheParentFeature);
            ThenTheParentFeatureIsStillStoredAndCurrent(TheParentFeature);
            ThenTheRefreshReportedAFullUpdateOf(portfolio, scanned: 2, fetched: 2);
        }

        // @error @driving_port @real-io @AC-3.2 @AC-3.3 @contract-shape:bounded-change
        // The mirror of "a quiet Feature keeps its open spell": a Feature that genuinely LEFT this
        // portfolio's query has to have its spell closed, and only the sweep over what departed can do it.
        // Two portfolios, so the row survives losing this one's claim and the spell survives with it.
        [Test]
        public async Task A_feature_that_was_blocked_when_it_left_the_query_stops_accruing_blocked_time()
        {
            var (first, second) = GivenTwoPortfoliosThatTrackTheSameFeatures();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(first);
            await GivenThePortfolioHasAlreadyBeenRefreshed(second);
            var departingFeature = GivenOneFeatureHasBeenBlockedForAWhile(second, TheDepartingFeature);

            GivenOneFeatureLeftTheQuery(TheDepartingFeature);
            await WhenTheScheduledRefreshRuns(second);

            ThenThePortfolioNoLongerHas(second, TheDepartingFeature);
            ThenTheDepartedFeatureIsStillStored(TheDepartingFeature);
            ThenTheDepartedFeaturesBlockedSpellWasClosed(second, departingFeature);
        }
    }
}
