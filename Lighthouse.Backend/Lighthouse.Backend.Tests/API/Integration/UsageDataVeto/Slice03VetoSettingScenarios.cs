using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.UsageDataVeto
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5733 slice 03, ADO #5836) - the administrator's veto as a
    /// setting. Driving port: the optional-feature toggle port and its read port. US-06 (AC-06.1,
    /// AC-06.2) and US-07 as an inherited precondition (AC-07.1, AC-07.3, AC-07.4).
    /// <para>
    /// The row is a veto, not an enable. It ships disengaged, so an instance that upgrades keeps
    /// behaving exactly as it did; engaging it is a deliberate act, and only a licensed instance can
    /// perform it. Everything below is phrased in those terms - "engaged" means the stored value is
    /// true and nothing is sent - because reading this file with the opposite polarity in mind
    /// inverts every assertion in it.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("slice-03")]
    public partial class Slice03VetoSettingTest
    {
        // @driving_port @real-io @AC-06.1 - the row itself. Until it exists the master switch reads no
        // row, concludes the veto is disengaged, and no control renders anywhere.
        [Test]
        public async Task The_veto_is_offered_to_administrators_as_a_setting_of_its_own()
        {
            GivenTheCallerAdministersTheInstance();

            var settings = await WhenAnyoneReadsTheBehaviourSettings();

            ThenTheVetoIsAmongThem(settings);
        }

        // @driving_port @real-io @AC-06.1 @DT-23 - and it ships disengaged. This is the one value the
        // seeder writes exactly once: it never overwrites the stored on/off of a key it already knows,
        // so a release that ships the wrong default cannot be repaired by seeding again.
        [Test]
        public void A_fresh_instance_starts_with_the_veto_disengaged()
        {
            ThenTheStoredVetoIsDisengaged();
        }

        // @driving_port @real-io @AC-06.1 - premium, which is what makes the Community half of AC-07.4
        // a commercial line rather than a missing feature.
        [Test]
        public void The_veto_is_a_premium_setting()
        {
            ThenTheVetoIsPremium();
        }

        // @driving_port @real-io @AC-06.2 - the upgrade case. An administrator who engaged the veto has
        // made a policy decision, and the next release may not quietly undo it.
        [Test]
        public async Task A_later_upgrade_leaves_an_engaged_veto_engaged()
        {
            GivenTheInstanceIsLicensedForPremium();
            GivenTheCallerAdministersTheInstance();
            await WhenTheAdminEngagesTheVeto();

            WhenTheInstanceIsUpgraded();

            ThenTheStoredVetoIsEngaged();
        }

        // @driving_port @real-io @AC-06.1 - the licensed half of the branch. Without it the slice could
        // be passed by refusing everybody.
        [Test]
        public async Task A_licensed_administrator_engages_the_veto_and_is_told_it_landed()
        {
            GivenTheInstanceIsLicensedForPremium();
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminEngagesTheVeto();

            ThenTheToggleWasTaken(response);
            ThenTheStoredVetoIsEngaged();
        }

        // @driving_port @real-io @AC-07.1 @AC-07.4 - a Community administrator is refused out loud. A
        // privacy control whose write can be dropped while the response says success is worse than no
        // control, which is why this is asserted here and not only where the gate was written.
        [Test]
        public async Task A_community_administrator_is_refused_the_veto_out_loud()
        {
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminEngagesTheVeto();

            ThenTheRefusalIsForbidden(response);
            ThenTheRefusalSaysPremiumIsRequired(response);
            ThenTheStoredVetoIsDisengaged();
        }

        // @driving_port @real-io @AC-07.4 - and it is not hidden from them. Seeing that the control
        // exists, seeing that usage data is flowing, and seeing that stopping it needs Premium is the
        // whole of what a Community administrator is owed here.
        [Test]
        public async Task A_community_administrator_can_still_see_the_veto_and_see_that_it_is_disengaged()
        {
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var settings = await WhenAnyoneReadsTheBehaviourSettings();

            ThenTheVetoIsAmongThem(settings);
            ThenTheVetoReadsAsDisengagedAndPremium(settings);
        }

        // @driving_port @real-io @DT-30 - nothing is wired to run when this row is written. The gate
        // reads the row on every emit, so there is no consequence to apply, and a later refactor that
        // attaches one would change when the veto takes effect without changing a single scenario above.
        [Test]
        public async Task Engaging_the_veto_stores_the_value_and_leaves_every_other_setting_alone()
        {
            GivenTheInstanceIsLicensedForPremium();
            GivenTheCallerAdministersTheInstance();

            var before = await WhenAnyoneReadsTheBehaviourSettings();
            await WhenTheAdminEngagesTheVeto();
            var after = await WhenAnyoneReadsTheBehaviourSettings();

            ThenTheOnlySettingThatChangedIsTheVeto(before, after);
        }

        // @driving_port @real-io @AC-07.1 - NOT ignored, and deliberately so. This slice inherits the
        // refusal from story #5876 rather than writing it. If that story is ever reverted, or its own
        // fixture archived with it, every scenario above would go green against a gate that silently
        // drops the write - so this Epic checks the inherited promise for itself.
        [Test]
        public async Task The_refusal_this_slice_depends_on_is_present_before_anything_here_relies_on_it()
        {
            var premiumSetting = GivenAPremiumBehaviourSetting();
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminTurnsItOn(premiumSetting);

            ThenTheRefusalIsForbidden(response);
            ThenTheStoredSettingIsStillOff(premiumSetting);
        }

        // @driving_port @real-io @AC-07.3 - NOT ignored. This Epic's invariant wherever the premium fix
        // was written: Faster Updates is not premium and may not become gated by it.
        [Test]
        public async Task Faster_updates_still_toggles_on_an_instance_with_no_premium_licence()
        {
            var shipped = GivenTheShippedSettingThatIsNotPremium();
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminTurnsItOn(shipped);

            ThenTheToggleWasTaken(response);
        }
    }
}
