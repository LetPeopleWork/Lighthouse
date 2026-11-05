using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Parents;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// What the owner and the connection write into the old single-field columns when they are given a list, and
    /// what a build one release back would make of that list. The old columns exist for that build only: for the
    /// two shapes it can express it must behave exactly as the list does, and for every other shape the result it
    /// gives is the documented limit of a rollback.
    /// </summary>
    [TestFixture]
    public class ParentSourcesOwnerWriteThroughTest
    {
        private const int A = 11;

        private const int B = 12;

        private const string TheOwnParent = "PAY-1";

        private static readonly ParentSourceEntry OwnParent = ParentSourceEntry.TrackerParent;

        private static ParentSources ListOf(params ParentSourceEntry[] entries) => ParentSources.Of(entries).List!;

        private static IEnumerable<TestCaseData> ListsAndTheOldColumnTheyWrite()
        {
            yield return new TestCaseData(ListOf(ParentSourceEntry.Field(A), OwnParent), (int?)A).SetName("A field then the own parent writes the field");
            yield return new TestCaseData(ListOf(OwnParent), (int?)null).SetName("The own parent alone writes nothing");
            yield return new TestCaseData(ListOf(ParentSourceEntry.Field(A), ParentSourceEntry.Field(B), OwnParent), (int?)A).SetName("Two fields write the first");
            yield return new TestCaseData(ListOf(OwnParent, ParentSourceEntry.Field(A)), (int?)A).SetName("The own parent first still writes the field");
            yield return new TestCaseData(ListOf(ParentSourceEntry.Field(A), ParentSourceEntry.Field(B)), (int?)A).SetName("Fields only write the first");
        }

        [TestCaseSource(nameof(ListsAndTheOldColumnTheyWrite))]
        [Ignore("Story 6501: enable with the owner's and the connection's list.")]
        public void An_owner_given_a_list_and_a_connection_default_write_the_old_column_through(ParentSources list, int? theOldColumn)
        {
            var team = new Team();
            var connection = new WorkTrackingSystemConnection();

            team.OwnParentSources(list);
            connection.SetParentSourcesDefaultForTeams(list);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.OwnParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(theOldColumn));
                Assert.That(connection.ParentOverrideForTeams, Is.EqualTo(theOldColumn));
            }
        }

        [Test]
        [Ignore("Story 6501: enable with the owner's and the connection's list.")]
        public void An_owner_that_follows_the_default_leaves_its_own_old_column_as_it_was()
        {
            var team = new Team();
            team.OwnParentOverride(A);

            team.FollowParentSourcesDefault();

            Assert.That(team.OwnParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(A),
                "Following writes nothing of its own, so what the owner held before is still there if it chooses again.");
        }

        public sealed record Rollback(string Name, ParentSourceEntry[] List, string WhatAHolds, string WhatBHolds, int? TheOldColumn, string TheListReads)
        {
            public override string ToString() => Name;
        }

        // The old column is stated as a literal for each shape. What a build one release back reads from it is in
        // each name: it reads that one field and, when the field holds nothing, the tracker's own parent.
        private static IEnumerable<Rollback> WhatEachShapeReadsBeforeAndAfterARollback()
        {
            yield return new("Field then own parent, field has a value: old build reads PAY-12 too", [ParentSourceEntry.Field(A), OwnParent], "PAY-12", "", A, "PAY-12");
            yield return new("Field then own parent, field empty: old build reads the own parent too", [ParentSourceEntry.Field(A), OwnParent], "", "", A, TheOwnParent);
            yield return new("The own parent alone: old build reads the own parent too", [OwnParent], "PAY-12", "", null, TheOwnParent);
            yield return new("Two fields then own parent, first empty: the list reads PAY-13, the old build the own parent", [ParentSourceEntry.Field(A), ParentSourceEntry.Field(B), OwnParent], "", "PAY-13", A, "PAY-13");
            yield return new("Own parent then field: the list reads the own parent, the old build PAY-12", [OwnParent, ParentSourceEntry.Field(A)], "PAY-12", "", A, TheOwnParent);
            yield return new("Two fields only, first empty: the list reads PAY-13, the old build the own parent", [ParentSourceEntry.Field(A), ParentSourceEntry.Field(B)], "", "PAY-13", A, "PAY-13");
        }

        [TestCaseSource(nameof(WhatEachShapeReadsBeforeAndAfterARollback))]
        public void Each_shape_writes_the_old_column_it_is_listed_with_and_the_list_reads_what_it_is_listed_to_read(Rollback shape)
        {
            var list = ListOf(shape.List);
            var readings = ParentReadings.Of([.. shape.List.Select(entry => new ParentReadingAt(entry, entry.Kind == ParentSourceKind.TrackerParent
                ? ParentReading.TrackersOwn(TheOwnParent)
                : TheValueOf(entry, shape)))]);

            var theListReads = ParentSourceSelector.Choose(readings, new StoredFeatures(new Dictionary<string, DateTime?>()), ParentNarrowing.DoNotNarrow).Key;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.ToTheLegacyField(), Is.EqualTo(shape.TheOldColumn));
                Assert.That(theListReads, Is.EqualTo(shape.TheListReads));
            }
        }

        private static ParentReading TheValueOf(ParentSourceEntry entry, Rollback shape)
        {
            var held = entry.AdditionalFieldDefinitionId == A ? shape.WhatAHolds : shape.WhatBHolds;

            return held.Length == 0 ? ParentReading.NoValue : ParentReading.FieldValue(held);
        }
    }
}
