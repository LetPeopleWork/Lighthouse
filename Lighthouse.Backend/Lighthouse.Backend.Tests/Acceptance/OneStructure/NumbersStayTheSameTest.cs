namespace Lighthouse.Backend.Tests.Acceptance.OneStructure
{
    /// <summary>
    /// Restructuring Teams and Portfolios must not change a single number anybody reads. For the demo data and for an
    /// installation shaped like the ones people upgrade from, every unit's item counts by type, 90-day throughput, Work
    /// Item Age and every Feature's When forecast are written out in the expectation file beside this test, and every
    /// later change is measured against them. A number that moves is named with its unit.
    /// </summary>
    [TestFixture]
    public class NumbersStayTheSameTest
    {
        [TestCase(Installation.DemoData)]
        [TestCase(Installation.UpgradeFixture)]
        public async Task Every_units_numbers_stay_as_recorded_and_two_recordings_agree(Installation installation)
        {
            var expected = GivenTheNumbersRecordedFor(installation);

            var (first, second) = await WhenTheNumbersAreRecordedTwice(installation);

            ThenTheTwoRecordingsAgree(installation, first, second);
            ThenEveryNumberIsAsRecorded(installation, first, expected);
        }

        private static UnitNumbers GivenTheNumbersRecordedFor(Installation installation) => UnitNumbersRecorder.Expected(installation);

        private static async Task<(UnitNumbers First, UnitNumbers Second)> WhenTheNumbersAreRecordedTwice(Installation installation)
            => (await UnitNumbersRecorder.Record(installation), await UnitNumbersRecorder.Record(installation));

        private static void ThenTheTwoRecordingsAgree(Installation installation, UnitNumbers first, UnitNumbers second)
        {
            var differences = second.DifferencesFrom(first);

            Assert.That(differences, Is.Empty,
                $"Recording the {installation} twice on the same code gave different numbers:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", differences)}{Environment.NewLine}A recording that does not agree with itself cannot tell a changed number from noise. Pin whatever the differing number reads (the clock, the random draws, the order of the records) so that two recordings agree.");
        }

        private static void ThenEveryNumberIsAsRecorded(Installation installation, UnitNumbers recorded, UnitNumbers expected)
        {
            var differences = recorded.DifferencesFrom(expected);

            if (differences.Count == 0)
            {
                return;
            }

            var kept = UnitNumbersRecorder.KeepForComparison(installation, recorded);

            Assert.Fail(
                $"{differences.Count} numbers of the {installation} are not the ones recorded:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", differences)}{Environment.NewLine}Nobody's numbers may change when Teams and Portfolios become units: each line names the unit and the number it shows. Find the change that moved it and undo it. What was recorded now is in {kept}.");
        }
    }
}
