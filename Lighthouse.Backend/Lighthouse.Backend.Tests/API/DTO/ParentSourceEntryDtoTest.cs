using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    public class ParentSourceEntryDtoTest
    {
        private const int TheFirstField = 3;

        private const int AnotherField = 9;

        private static readonly ParentSources Held = ParentSources.Of(
            [ParentSourceEntry.Field(TheFirstField), ParentSourceEntry.TrackerParent]).List!;

        private static List<ParentSourceEntryDto?> TheListAClientRead() => ParentSourceEntryDto.ListOf(Held);

        private static List<ParentSourceEntryDto?> AnotherList() => ParentSourceEntryDto.ListOf(ParentSources.OnlyTheTrackersOwnParent);

        [Test]
        public void No_list_reads_as_a_sent_list_of_nothing_and_a_held_list_as_the_copy_a_client_reads()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ParentSourceEntryDto.TextOf((ParentSources?)null), Is.EqualTo(ParentSourceEntryDto.TextOf((List<ParentSourceEntryDto?>?)null)));
                Assert.That(ParentSourceEntryDto.TextOf(Held), Is.EqualTo(ParentSourceEntryDto.TextOf(TheListAClientRead())));
                Assert.That(ParentSourceEntryDto.TextOf(Held), Is.Not.EqualTo(ParentSourceEntryDto.TextOf((ParentSources?)null)));
            }
        }

        [Test]
        public void A_changed_field_beside_the_list_the_client_read_is_an_edit_of_the_old_field()
        {
            Assert.That(ParentSourceEntryDto.TheOldFieldIsTheEdit(AnotherField, TheListAClientRead(), Held), Is.True);
        }

        [Test]
        public void The_lists_own_first_field_beside_the_list_the_client_read_is_no_edit_of_the_old_field()
        {
            Assert.That(ParentSourceEntryDto.TheOldFieldIsTheEdit(TheFirstField, TheListAClientRead(), Held), Is.False);
        }

        [Test]
        public void A_changed_field_beside_a_changed_list_is_no_edit_of_the_old_field()
        {
            Assert.That(ParentSourceEntryDto.TheOldFieldIsTheEdit(AnotherField, AnotherList(), Held), Is.False);
        }
    }
}
