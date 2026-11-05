using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// The list of places a Team or a Portfolio reads its parent from, with no tracker, no database and no
    /// host: what a list may hold, what it is stored as, and what a build one release back would make of it.
    /// </summary>
    [TestFixture]
    public class ParentSourcesTest
    {
        private const string TheOwnParentAfterField12 =
            """[{"Kind":1,"AdditionalFieldDefinitionId":12},{"Kind":0,"AdditionalFieldDefinitionId":null}]""";

        private const string OnlyTheOwnParent = """[{"Kind":0,"AdditionalFieldDefinitionId":null}]""";

        private static readonly ParentSourceEntry OwnParent = ParentSourceEntry.TrackerParent;

        private static ParentSourceEntry Field(int id) => ParentSourceEntry.Field(id);

        private static IEnumerable<TestCaseData> ListsThatCanBeKept()
        {
            yield return new TestCaseData((object)new[] { OwnParent }).SetName("One entry: only the tracker's own parent");
            yield return new TestCaseData((object)new[] { Field(11) }).SetName("One entry: only a field");
            yield return new TestCaseData((object)new[] { Field(12), Field(11), OwnParent }).SetName("The order given is the order kept");
            yield return new TestCaseData((object)new[] { OwnParent, Field(11), Field(12), Field(13), Field(14) }).SetName("Five entries, the most a list holds");
        }

        [TestCaseSource(nameof(ListsThatCanBeKept))]
        public void Of_one_to_five_different_entries_keeps_them_in_the_order_given(ParentSourceEntry[] entries)
        {
            var outcome = ParentSources.Of(entries);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Violations, Is.Empty);
                Assert.That(outcome.List, Is.Not.Null);
                Assert.That(outcome.List!.Entries, Is.EqualTo(entries).AsCollection,
                    "A reordered list changes which parent wins, so the order must come back exactly as it went in.");
            }
        }

        private static IEnumerable<TestCaseData> ListsThatAreRefused()
        {
            yield return new TestCaseData(Array.Empty<ParentSourceEntry>(), ParentSourcesViolation.Empty)
                .SetName("An empty list: a Work Item would have nowhere to look");
            yield return new TestCaseData(
                    new[] { Field(11), Field(12), Field(13), Field(14), Field(15), Field(16) }, ParentSourcesViolation.TooMany)
                .SetName("Six entries: one more than a list holds");
            yield return new TestCaseData(new[] { Field(11), Field(12), Field(11) }, ParentSourcesViolation.Duplicate)
                .SetName("The same field twice");
            yield return new TestCaseData(new[] { OwnParent, Field(11), OwnParent }, ParentSourcesViolation.Duplicate)
                .SetName("The tracker's own parent twice");
        }

        [TestCaseSource(nameof(ListsThatAreRefused))]
        public void Of_refuses_a_list_that_cannot_be_kept_and_says_why(ParentSourceEntry[] entries, ParentSourcesViolation expected)
        {
            var outcome = ParentSources.Of(entries);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.List, Is.Null, "A refused list must not be half-kept.");
                Assert.That(outcome.Violations, Does.Contain(expected));
            }
        }

        private static IEnumerable<TestCaseData> ListsAndWhatAnOlderBuildReadsOfThem()
        {
            yield return new TestCaseData(new[] { Field(11), OwnParent }, 11).SetName("Field then own parent: the old override");
            yield return new TestCaseData(new[] { OwnParent }, (int?)null).SetName("Own parent only: no override");
            yield return new TestCaseData(new[] { Field(11), Field(12), OwnParent }, 11).SetName("Two fields: the second is lost");
            yield return new TestCaseData(new[] { OwnParent, Field(11) }, 11).SetName("Own parent first: the order is lost");
            yield return new TestCaseData(new[] { Field(11), Field(12) }, 11).SetName("Fields only: the decision not to use the own parent is lost");
        }

        [TestCaseSource(nameof(ListsAndWhatAnOlderBuildReadsOfThem))]
        public void ToTheLegacyField_is_the_first_field_of_the_list_or_nothing(ParentSourceEntry[] entries, int? whatAnOlderBuildReads)
        {
            var list = ParentSources.Of(entries).List!;

            Assert.That(list.ToTheLegacyField(), Is.EqualTo(whatAnOlderBuildReads),
                "The previous build reads only this one value, so it is what keeps a rollback honest for the two shapes it can express.");
        }

        [TestCase(TheOwnParentAfterField12, 12, true)]
        [TestCase(OnlyTheOwnParent, null, false)]
        public void The_stored_text_is_exactly_what_the_upgrade_writes_and_reads_back_identically(string storedText, int? firstField, bool startsWithAField)
        {
            var list = ParentSources.FromStoredText(storedText);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.ToStoredText(), Is.EqualTo(storedText),
                    "The data step writes this text in SQL, so the serialiser has to produce it byte for byte, or the two drift apart unseen.");
                Assert.That(list.ToTheLegacyField(), Is.EqualTo(firstField));
                Assert.That(list.Entries[0].Kind, Is.EqualTo(startsWithAField ? ParentSourceKind.AdditionalField : ParentSourceKind.TrackerParent));
            }
        }

        [Test]
        public void A_list_built_from_entries_stores_the_same_text_the_upgrade_writes()
        {
            var list = ParentSources.Of([Field(12), OwnParent]).List!;

            Assert.That(list.ToStoredText(), Is.EqualTo(TheOwnParentAfterField12));
        }

        // Built from raw text on purpose: an entry that names no field or carries a field it should not cannot
        // be constructed at all, so the only way such a list reaches the code is as stored text. The design names
        // no exception type for a refused read, only that it is not repaired, so any exception is accepted here.
        [TestCase("""[]""", Description = "An empty list")]
        [TestCase("""[{"Kind":1,"AdditionalFieldDefinitionId":4},{"Kind":1,"AdditionalFieldDefinitionId":4}]""", Description = "A duplicate")]
        [TestCase("""[{"Kind":0,"AdditionalFieldDefinitionId":3}]""", Description = "The own parent carrying a field id")]
        [TestCase("""[{"Kind":1,"AdditionalFieldDefinitionId":null}]""", Description = "A field naming no field")]
        [TestCase("""[{"Kind":7,"AdditionalFieldDefinitionId":null}]""", Description = "A kind this version does not know")]
        public void Stored_text_the_rules_refuse_fails_loudly_instead_of_being_repaired(string storedText)
        {
            var thrown = Assert.Catch(() => ParentSources.FromStoredText(storedText));

            Assert.That(thrown, Is.Not.InstanceOf<NotImplementedException>(),
                "A silently repaired list changes where parents come from, so a bad text has to stop the read.");
        }
    }
}
