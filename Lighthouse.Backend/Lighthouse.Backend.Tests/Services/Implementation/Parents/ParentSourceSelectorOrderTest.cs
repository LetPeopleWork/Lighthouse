using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Parents;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Parents
{
    /// <summary>
    /// The order a record's parent sources are read in, with no tracker in sight: what each position found
    /// goes in, the parent that wins comes out. A Team narrows link candidates to the Features Lighthouse
    /// holds; a Portfolio does not, because the level above it is stored only once it has been read.
    /// </summary>
    [TestFixture]
    public class ParentSourceSelectorOrderTest
    {
        private static readonly ParentSourceEntry OwnParent = ParentSourceEntry.TrackerParent;

        private static readonly ParentSourceEntry CausedBy = ParentSourceEntry.Field(11);

        private static readonly ParentSourceEntry ProgramRef = ParentSourceEntry.Field(12);

        private static readonly ParentSourceEntry Sponsor = ParentSourceEntry.Field(13);

        private static ParentReadingAt At(ParentSourceEntry source, ParentReading reading) => new(source, reading);

        private static ParentReading LinksTo(params string[] keys)
            => ParentReading.LinkCandidates(ParentResolution.From([.. keys.Select(key => new ParentCandidate(key, null))]));

        private static StoredFeatures Holding(params string[] keys)
            => new(keys.ToDictionary(key => key, _ => (DateTime?)null));

        public sealed record Chosen(string Name, ParentReadingAt[] Positions, string[] Features, ParentNarrowing Narrowing, string Key, int? Winner)
        {
            public override string ToString() => Name;
        }

        private static IEnumerable<Chosen> TheWinnerOfEveryOrder()
        {
            yield return new("The first listed source with a value wins",
                [At(ProgramRef, ParentReading.FieldValue("PAY-12")), At(OwnParent, ParentReading.TrackersOwn("PAY-1"))],
                [], ParentNarrowing.Narrow, "PAY-12", 0);

            yield return new("Listed first, the tracker's own parent wins over a field",
                [At(OwnParent, ParentReading.TrackersOwn("PAY-1")), At(ProgramRef, ParentReading.FieldValue("PAY-12"))],
                [], ParentNarrowing.Narrow, "PAY-1", 0);

            yield return new("A source with no value hands over to the next",
                [At(ProgramRef, ParentReading.NoValue), At(OwnParent, ParentReading.TrackersOwn("PAY-1"))],
                [], ParentNarrowing.Narrow, "PAY-1", 1);

            yield return new("No listed source yields, and the Work Item's only parent is the tracker's own, which is not listed: no Feature, nothing is guessed",
                [At(CausedBy, ParentReading.NoValue), At(ProgramRef, ParentReading.NoValue)],
                [], ParentNarrowing.Narrow, "", null);

            yield return new("A field with a value wins even when it names no Feature Lighthouse holds",
                [At(ProgramRef, ParentReading.FieldValue("EXT-1")), At(Sponsor, ParentReading.FieldValue("PAY-12"))],
                ["PAY-12"], ParentNarrowing.Narrow, "EXT-1", 0);

            yield return new("A source that could not be read yields nothing and the next is read",
                [At(CausedBy, ParentReading.CannotBeRead), At(ProgramRef, ParentReading.FieldValue("PAY-12"))],
                [], ParentNarrowing.Narrow, "PAY-12", 1);

            yield return new("A link to one stored Feature wins",
                [At(CausedBy, LinksTo("PAY-7")), At(OwnParent, ParentReading.TrackersOwn("PAY-1"))],
                ["PAY-7"], ParentNarrowing.Narrow, "PAY-7", 0);

            yield return new("A link whose only candidate is no stored Feature hands over to the next source",
                [At(CausedBy, LinksTo("INC-88")), At(ProgramRef, ParentReading.FieldValue("PAY-12"))],
                ["PAY-12"], ParentNarrowing.Narrow, "PAY-12", 1);

            yield return new("A link to no stored Feature, with nothing after it, leaves no parent",
                [At(CausedBy, LinksTo("INC-88"))],
                [], ParentNarrowing.Narrow, "", null);

            yield return new("Several link candidates: the one stored Feature among them wins",
                [At(CausedBy, LinksTo("INC-88", "PAY-7"))],
                ["PAY-7"], ParentNarrowing.Narrow, "PAY-7", 0);

            yield return new("A Portfolio does not narrow: a lone link candidate wins though nothing stores it yet",
                [At(CausedBy, LinksTo("PRG-2")), At(OwnParent, ParentReading.TrackersOwn("PRG-1"))],
                [], ParentNarrowing.DoNotNarrow, "PRG-2", 0);
        }

        [TestCaseSource(nameof(TheWinnerOfEveryOrder))]
        public void Choose_reads_the_positions_in_order_and_the_first_that_yields_wins(Chosen scenario)
        {
            var choice = ParentSourceSelector.Choose(ParentReadings.Of(scenario.Positions), Holding(scenario.Features), scenario.Narrowing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(choice.Key, Is.EqualTo(scenario.Key));
                Assert.That(choice.WinnerPosition, Is.EqualTo(scenario.Winner));
                Assert.That(choice.NeedsStoredFeatures, Is.False, "The full call has the stored Features in hand, so nothing is left to ask.");
            }
        }

        [Test]
        public void Choose_among_several_link_candidates_takes_the_oldest_link()
        {
            var links = ParentReading.LinkCandidates(ParentResolution.From(
            [
                new ParentCandidate("PAY-7", new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc)),
                new ParentCandidate("PAY-8", new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc)),
            ]));

            var choice = ParentSourceSelector.Choose(
                ParentReadings.Of(At(CausedBy, links)), Holding("PAY-7", "PAY-8"), ParentNarrowing.Narrow);

            Assert.That(choice.Key, Is.EqualTo("PAY-8"),
                "Several Features among the links are decided as the placement of a Work Item always was: oldest link first.");
        }

        public sealed record Provisional(string Name, ParentReadingAt[] Positions, ParentNarrowing Narrowing, string Key, bool NeedsStoredFeatures, int? Winner)
        {
            public override string ToString() => Name;
        }

        private static IEnumerable<Provisional> WhatTheConnectorCanDecideWithoutKnowingTheFeatures()
        {
            yield return new("A field with a value ahead of a link settles it: nothing to ask",
                [At(ProgramRef, ParentReading.FieldValue("PAY-12")), At(CausedBy, LinksTo("PAY-7"))],
                ParentNarrowing.Narrow, "PAY-12", false, 0);

            yield return new("A Team's lone link is taken at face value until the refresh has tested it against the stored Features",
                [At(CausedBy, ParentReading.NoValue), At(ProgramRef, LinksTo("PAY-7")), At(Sponsor, ParentReading.FieldValue("PAY-12"))],
                ParentNarrowing.Narrow, "PAY-7", true, 1);

            yield return new("A Team's several link candidates name nothing until the refresh has chosen among the stored Features",
                [At(CausedBy, LinksTo("PAY-7", "PAY-8"))],
                ParentNarrowing.Narrow, "", true, 0);

            yield return new("A Portfolio's lone link needs no Feature test",
                [At(CausedBy, LinksTo("PRG-2"))],
                ParentNarrowing.DoNotNarrow, "PRG-2", false, 0);

            yield return new("A Portfolio's several link candidates still wait for the refresh to choose",
                [At(CausedBy, LinksTo("PRG-2", "PRG-3"))],
                ParentNarrowing.DoNotNarrow, "", true, 0);

            yield return new("A link naming nobody is passed over and the next source is read",
                [At(CausedBy, ParentReading.LinkCandidates(default)), At(ProgramRef, ParentReading.FieldValue("PAY-12"))],
                ParentNarrowing.Narrow, "PAY-12", false, 1);

            yield return new("Nothing yields: no parent and nothing to ask",
                [At(CausedBy, ParentReading.NoValue)],
                ParentNarrowing.Narrow, "", false, null);
        }

        [TestCaseSource(nameof(WhatTheConnectorCanDecideWithoutKnowingTheFeatures))]
        public void ChooseProvisionally_stops_at_the_first_link_that_needs_a_Feature_to_be_tested(Provisional scenario)
        {
            var choice = ParentSourceSelector.ChooseProvisionally(ParentReadings.Of(scenario.Positions), scenario.Narrowing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(choice.Key, Is.EqualTo(scenario.Key));
                Assert.That(choice.NeedsStoredFeatures, Is.EqualTo(scenario.NeedsStoredFeatures));
                Assert.That(choice.WinnerPosition, Is.EqualTo(scenario.Winner));
            }
        }

        private static ParentReadings ReadingsWithTwoLinkEntriesAndAField()
            => ParentReadings.Of(
                At(CausedBy, LinksTo("PAY-7")),
                At(ProgramRef, LinksTo("PAY-8")),
                At(Sponsor, ParentReading.FieldValue("PAY-12")));

        private static WorkItemBase ARecordShowingTheFirstLinkAsItsParent()
        {
            var record = new WorkItemBase { ReferenceId = "PAY-142", ParentReferenceId = "PAY-7" };
            record.AdditionalFieldValues[11] = "PAY-7";
            record.AdditionalFieldValues[12] = string.Empty;
            record.AdditionalFieldValues[13] = "PAY-12";
            return record;
        }

        [Test]
        public void HangUnder_a_choice_moves_the_shown_key_to_the_winning_link_entry_and_leaves_a_field_alone()
        {
            var record = ARecordShowingTheFirstLinkAsItsParent();

            ParentSourceSelector.HangUnder(record, ReadingsWithTwoLinkEntriesAndAField(), new ParentChoice("PAY-8", 1, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(record.ParentReferenceId, Is.EqualTo("PAY-8"));
                Assert.That(record.AdditionalFieldValues[12], Is.EqualTo("PAY-8"), "The winning link entry shows the key that won.");
                Assert.That(record.AdditionalFieldValues[11], Is.Null.Or.Empty,
                    "The entry that lost shows nothing, or a key that was not acted on would send somebody looking for a parent that was never stored.");
                Assert.That(record.AdditionalFieldValues[13], Is.EqualTo("PAY-12"), "A field entry's value is the tracker's own, never something this writes.");
            }
        }

        [Test]
        public void HangUnder_an_empty_choice_takes_a_provisional_parent_back()
        {
            var record = ARecordShowingTheFirstLinkAsItsParent();

            ParentSourceSelector.HangUnder(record, ReadingsWithTwoLinkEntriesAndAField(), new ParentChoice(string.Empty, null, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(record.ParentReferenceId, Is.Empty, "A key that failed the Feature test must not stay behind as the stored parent.");
                Assert.That(record.AdditionalFieldValues[11], Is.Null.Or.Empty);
                Assert.That(record.AdditionalFieldValues[12], Is.Null.Or.Empty);
                Assert.That(record.AdditionalFieldValues[13], Is.EqualTo("PAY-12"));
            }
        }

        [Test]
        public void HangUnder_the_same_choice_twice_leaves_the_record_as_after_the_first_time()
        {
            var once = ARecordShowingTheFirstLinkAsItsParent();
            var twice = ARecordShowingTheFirstLinkAsItsParent();
            var choice = new ParentChoice("PAY-8", 1, false);
            var readings = ReadingsWithTwoLinkEntriesAndAField();

            ParentSourceSelector.HangUnder(once, readings, choice);
            ParentSourceSelector.HangUnder(twice, readings, choice);
            ParentSourceSelector.HangUnder(twice, readings, choice);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(twice.ParentReferenceId, Is.EqualTo(once.ParentReferenceId));
                Assert.That(twice.AdditionalFieldValues, Is.EquivalentTo(once.AdditionalFieldValues));
            }
        }
    }
}
