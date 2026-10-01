using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BehaviourSettings
{
    /// <summary>
    /// Filling in past days on the over-time charts is on unless a System Admin switches it off. A new
    /// instance starts with it on. An instance upgraded from an earlier release has it switched on once,
    /// whichever way it was left, because nothing recorded whether an earlier off was chosen or merely
    /// seeded. From then on an admin's off is kept across every restart. A restore never makes that
    /// switch-on: it brings back what the backup held. Clearing the database leaves a new instance. Driving
    /// ports: start-up (every seeder running against the database), the restore and clear an administrator
    /// starts from Settings, and the behaviour-settings read and toggle.
    ///
    /// That only a System Admin can switch it is already held by the fill's own scenarios and is not
    /// repeated here.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6083-over-time-history-fill-on-by-default")]
    public partial class Story6083HistoryFillOnByDefaultTest
    {
        // @walking_skeleton @driving_port @real-io @contract-shape:pure-function
        // Nobody has to know the switch exists for the charts to fill in.
        [Test]
        public async Task A_new_instance_has_the_history_fill_switched_on()
        {
            GivenTheCallerAdministersTheInstance();

            var fill = await WhenTheAdminReadsTheHistoryFillSetting();

            ThenTheHistoryFillReadsOn(fill);
        }

        // @driving_port @real-io @kpi @contract-shape:bounded-change
        // An earlier off cannot be told apart from an off nobody chose, so every earlier state comes out on,
        // including an instance old enough never to have offered the setting at all.
        [Test]
        [TestCase(HowAnEarlierReleaseLeftTheFill.SwitchedOff)]
        [TestCase(HowAnEarlierReleaseLeftTheFill.SwitchedOn)]
        [TestCase(HowAnEarlierReleaseLeftTheFill.NotYetOffered)]
        public async Task An_upgraded_instance_has_the_history_fill_switched_on_whichever_way_the_earlier_release_left_it(HowAnEarlierReleaseLeftTheFill earlierState)
        {
            GivenTheCallerAdministersTheInstance();
            GivenTheInstanceAsAnEarlierReleaseLeftIt(earlierState);

            WhenTheInstanceIsUpgraded();
            var fill = await WhenTheAdminReadsTheHistoryFillSetting();

            using (Assert.EnterMultipleScope())
            {
                ThenTheHistoryFillReadsOn(fill);
                ThenTheInstanceRemembersItSwitchedTheFillOn();
            }
        }

        // @driving_port @real-io @contract-shape:bounded-change
        // The upgrade hands the admin a fill that is on. It does not touch how the setting describes itself,
        // and it does not touch any other setting.
        [Test]
        public async Task The_upgrade_switches_the_history_fill_on_and_changes_nothing_else_in_behaviour_settings()
        {
            GivenTheCallerAdministersTheInstance();
            GivenTheInstanceAsAnEarlierReleaseLeftIt(HowAnEarlierReleaseLeftTheFill.SwitchedOff);
            var before = await GivenWhatTheAdminSawInBehaviourSettingsBeforeTheUpgrade();

            WhenTheInstanceIsUpgraded();
            var after = await WhenTheAdminOpensBehaviourSettings();

            ThenOnlyTheHistoryFillWasSwitchedOn(before, after);
        }

        // @driving_port @real-io @kpi @contract-shape:unbounded-preservation
        // The switch-on happens once. An admin who disagrees with it is answered on every later start-up.
        [Test]
        public async Task An_admin_who_switches_the_history_fill_off_after_the_upgrade_keeps_it_off_across_every_restart()
        {
            GivenTheCallerAdministersTheInstance();
            await GivenTheInstanceWasUpgradedFromAnEarlierReleaseThatLeftTheFillOff();

            await WhenTheAdminSwitchesTheHistoryFillOff();
            WhenTheInstanceRestarts(times: 3);
            var fill = await WhenTheAdminReadsTheHistoryFillSetting();

            ThenTheHistoryFillReadsOff(fill);
        }

        // @driving_port @real-io @contract-shape:unbounded-preservation
        // A new instance is not an earlier release. Its admin's off is kept from the very first restart.
        [Test]
        public async Task A_new_instance_whose_admin_switches_the_history_fill_off_keeps_it_off_across_restarts()
        {
            GivenTheCallerAdministersTheInstance();
            await GivenANewInstanceWhoseAdminFindsTheHistoryFillOn();

            await WhenTheAdminSwitchesTheHistoryFillOff();
            WhenTheInstanceRestarts(times: 2);
            var fill = await WhenTheAdminReadsTheHistoryFillSetting();

            ThenTheHistoryFillReadsOff(fill);
        }

        // @driving_port @real-io @error @contract-shape:bounded-change
        // The setting was deleted by hand after the one-time switch-on had already happened. It must come
        // back, or the admin has nothing to switch, and it comes back on: a missing setting is added the way a
        // new instance gets it, whatever the instance remembers about an earlier switch-on.
        [Test]
        public async Task A_history_fill_setting_removed_by_hand_comes_back_on_at_the_next_restart()
        {
            GivenTheCallerAdministersTheInstance();
            await GivenANewInstanceWhoseAdminFindsTheHistoryFillOn();
            GivenTheHistoryFillSettingWasRemovedByHand();

            WhenTheInstanceRestarts(times: 1);
            var fill = await WhenTheAdminReadsTheHistoryFillSetting();

            ThenTheHistoryFillReadsOn(fill);
        }

        // @driving_port @real-io @error @contract-shape:unbounded-preservation
        // A restore brings back exactly what the backup held. Restoring the backup taken before the upgrade is
        // how an admin undoes filled days, so a fill the backup held off must come back off, and stay off at
        // the next restart rather than being switched on as if this were an upgrade.
        [Test]
        public async Task Restoring_a_backup_from_before_this_release_brings_the_history_fill_back_off_and_a_restart_keeps_it_off()
        {
            GivenTheCallerAdministersTheInstance();
            GivenABackupFromBeforeThisReleaseThatHeldTheHistoryFillOff();

            await WhenTheAdminRestoresTheBackup();
            var afterTheRestore = await WhenTheAdminReadsTheHistoryFillSetting();
            WhenTheInstanceRestarts(times: 1);
            var afterTheRestart = await WhenTheAdminReadsTheHistoryFillSetting();

            using (Assert.EnterMultipleScope())
            {
                ThenTheHistoryFillReadsAsTheBackupHeldIt(afterTheRestore);
                ThenTheHistoryFillReadsOff(afterTheRestart);
                ThenTheInstanceRemembersItSwitchedTheFillOn();
            }
        }

        // @driving_port @real-io @contract-shape:bounded-change
        // Clearing the database leaves a new instance behind, and a new instance has the fill on, whatever the
        // instance held before.
        [Test]
        public async Task Clearing_the_database_leaves_the_history_fill_switched_on_like_a_new_instance()
        {
            GivenTheCallerAdministersTheInstance();
            GivenTheInstanceAsAnEarlierReleaseLeftIt(HowAnEarlierReleaseLeftTheFill.SwitchedOff);

            await WhenTheAdminClearsTheDatabase();
            var fill = await WhenTheAdminReadsTheHistoryFillSetting();

            using (Assert.EnterMultipleScope())
            {
                ThenTheHistoryFillReadsOn(fill);
                ThenTheInstanceRemembersItSwitchedTheFillOn();
            }
        }
    }
}
