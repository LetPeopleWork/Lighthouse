using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BehaviourSettings
{
    /// <summary>
    /// Filling in past days on the over-time charts stops being a setting. Behaviour Settings no longer
    /// offers it, on a new instance and on one upgraded from a release that did, whichever way that release
    /// left it. The upgrade also clears away the record the earlier release kept about switching it on once,
    /// and touches nothing else. A restored backup and a cleared database come back the same way. Driving
    /// ports: start-up (every seeder running against the database), the restore and clear an administrator
    /// starts from Settings, and the behaviour-settings read and write.
    ///
    /// That the charts then fill in, on an instance that had the fill switched off, is shown at the chart
    /// itself, by the scenarios in the PercentilesOverTime folder that sit beside the fill's own.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6084-over-time-history-fill-always-on")]
    public partial class Story6084HistoryFillAlwaysOnTest
    {
        // @walking_skeleton @driving_port @real-io @contract-shape:pure-function
        // Nobody has anything to decide about the fill, so there is nothing to show them.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task A_new_instance_offers_no_history_fill_setting()
        {
            GivenTheCallerAdministersTheInstance();

            var offered = await WhenTheAdminOpensBehaviourSettings();

            ThenBehaviourSettingsOffersNoHistoryFill(offered);
        }

        // @driving_port @real-io @kpi @contract-shape:bounded-change
        // The setting goes whichever way the last release left it, an admin's deliberate off included, and so
        // does the record of the one-time switch-on. Every other setting and stored value stays as it was.
        [Test]
        [Ignore(PendingDeliver)]
        [TestCase(HowTheLastReleaseLeftTheFill.SwitchedOnByDefault)]
        [TestCase(HowTheLastReleaseLeftTheFill.SwitchedOffByTheAdmin)]
        [TestCase(HowTheLastReleaseLeftTheFill.SwitchedOffBeforeItWasSwitchedOnByDefault)]
        public async Task An_upgraded_instance_no_longer_offers_the_history_fill_and_keeps_everything_else_as_it_was(HowTheLastReleaseLeftTheFill lastRelease)
        {
            GivenTheCallerAdministersTheInstance();
            GivenTheInstanceAsTheLastReleaseLeftIt(lastRelease);
            var before = await GivenWhatTheInstanceHeldBeforeTheUpgrade();

            WhenTheInstanceIsUpgraded();
            var after = await WhenTheAdminOpensBehaviourSettings();

            using (Assert.EnterMultipleScope())
            {
                ThenOnlyTheHistoryFillLeftBehaviourSettings(before, after);
                ThenNothingIsLeftOfTheHistoryFillSetting();
                ThenOnlyTheRecordOfTheSwitchOnLeftTheStoredSettings(before);
            }
        }

        // @driving_port @real-io @error @kpi @contract-shape:bounded-change
        // The setting can already be gone, removed by hand or never stored, while the record of the switch-on
        // stays behind. The record goes on its own.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task An_instance_whose_fill_setting_was_already_removed_keeps_nothing_of_it_after_the_upgrade()
        {
            GivenTheCallerAdministersTheInstance();
            GivenTheInstanceAsTheLastReleaseLeftIt(HowTheLastReleaseLeftTheFill.SettingRemovedButTheRecordKept);

            WhenTheInstanceIsUpgraded();
            var offered = await WhenTheAdminOpensBehaviourSettings();

            using (Assert.EnterMultipleScope())
            {
                ThenBehaviourSettingsOffersNoHistoryFill(offered);
                ThenNothingIsLeftOfTheHistoryFillSetting();
            }
        }

        // @driving_port @real-io @kpi @contract-shape:unbounded-preservation
        // Every later start-up runs the seeders again. None of them brings the setting or the record back, and
        // none of them changes anything else either: Behaviour Settings and every stored value read as they did
        // straight after the upgrade.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task Restarting_again_and_again_never_brings_the_history_fill_setting_back()
        {
            GivenTheCallerAdministersTheInstance();
            var afterTheUpgrade = await GivenAnInstanceUpgradedFromAReleaseWhoseAdminHadSwitchedTheFillOff();

            WhenTheInstanceRestarts(times: 3);
            var afterTheRestarts = await WhenTheAdminOpensBehaviourSettings();

            using (Assert.EnterMultipleScope())
            {
                ThenBehaviourSettingsReadsExactlyAsItDid(afterTheUpgrade.Offered, afterTheRestarts);
                ThenEveryStoredValueReadsExactlyAsItDid(afterTheUpgrade);
                ThenNothingIsLeftOfTheHistoryFillSetting();
            }
        }

        // @driving_port @real-io @error @contract-shape:bounded-change
        // A script written while the fill was a setting, the way the end-to-end tests switch it, is told the
        // setting does not exist. It does not quietly put a setting back that nothing reads.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task A_script_that_still_switches_the_history_fill_is_told_there_is_no_such_setting()
        {
            GivenTheCallerAdministersTheInstance();

            var read = await WhenAScriptReadsTheHistoryFillSettingByName();
            var write = await WhenAScriptSwitchesTheHistoryFillOff();

            using (Assert.EnterMultipleScope())
            {
                ThenTheScriptIsToldThereIsNoSuchSetting(read, "reading");
                ThenTheScriptIsToldThereIsNoSuchSetting(write, "switching");
                ThenNothingIsLeftOfTheHistoryFillSetting();
            }
        }

        // @driving_port @real-io @error @kpi @contract-shape:bounded-change
        // A restore brings back what the backup held, then the seeders run as at any start-up. Whatever the
        // backup held about the fill, the restored instance ends like every other one: nothing to switch and
        // nothing recorded, so its charts fill like everyone else's. It stays that way at the next restart. The
        // most common backup holds the fill switched on by default, with the record of that switch-on beside it.
        [Test]
        [Ignore(PendingDeliver)]
        [TestCase(HowTheLastReleaseLeftTheFill.SwitchedOnByDefault)]
        [TestCase(HowTheLastReleaseLeftTheFill.SwitchedOffByTheAdmin)]
        [TestCase(HowTheLastReleaseLeftTheFill.SwitchedOffBeforeItWasSwitchedOnByDefault)]
        [TestCase(HowTheLastReleaseLeftTheFill.NotYetOffered)]
        public async Task Restoring_a_backup_leaves_no_history_fill_setting_whatever_the_backup_held(HowTheLastReleaseLeftTheFill whatTheBackupHeld)
        {
            GivenTheCallerAdministersTheInstance();
            GivenABackupThatHeldTheFill(whatTheBackupHeld);

            await WhenTheAdminRestoresTheBackup();
            var afterTheRestore = await WhenTheAdminOpensBehaviourSettings();
            WhenTheInstanceRestarts(times: 1);
            var afterTheRestart = await WhenTheAdminOpensBehaviourSettings();

            using (Assert.EnterMultipleScope())
            {
                ThenBehaviourSettingsOffersNoHistoryFill(afterTheRestore);
                ThenBehaviourSettingsOffersNoHistoryFill(afterTheRestart);
                ThenNothingIsLeftOfTheHistoryFillSetting();
            }
        }

        // @driving_port @real-io @contract-shape:bounded-change
        // Clearing the database leaves a new instance behind, and a new instance has no fill setting.
        [Test]
        [Ignore(PendingDeliver)]
        public async Task Clearing_the_database_leaves_no_history_fill_setting_like_a_new_instance()
        {
            GivenTheCallerAdministersTheInstance();
            GivenTheInstanceAsTheLastReleaseLeftIt(HowTheLastReleaseLeftTheFill.SwitchedOffByTheAdmin);

            await WhenTheAdminClearsTheDatabase();
            var offered = await WhenTheAdminOpensBehaviourSettings();

            using (Assert.EnterMultipleScope())
            {
                ThenBehaviourSettingsOffersNoHistoryFill(offered);
                ThenNothingIsLeftOfTheHistoryFillSetting();
            }
        }
    }
}
