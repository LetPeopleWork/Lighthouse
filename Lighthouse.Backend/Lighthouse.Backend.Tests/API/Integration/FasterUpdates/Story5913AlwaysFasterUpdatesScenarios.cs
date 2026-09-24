using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// Faster Updates stops being a switch. Every refresh a connector can make cheap is cheap, on every
    /// instance, including one whose operator had switched it off, and the Settings → System list no
    /// longer offers the row. Driving ports: the settings list an administrator reads, the upgrade (the
    /// seeders re-running against an existing database) and the scheduled refresh.
    ///
    /// Everything that still falls back to a full download is already asserted by the Epic #5687 slices
    /// and is not repeated here: a connector that cannot be scanned, a failed scan, a changed fetch shape,
    /// a first refresh and a stored record without a change stamp.
    ///
    /// Every scenario ships [Ignore]d. DELIVER un-ignores one at a time; each is one TDD cycle.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-5913-always-faster-updates")]
    public partial class Story5913AlwaysFasterUpdatesTest
    {
        // @driving_port @real-io @AC-1.1 @contract-shape:unbounded-preservation
        // A new instance never meets the switch at all.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task A_fresh_install_offers_no_faster_updates_switch()
        {
            var offered = await WhenTheAdminOpensTheSystemSettings();

            ThenTheSettingsListOffersNoFasterUpdatesSwitch(offered);
        }

        // @driving_port @real-io @AC-1.1 @contract-shape:bounded-change
        // The upgrade takes the row away whichever way it was set, and takes nothing else with it.
        [Test]
        [Ignore(PendingDeliver)]
        [TestCase(HowTheOperatorLeftIt.On)]
        [TestCase(HowTheOperatorLeftIt.Off)]
        public async Task An_upgraded_instance_offers_no_faster_updates_switch_whichever_way_it_was_set(HowTheOperatorLeftIt position)
        {
            GivenTheInstanceHadFasterUpdates(position);
            var before = await GivenWhatTheAdminSawBeforeTheUpgrade();

            WhenTheInstanceIsUpgraded();
            var after = await WhenTheAdminOpensTheSystemSettings();

            ThenOnlyTheFasterUpdatesSwitchIsGone(before, after);
        }

        // @driving_port @real-io @AC-1.5 @kpi @contract-shape:unbounded-preservation
        // Every later start-up re-runs the seeders. None of them may bring the row back.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task Upgrading_again_never_brings_the_faster_updates_switch_back()
        {
            GivenTheInstanceHadFasterUpdates(HowTheOperatorLeftIt.Off);
            GivenTheInstanceHasBeenUpgraded();

            WhenTheInstanceIsUpgraded();
            WhenTheInstanceIsUpgraded();
            var offered = await WhenTheAdminOpensTheSystemSettings();

            ThenTheSettingsListOffersNoFasterUpdatesSwitch(offered);
            ThenNoFasterUpdatesSettingIsStored();
        }

        // @driving_port @real-io @AC-1.2 @kpi @contract-shape:bounded-change
        // The operator who had it off is the one this story is for. The instance also holds no premium
        // licence, because the cheaper refresh was never something a licence paid for.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task A_team_on_an_instance_that_had_faster_updates_off_downloads_only_the_issues_that_moved_after_the_upgrade()
        {
            var team = GivenAJiraCloudTeamWhoseTrackerCanBeScanned();
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheInstanceHadFasterUpdates(HowTheOperatorLeftIt.Off);
            GivenTheTrackerHoldsThreeIssues();
            await GivenTheTeamWasRefreshedBeforeTheUpgrade(team);
            GivenTheInstanceHasBeenUpgraded();

            GivenOneIssueMovedOnTheTracker("ITEM-2");
            await WhenTheScheduledRefreshRuns(team);

            ThenTheWholeQueryWasScannedForIdentitiesOnly();
            ThenOnlyTheIssuesThatMovedWereDownloaded("ITEM-2");
            ThenTheOperatorSeesACheaperUpdate(scanned: 3, fetched: 1);
            ThenTheRefreshReportedACheaperUpdateOf(team, scanned: 3, fetched: 1);
        }

        // @driving_port @real-io @AC-1.3 @contract-shape:bounded-change
        // The portfolio half read the same switch, so it has to stop needing it too.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task A_portfolio_on_an_instance_that_had_faster_updates_off_downloads_only_the_features_that_moved_after_the_upgrade()
        {
            var portfolio = GivenAJiraCloudPortfolioWhoseTrackerCanBeScanned();
            GivenTheInstanceHadFasterUpdates(HowTheOperatorLeftIt.Off);
            GivenTheTrackerHoldsThreeFeatures();
            await GivenThePortfolioWasRefreshedBeforeTheUpgrade(portfolio);
            GivenTheInstanceHasBeenUpgraded();

            GivenOneFeatureMovedOnTheTracker("FEAT-2");
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheWholeFeatureQueryWasScannedForIdentitiesOnly();
            ThenOnlyTheFeaturesThatMovedWereDownloaded("FEAT-2");
            ThenTheRefreshReportedACheaperUpdateOf(portfolio, scanned: 3, fetched: 1);
        }

        // @driving_port @real-io @AC-1.3 @contract-shape:bounded-change
        // The parent Features decide their own mode, separately from the Features under them, so a
        // portfolio whose Features went cheap says nothing about whether its parents did.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task The_parent_features_on_an_instance_that_had_faster_updates_off_are_scanned_rather_than_downloaded_after_the_upgrade()
        {
            var portfolio = GivenAJiraCloudPortfolioWhoseTrackerCanBeScanned();
            GivenTheInstanceHadFasterUpdates(HowTheOperatorLeftIt.Off);
            GivenTheTrackerHoldsTwoFeaturesUnderOneParent();
            await GivenThePortfolioWasRefreshedBeforeTheUpgrade(portfolio);
            GivenTheInstanceHasBeenUpgraded();

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheParentFeaturesWereScannedFor(TheParentFeature);
            ThenNoParentFeatureWasDownloaded();
            ThenTheParentFeatureIsStillStoredAndCurrent(TheParentFeature);
        }
    }
}
