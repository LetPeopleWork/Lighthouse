using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BehaviourSettings
{
    /// <summary>
    /// DISTILL acceptance scenarios (Story 5876) - Slice 01: a refused toggle says so. Driving port: the
    /// optional-feature toggle port and its read port. US-02 (AC-02.1 ... AC-02.5).
    /// <para>
    /// This slice ships before the move, because slice 02 relocates a setting whose 403 is already a
    /// shipped promise onto an endpoint that today answers 200 carrying the unchanged row. The scenarios
    /// therefore run against a premium row of their own making: no live premium optional feature exists
    /// until slice 02, and depending on one would invert the slice order the whole story rests on.
    /// </para>
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-5876-behaviour-settings")]
    [Category("slice-01")]
    public partial class Slice01PremiumRefusalTest
    {
        // @driving_port @real-io @AC-02.1 - the promise of the slice. A refusal that returns 200 carrying
        // the old value is worse than an error: the caller is told the write landed.
        [Test]
        public async Task A_toggle_the_licence_does_not_cover_is_refused_out_loud()
        {
            var premiumSetting = GivenAPremiumBehaviourSetting();
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminTurnsItOn(premiumSetting);

            ThenTheRefusalIsForbidden(response);
        }

        // @driving_port @real-io @AC-02.1 - the refusal has to look like the one the other door already
        // gives, or the two doors onto the same setting answer a client differently.
        [Test]
        public async Task The_refusal_reads_the_same_as_the_one_the_other_door_already_gives()
        {
            var premiumSetting = GivenAPremiumBehaviourSetting();
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminTurnsItOn(premiumSetting);

            ThenTheRefusalSaysPremiumIsRequired(response);
        }

        // @driving_port @real-io @AC-02.1 - and nothing is written. Asserted through the read port as well
        // as the store, because a caller can only see the former.
        [Test]
        public async Task A_refused_toggle_leaves_the_setting_exactly_as_it_was()
        {
            var premiumSetting = GivenAPremiumBehaviourSetting();
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var before = await WhenAnyoneReadsTheBehaviourSettings();
            await WhenTheAdminTurnsItOn(premiumSetting);
            var after = await WhenAnyoneReadsTheBehaviourSettings();

            ThenTheSettingsAreUnchanged(before, after);
            ThenTheStoredSettingIsStillOff(PremiumFixtureKey);
        }

        // @driving_port @real-io @AC-02.2 - the licensed half of the same branch. Without it the slice
        // could be passed by refusing everybody.
        [Test]
        public async Task A_toggle_the_licence_covers_is_taken_and_reported_back()
        {
            var premiumSetting = GivenAPremiumBehaviourSetting();
            GivenTheInstanceIsLicensedForPremium();
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminTurnsItOn(premiumSetting);

            ThenTheToggleWasTaken(response);
            ThenTheStoredSettingIsOn(PremiumFixtureKey);
        }

        // @driving_port @real-io @AC-02.3 - Faster Updates is not premium and must never become gated by
        // this fix. Asserted on both licence states explicitly rather than implied by the premium cases:
        // an inverted check refuses everything and would pass every scenario above.
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task The_setting_the_licence_has_nothing_to_say_about_is_taken_either_way(bool licensed)
        {
            var fasterUpdates = GivenTheShippedNonPremiumBehaviourSetting();
            GivenTheInstanceLicenceState(licensed);
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminTurnsItOn(fasterUpdates);

            ThenTheToggleWasTaken(response);
            ThenTheStoredSettingIsOn(ShippedNonPremiumKey);
        }

        // @AC-02.3 - and it stays non-premium. A fix that gated it would still pass
        // the two cases above on a licensed instance.
        [Test]
        public void The_setting_the_licence_has_nothing_to_say_about_is_still_not_premium()
        {
            GivenTheShippedNonPremiumBehaviourSetting();

            ThenTheStoredSettingIsNotPremium(ShippedNonPremiumKey);
        }

        // @driving_port @real-io @AC-02.1 - the door this setting has today already refuses correctly, and
        // that refusal is the shipped promise (Epic #5375 AC-2.5) the whole slice order exists to protect.
        // Nothing asserted it until now, so the criterion the sequencing defends was itself untested.
        [Test]
        public async Task The_door_this_setting_has_today_already_refuses_an_unlicensed_administrator()
        {
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminHandsTheOrderOverThroughTheDoorItHasToday();

            ThenTheRefusalIsForbidden(response);
            ThenTheRefusalSaysPremiumIsRequired(response);
        }

        // @driving_port @real-io @AC-02.1 - and once the setting has two doors, both must refuse alike.
        // The design says the new path's refusal is "matched by hand" against the attribute's; that is a
        // claim about two strings in two files, and it is only true for as long as something compares them.
        [Test]
        public async Task Both_doors_refuse_an_unlicensed_administrator_in_the_same_words()
        {
            var premiumSetting = GivenAPremiumBehaviourSetting();
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var throughTheDoorItHasToday = await WhenTheAdminHandsTheOrderOverThroughTheDoorItHasToday();
            var throughTheNewDoor = await WhenTheAdminTurnsItOn(premiumSetting);

            ThenBothRefusalsAreIdentical(throughTheDoorItHasToday, throughTheNewDoor);
        }

        // @driving_port @real-io @AC-02.1 - a setting that does not exist is still Not Found, licence or
        // no licence. The refusal runs after the lookup; hoisting it above would answer 403 for a row
        // nobody could name, and lose the answer the caller actually needs.
        [Test]
        public async Task A_setting_that_does_not_exist_is_still_reported_as_not_found()
        {
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var response = await WhenTheAdminTurnsOnASettingThatDoesNotExist();

            ThenItWasReportedAsNotFound(response);
        }
    }
}
