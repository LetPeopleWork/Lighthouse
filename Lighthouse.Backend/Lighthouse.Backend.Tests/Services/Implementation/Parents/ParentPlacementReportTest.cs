using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Parents;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Parents
{
    /// <summary>
    /// The words an administrator reads after a refresh that placed, or could not place, records whose links
    /// name several issues. The copy is approved text, so it is pinned here without a logger in the way.
    /// </summary>
    [TestFixture]
    public class ParentPlacementReportTest
    {
        private const string TheOwner = "Team \"Payments\"";

        private const string TheFeature = "PAY-88";

        private const string TheOtherFeature = "PAY-91";

        private const string AnIncident = "INC-5531";

        private const string AnotherIncident = "INC-5602";

        private const string AThirdIncident = "INC-5610";

        private static readonly string[] TheLineForOnePlacedRecord =
        [
            "Team \"Payments\" placed 1 record whose links name more than one issue: "
            + "PAY-311 under PAY-88 (only Feature; set aside INC-5531)",
        ];

        private static readonly string[] TheLineForOneRecordNotPlaced =
        [
            "Team \"Payments\" could not place 1 record whose links name more than one issue: "
            + "PAY-402 (linked to INC-5602, INC-5610; none is a Feature Lighthouse holds). It takes no parent from its links.",
        ];

        private static readonly string[] TheLinesForTwoOfEach =
        [
            "Team \"Payments\" placed 2 records whose links name more than one issue: "
            + "PAY-311 under PAY-88 (only Feature; set aside INC-5531); "
            + "PAY-450 under PAY-88 (only Feature; set aside INC-5602, INC-5610)",
            "Team \"Payments\" could not place 2 records whose links name more than one issue: "
            + "PAY-402 (linked to INC-5602, INC-5610; none is a Feature Lighthouse holds); "
            + "PAY-405 (linked to INC-5531, INC-5602; none is a Feature Lighthouse holds). It takes no parent from its links.",
        ];

        private static readonly string[] TheLineForTheOldestLink =
        [
            "Team \"Payments\" placed 1 record whose links name more than one issue: "
            + "PAY-420 under PAY-88 (oldest link, 2026-03-04; set aside PAY-91, 2026-06-17)",
        ];

        private static readonly string[] TheLineForTheEarliestCreated =
        [
            "Team \"Payments\" placed 1 record whose links name more than one issue: "
            + "PAY-421 under PAY-88 (earliest created, 2025-11-02; set aside PAY-91)",
        ];

        private static readonly string[] TheLineForTheLowestKey =
        [
            "Team \"Payments\" placed 1 record whose links name more than one issue: "
            + "PAY-422 under PAY-88 (lowest key; set aside PAY-91)",
        ];

        private static readonly DateTime LateOnMarch4 = new(2026, 3, 4, 23, 30, 0, DateTimeKind.Utc);

        private static readonly DateTime June17 = new(2026, 6, 17, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime November2 = new(2025, 11, 2, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime January15 = new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Team_NamesTheTeamInQuotes()
        {
            Assert.That(ParentPlacementReport.Team("Payments"), Is.EqualTo(TheOwner));
        }

        [Test]
        public void Lines_NoRecords_WritesNoLine()
        {
            Assert.That(ParentPlacementReport.Lines(TheOwner, []), Is.Empty,
                "A refresh where nothing had several candidates has nothing to warn about.");
        }

        [Test]
        public void Lines_OnePlacedRecord_WritesOnlyThePlacedLineInTheSingular()
        {
            var lines = ParentPlacementReport.Lines(TheOwner, [PlacedUnderTheFeature("PAY-311", AnIncident)]);

            Assert.That(lines, Is.EqualTo(TheLineForOnePlacedRecord));
        }

        [Test]
        public void Lines_OneRecordWhoseLinksNameNoFeature_WritesOnlyTheNotPlacedLineInTheSingular()
        {
            var lines = ParentPlacementReport.Lines(TheOwner,
                [NotPlaced("PAY-402", ParentNotPlacedReason.NoneIsAStoredFeature, AThirdIncident, AnotherIncident)]);

            Assert.That(lines, Is.EqualTo(TheLineForOneRecordNotPlaced),
                "The candidates are named in key order, whatever order the tracker listed the links in.");
        }

        [Test]
        public void Lines_SeveralOfEach_WritesOnePlacedLineThenOneNotPlacedLineInThePlural()
        {
            var lines = ParentPlacementReport.Lines(TheOwner,
            [
                PlacedUnderTheFeature("PAY-311", AnIncident),
                NotPlaced("PAY-402", ParentNotPlacedReason.NoneIsAStoredFeature, AnotherIncident, AThirdIncident),
                PlacedUnderTheFeature("PAY-450", AnotherIncident, AThirdIncident),
                NotPlaced("PAY-405", ParentNotPlacedReason.NoneIsAStoredFeature, AnotherIncident, AnIncident),
            ]);

            Assert.That(lines, Is.EqualTo(TheLinesForTwoOfEach),
                "One line per outcome for the whole fetch, never one per record, each naming every record it counts.");
        }

        [Test]
        public void Lines_PlacedByTheOldestLink_NamesTheLinkDayInUtcAndDatesTheSetAsideLink()
        {
            var lines = ParentPlacementReport.Lines(TheOwner, [Placed("PAY-420",
                [new ParentCandidate(TheOtherFeature, June17), new ParentCandidate(TheFeature, LateOnMarch4)],
                (TheFeature, January15), (TheOtherFeature, November2))]);

            Assert.That(lines, Is.EqualTo(TheLineForTheOldestLink), "The day is the UTC day of the link, so a link made late in the evening is not shown on the next day where the server runs.");
        }

        [Test]
        public void Lines_ALinkDayHeldInLocalTime_IsStillNamedByItsUtcDay()
        {
            var lines = ParentPlacementReport.Lines(TheOwner, [Placed("PAY-420",
                [new ParentCandidate(TheOtherFeature, June17), new ParentCandidate(TheFeature, LateOnMarch4.ToLocalTime())],
                (TheFeature, January15), (TheOtherFeature, November2))]);

            Assert.That(lines, Is.EqualTo(TheLineForTheOldestLink), "A server east of UTC holds this link as made on the 5th; the line still names the 4th.");
        }

        [Test]
        public void Lines_PlacedByTheEarliestCreatedFeature_NamesItsCreatedDayAndAddsNoDateToAnUndatedLink()
        {
            var lines = ParentPlacementReport.Lines(TheOwner, [Placed("PAY-421",
                [new ParentCandidate(TheOtherFeature, null), new ParentCandidate(TheFeature, null)],
                (TheFeature, November2), (TheOtherFeature, January15))]);

            Assert.That(lines, Is.EqualTo(TheLineForTheEarliestCreated), "A link with no recorded date is named without one, never with an invented date.");
        }

        [Test]
        public void Lines_PlacedByTheLowestKey_NamesNoDate()
        {
            var lines = ParentPlacementReport.Lines(TheOwner, [Placed("PAY-422",
                [new ParentCandidate(TheOtherFeature, null), new ParentCandidate(TheFeature, null)],
                (TheFeature, null), (TheOtherFeature, null))]);

            Assert.That(lines, Is.EqualTo(TheLineForTheLowestKey), "Nothing dated decided, so the line shows no date.");
        }

        private static RecordPlacement Placed(string referenceId, ParentCandidate[] candidates, params (string Key, DateTime? Created)[] storedFeatures)
        {
            var links = ParentResolution.From(candidates);
            var stored = new StoredFeatures(storedFeatures.ToDictionary(feature => feature.Key, feature => feature.Created, StringComparer.Ordinal));

            return new RecordPlacement(referenceId, links, links.Place(stored, ParentNarrowing.Narrow));
        }

        private static RecordPlacement PlacedUnderTheFeature(string referenceId, params string[] setAside)
        {
            var links = ParentResolution.From([TheFeature, .. setAside]);
            var placement = links.Place(new StoredFeatures(new Dictionary<string, DateTime?> { [TheFeature] = null }), ParentNarrowing.Narrow);

            return new RecordPlacement(referenceId, links, placement);
        }

        private static RecordPlacement NotPlaced(string referenceId, ParentNotPlacedReason reason, params string[] candidates)
            => new(referenceId, ParentResolution.From(candidates), ParentPlacement.NotPlacedBecause(reason));
    }
}
