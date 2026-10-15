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
            + "PAY-420 (linked to PAY-88, PAY-91; more than one is a Feature Lighthouse holds). It takes no parent from its links.",
        ];

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
                NotPlaced("PAY-420", ParentNotPlacedReason.MoreThanOneStoredFeature, TheFeature, TheOtherFeature),
            ]);

            Assert.That(lines, Is.EqualTo(TheLinesForTwoOfEach),
                "One line per outcome for the whole fetch, never one per record, each naming every record it counts.");
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
