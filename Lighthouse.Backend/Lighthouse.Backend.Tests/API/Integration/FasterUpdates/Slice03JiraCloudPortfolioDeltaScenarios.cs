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
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
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
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
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
    }
}
