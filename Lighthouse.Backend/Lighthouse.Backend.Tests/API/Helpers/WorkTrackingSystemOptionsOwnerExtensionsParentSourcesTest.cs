using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    /// <summary>What a Team or Portfolio save means when it carries the list, the old single field, or both.</summary>
    [TestFixture]
    public class WorkTrackingSystemOptionsOwnerExtensionsParentSourcesTest
    {
        private const int TheFirstField = 3;

        private const int AnotherField = 9;

        private static readonly ParentSources Held = ParentSources.Of(
            [ParentSourceEntry.Field(TheFirstField), ParentSourceEntry.TrackerParent]).List!;

        private static Team AnOwnerHoldingTheList() => new Team().OwningParentSources(Held);

        private static List<ParentSourceEntryDto?> TheListItRead() => ParentSourceEntryDto.ListOf(Held);

        private static List<ParentSourceEntryDto?> AnotherList() => ParentSourceEntryDto.ListOf(ParentSources.OnlyTheTrackersOwnParent);

        [Test]
        public void A_save_choosing_to_own_the_list_it_read_keeps_it_when_it_names_no_old_field()
        {
            var save = new TeamSettingDto { ParentSources = TheListItRead(), ParentOverrideSource = SettingSource.Own };

            var outcome = AnOwnerHoldingTheList().ParentSourcesOwnedOnceTaken(save);

            Assert.That(outcome?.List?.ToStoredText(), Is.EqualTo(Held.ToStoredText()));
        }

        [Test]
        public void A_save_naming_no_source_owns_a_list_that_differs_from_the_one_the_owner_reads()
        {
            var save = new TeamSettingDto { ParentSources = AnotherList() };

            var outcome = AnOwnerHoldingTheList().ParentSourcesOwnedOnceTaken(save);

            Assert.That(outcome?.List?.ToStoredText(), Is.EqualTo(ParentSources.OnlyTheTrackersOwnParent.ToStoredText()));
        }

        [Test]
        public void A_save_naming_no_source_only_echoes_the_list_the_owner_reads()
        {
            var save = new TeamSettingDto { ParentSources = TheListItRead() };

            Assert.That(AnOwnerHoldingTheList().ParentSourcesOwnedOnceTaken(save), Is.Null);
        }

        [Test]
        public void A_changed_old_field_beside_the_list_the_client_read_edits_the_field_and_not_the_list()
        {
            var owner = AnOwnerHoldingTheList();
            var save = new TeamSettingDto
            {
                ParentSources = TheListItRead(),
                ParentOverrideAdditionalFieldDefinitionId = AnotherField,
                ParentOverrideSource = SettingSource.Own,
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(owner.ParentSourcesOwnedOnceTaken(save), Is.Null);
                Assert.That(owner.ParentOverrideFieldAsSent(save), Is.EqualTo(AnotherField));
            }
        }

        [Test]
        public void An_old_field_beside_a_new_list_has_nothing_left_to_say()
        {
            var save = new TeamSettingDto { ParentSources = AnotherList(), ParentOverrideAdditionalFieldDefinitionId = AnotherField };

            Assert.That(AnOwnerHoldingTheList().ParentOverrideFieldAsSent(save), Is.Null);
        }

        [Test]
        public void A_save_without_a_list_means_the_old_field_it_sends()
        {
            var save = new TeamSettingDto { ParentOverrideAdditionalFieldDefinitionId = AnotherField };

            Assert.That(AnOwnerHoldingTheList().ParentOverrideFieldAsSent(save), Is.EqualTo(AnotherField));
        }
    }
}
