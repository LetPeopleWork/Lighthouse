using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5687 — Faster Updates), slice 02: the second and later
    /// refreshes of a team download full payloads only for the issues that moved, while still
    /// enumerating the whole query so removals are caught.
    /// Driving port: the scheduled refresh. US-02, AC-2.1 … AC-2.6, AC-2.9 … AC-2.12.
    ///
    /// AC-2.7 (the remote change stamp survives <c>Update(…)</c>) lives in
    /// <c>Models/Slice02RemoteChangeStampSurvivesUpdateTest</c> — losing it degrades every later
    /// refresh to a full download with every other test still green, so it is asserted directly.
    /// AC-2.8 (≤10% of the remote requests, KPI-2) is not automated: it is a dogfood measurement against
    /// a real Jira Cloud project with ≥1000 issues, read off slice 01's summary line. A synthetic issue
    /// count would prove the plumbing and not the premise.
    ///
    /// Every scenario ships [Ignore]d. DELIVER un-ignores one at a time; each is one TDD cycle.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5687-faster-updates")]
    [Category("slice-02")]
    public partial class Slice02JiraCloudTeamDeltaTest
    {
        // @driving_port @real-io @AC-2.1 @contract-shape:bounded-change
        // The upgrade case: items exist, none of them carries a stamp yet.
        [Test]
        [Ignore("BLOCKED on a harness defect, not on production code: GivenTheTeamsIssuesWereStoredBeforeThisRelease "
            + "seeds against TheTeamUnderRefresh.Id, which is only assigned by WhenTheScheduledRefreshRuns - so the "
            + "Given runs with team id 0 and throws. Verified: with the id passed in, this scenario passes. "
            + "Routed to nw-acceptance-designer (step 01-02).")]
        public async Task The_first_refresh_after_an_upgrade_downloads_everything_and_remembers_when_each_issue_last_changed()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            GivenTheTeamsIssuesWereStoredBeforeThisRelease("ITEM-1", "ITEM-2");

            await WhenTheScheduledRefreshRuns(team);

            ThenTheWholeQueryWasDownloaded();
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
            ThenEveryStoredIssueRemembersWhenItLastChanged(team);
        }

        // @walking_skeleton @driving_port @real-io @AC-2.2 @contract-shape:bounded-change
        // The thing the epic is buying.
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
        public async Task A_later_refresh_downloads_only_the_issues_that_moved()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            GivenOneIssueMovedOnTheTracker("ITEM-2");
            await WhenTheScheduledRefreshRuns(team);

            ThenTheWholeQueryWasScannedForIdentitiesOnly();
            ThenOnlyTheIssuesThatMovedWereDownloaded("ITEM-2");
            ThenTheOperatorSeesACheaperUpdate(scanned: 3, fetched: 1);
            ThenTheRefreshReportedACheaperUpdateOf(team, scanned: 3, fetched: 1);
        }

        // @error @driving_port @real-io @AC-2.3 @D2 @contract-shape:bounded-change
        // The rule whose failure deletes live work items: removed is an exact set difference.
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
        public async Task An_issue_that_left_the_query_is_gone_from_the_team_on_the_very_next_cycle()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            GivenOneIssueLeftTheQuery("ITEM-3");
            await WhenTheScheduledRefreshRuns(team);

            ThenTheTeamNoLongerHas("ITEM-3", team);
            ThenTheTeamStillHas(team, "ITEM-1", "ITEM-2");
            ThenTheRefreshReportedACheaperUpdateOf(team, scanned: 2, fetched: 0);
        }

        // @driving_port @real-io @AC-2.4 @contract-shape:unbounded-preservation
        // Hypothesis 2: "updated is not trustworthy". Whole-surface, not a spot check of two fields.
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
        public async Task An_issue_that_did_not_move_is_left_exactly_as_it_was()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            var before = GivenHowTheUntouchedIssueLooksNow(team, "ITEM-1");
            GivenOneIssueMovedOnTheTracker("ITEM-2");
            await WhenTheScheduledRefreshRuns(team);

            ThenTheRefreshReportedACheaperUpdateOf(team, scanned: 3, fetched: 1);
            ThenTheUntouchedIssueIsIdenticalTo(before, team, "ITEM-1");
        }

        // @driving_port @real-io @AC-2.5 @D10 @contract-shape:bounded-change
        // The regression that leaves every other test green.
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
        public async Task An_issue_that_stopped_moving_still_goes_stale()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDays();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsAnIssueNobodyHasTouchedInWeeks();
            GivenThatIssueWasAlreadyStoredWithTheDayItEnteredItsState(team);

            await WhenTheScheduledRefreshRuns(team);

            ThenTheIssueWasReportedAsStale(team);
            ThenTheIssueWasNeverDownloaded();
        }

        // @error @driving_port @real-io @AC-2.6 @D8 @contract-shape:bounded-change
        // No partial results, ever.
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
        public async Task A_refresh_whose_scan_fails_downloads_everything_rather_than_half()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            GivenTheScanFails();
            await WhenTheScheduledRefreshRuns(team);

            ThenTheWholeQueryWasDownloaded();
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
            ThenTheTeamStillHas(team, "ITEM-1", "ITEM-2", "ITEM-3");
            ThenTheOperatorIsToldTheScanFailed();
        }

        // @driving_port @real-io @AC-2.9 @D9 @contract-shape:bounded-change
        // Everything downstream of the fetch keeps happening.
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
        public async Task A_cheaper_refresh_still_rolls_up_remaining_work_and_still_asks_for_a_new_forecast()
        {
            var team = GivenATeamDeliveringOneFeature();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssuesOnThatFeature();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            GivenOneIssueLeftTheQuery("ITEM-3");
            await WhenTheScheduledRefreshRuns(team);

            ThenTheRefreshReportedACheaperUpdateOf(team, scanned: 2, fetched: 0);
            ThenTheFeatureReportsTheWorkThatIsLeft(remainingItems: 2);
            ThenTheTeamsDataWasAnnouncedAsRefreshed(team);
        }

        // @driving_port @real-io @AC-2.10 @A1 @contract-shape:unbounded-preservation
        // Off is the default, and off means nothing is scanned - the defining claim is an absence.
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
        public async Task A_refresh_never_scans_unless_an_operator_asked_for_it()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenNobodyAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            await WhenTheScheduledRefreshRuns(team);

            ThenTheTrackerWasNeverScanned();
            ThenTheWholeQueryWasDownloaded();
            ThenTheRefreshReportedAFullUpdateOf(team, scanned: 3, fetched: 3);
        }

        // @driving_port @real-io @AC-2.11 @A1 @contract-shape:bounded-change
        // A soft launch is only usable if the toggle bites today.
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
        public async Task Asking_for_the_cheaper_refresh_takes_effect_on_the_very_next_cycle()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenNobodyAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            GivenTheOperatorAskedForTheCheaperRefresh();
            await WhenTheScheduledRefreshRuns(team);

            ThenTheWholeQueryWasScannedForIdentitiesOnly();
            ThenTheRefreshReportedACheaperUpdateOf(team, scanned: 3, fetched: 0);
        }

        // @AC-2.12 @A1 @contract-shape:unbounded-preservation
        // A fresh install, and an instance upgrading into this release, both stay off.
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this scenario when it implements it.")]
        public void An_instance_that_never_asked_for_the_cheaper_refresh_does_not_get_it()
        {
            ThenTheCheaperRefreshIsOfferedButSwitchedOff();

            WhenTheInstanceIsUpgradedAgain();

            ThenTheCheaperRefreshIsOfferedButSwitchedOff();
        }
    }
}
