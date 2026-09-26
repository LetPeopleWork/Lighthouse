using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Forecast;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    [TestFixture]
    public class RealityCheckVerdictPolicyTest
    {
        private const int ValueAt95 = 10;

        private const int ValueAt50 = 25;

        private static readonly RealityCheckForecastDto[] Forecast =
        [
            new(50, ValueAt50),
            new(70, 18),
            new(85, 14),
            new(95, ValueAt95),
        ];

        [TestCase(0, CellOutcome.OverForecast)]
        [TestCase(ValueAt95 - 1, CellOutcome.OverForecast)]
        [TestCase(ValueAt95, CellOutcome.WithinBand)]
        [TestCase(ValueAt95 + 1, CellOutcome.WithinBand)]
        [TestCase(ValueAt50 - 1, CellOutcome.WithinBand)]
        [TestCase(ValueAt50, CellOutcome.WithinBand)]
        [TestCase(ValueAt50 + 1, CellOutcome.UnderForecast)]
        public void Outcome_PlacesTheActualAgainstTheBandFromThe95ToThe50Value(int actualCompleted, CellOutcome expected)
        {
            Assert.That(RealityCheckVerdictPolicy.Outcome(actualCompleted, Forecast), Is.EqualTo(expected));
        }

        [TestCase(6, CellOutcome.OverForecast)]
        [TestCase(7, CellOutcome.WithinBand)]
        [TestCase(8, CellOutcome.UnderForecast)]
        public void Outcome_WhenEveryLevelForecastsTheSameValue_OnlyThatValueIsWithinTheBand(int actualCompleted, CellOutcome expected)
        {
            RealityCheckForecastDto[] flatForecast = [new(95, 7), new(85, 7), new(70, 7), new(50, 7)];

            Assert.That(RealityCheckVerdictPolicy.Outcome(actualCompleted, flatForecast), Is.EqualTo(expected));
        }

        [TestCase(ValueAt95 - 1, false)]
        [TestCase(ValueAt95, true)]
        [TestCase(ValueAt95 + 1, true)]
        public void Held_ALevelHoldsWhenTheActualReachesItsValue(int actualCompleted, bool expected)
        {
            Assert.That(RealityCheckVerdictPolicy.Held(actualCompleted, ValueAt95), Is.EqualTo(expected));
        }

        [TestCase(50, 8.0)]
        [TestCase(70, 11.2)]
        [TestCase(85, 13.6)]
        [TestCase(95, 15.2)]
        public void Coverage_ALevelIsExpectedToHoldInItsOwnPercentageOfTheEvaluatedChecks(int confidenceLevel, double expectedHeldCount)
        {
            var coverage = RealityCheckVerdictPolicy.Coverage(confidenceLevel, 10, 16);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(coverage.ConfidenceLevel, Is.EqualTo(confidenceLevel));
                Assert.That(coverage.HeldCount, Is.EqualTo(10));
                Assert.That(coverage.ExpectedHeldCount, Is.EqualTo(expectedHeldCount).Within(1e-9));
            }
        }

        [TestCase(50, 0, 0, LevelReading.NotEvaluated)]
        [TestCase(95, 0, 0, LevelReading.NotEvaluated)]
        [TestCase(50, 0, 2, LevelReading.NeverHeld)]
        [TestCase(95, 0, 1, LevelReading.SometimesHeld)]
        [TestCase(35, 0, 3, LevelReading.NeverHeld)]
        [TestCase(95, 1, 2, LevelReading.SometimesHeld)]
        [TestCase(95, 20, 20, LevelReading.AlwaysHeld)]
        [TestCase(99, 99, 99, LevelReading.SometimesHeld)]
        [TestCase(99, 100, 100, LevelReading.AlwaysHeld)]
        [TestCase(95, 16, 16, LevelReading.SometimesHeld)]
        [TestCase(50, 16, 16, LevelReading.AlwaysHeld)]
        [TestCase(85, 16, 16, LevelReading.AlwaysHeld)]
        [TestCase(85, 15, 16, LevelReading.SometimesHeld)]
        [TestCase(95, 0, 16, LevelReading.NeverHeld)]
        public void Coverage_NeverAndAlwaysAreFindingsOnlyWhenTheLevelsOwnRateExpectedAWholeCheckToGoTheOtherWay(
            int confidenceLevel, int heldCount, int runsEvaluated, LevelReading expected)
        {
            Assert.That(RealityCheckVerdictPolicy.Coverage(confidenceLevel, heldCount, runsEvaluated).Reading, Is.EqualTo(expected));
        }

        private static readonly int[] Ladder = [14, 30, 60, 90];

        private static readonly int[] LadderWithoutThirty = [14, 60, 90];

        private static readonly int[] LadderWithoutSixty = [14, 30, 90];

        private const int ChecksPerWindow = 4;

        private enum Check
        {
            FellShort,
            InsideTheBand,
            AboveTheBand,
            CouldNotRun,
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        public void SoundWindows_AWindowHoldsUpOnlyWhenItsMostCautiousForecastHeldInMoreThanHalfOfItsChecks(int timesHeld, bool holdsUp)
        {
            var cells = ChecksOf(30, [.. Enumerable.Repeat(Check.InsideTheBand, timesHeld), .. Enumerable.Repeat(Check.FellShort, ChecksPerWindow - timesHeld)])
                .Concat(EveryOtherWindow(30, Check.InsideTheBand))
                .ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 30);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays, Is.EqualTo(holdsUp ? Ladder : LadderWithoutThirty));
                Assert.That(soundWindow.Determination, Is.EqualTo(holdsUp ? Determination.AllWindowsAlike : Determination.SomeWindowsSound));
                Assert.That(soundWindow.CurrentSettingStanding, Is.EqualTo(holdsUp ? CurrentSettingStanding.Inside : CurrentSettingStanding.Outside));
            }
        }

        [Test]
        public void SoundWindows_DeliveringMoreThanForecastInEveryCheckNeverCountsAgainstAWindow()
        {
            var cells = ChecksOf(30, [.. Enumerable.Repeat(Check.AboveTheBand, ChecksPerWindow)])
                .Concat(EveryOtherWindow(30, Check.InsideTheBand))
                .ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 30);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays, Is.EqualTo(Ladder));
                Assert.That(soundWindow.Determination, Is.EqualTo(Determination.AllWindowsAlike));
                Assert.That(soundWindow.CurrentSettingStanding, Is.EqualTo(CurrentSettingStanding.Inside));
            }
        }

        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        public void SoundWindows_InAMixOfShortAndAboveTheBandChecksOnlyTheShortOnesCount(int timesShort, bool holdsUp)
        {
            var cells = ChecksOf(60, [.. Enumerable.Repeat(Check.FellShort, timesShort), .. Enumerable.Repeat(Check.AboveTheBand, ChecksPerWindow - timesShort)])
                .Concat(EveryOtherWindow(60, Check.InsideTheBand))
                .ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 14);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays.Contains(60), Is.EqualTo(holdsUp));
                Assert.That(soundWindow.CurrentSettingStanding, Is.EqualTo(CurrentSettingStanding.Inside));
            }
        }

        [Test]
        public void SoundWindows_TheRegionKeepsTheLadderOrderWhateverOrderTheChecksArriveIn()
        {
            var cells = Enumerable.Reverse(Ladder)
                .SelectMany(windowDays => ChecksOf(windowDays, [.. Enumerable.Repeat(windowDays == 60 ? Check.FellShort : Check.InsideTheBand, ChecksPerWindow)]))
                .ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 60);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays, Is.EqualTo(LadderWithoutSixty));
                Assert.That(soundWindow.Determination, Is.EqualTo(Determination.SomeWindowsSound));
                Assert.That(soundWindow.CurrentSettingStanding, Is.EqualTo(CurrentSettingStanding.Outside));
            }
        }

        [Test]
        public void SoundWindows_WhenNoWindowHoldsUpTheRegionIsEmptyAndNoWindowIsNamed()
        {
            var cells = Ladder.SelectMany(windowDays => ChecksOf(windowDays, [.. Enumerable.Repeat(Check.FellShort, ChecksPerWindow)])).ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 14);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays, Is.Empty);
                Assert.That(soundWindow.Determination, Is.EqualTo(Determination.NoWindowSound));
                Assert.That(soundWindow.CurrentSettingStanding, Is.EqualTo(CurrentSettingStanding.Outside));
            }
        }

        [Test]
        public void SoundWindows_WhenNoCheckCouldRunAnywhereThereIsNotEnoughEvidence()
        {
            var cells = Ladder.SelectMany(windowDays => ChecksOf(windowDays, [.. Enumerable.Repeat(Check.CouldNotRun, ChecksPerWindow)])).ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 14);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays, Is.Empty);
                Assert.That(soundWindow.Determination, Is.EqualTo(Determination.NotEnoughEvidence));
            }
        }

        private static IEnumerable<RealityCheckCellDto> EveryOtherWindow(int windowDays, Check check) =>
            Ladder
                .Where(otherWindowDays => otherWindowDays != windowDays)
                .SelectMany(otherWindowDays => ChecksOf(otherWindowDays, [.. Enumerable.Repeat(check, ChecksPerWindow)]));

        private static IEnumerable<RealityCheckCellDto> ChecksOf(int windowDays, Check[] checks) =>
            checks.Select(check => ACheck(windowDays, check));

        private static RealityCheckCellDto ACheck(int windowDays, Check check)
        {
            var actualCompleted = check switch
            {
                Check.FellShort => ValueAt95 - 1,
                Check.AboveTheBand => ValueAt50 + 1,
                _ => ValueAt95,
            };
            var day = new DateOnly(2026, 9, 1);
            var ran = check != Check.CouldNotRun;

            return new RealityCheckCellDto(
                14,
                windowDays,
                day,
                day,
                day,
                day,
                new RealityCheckSufficiencyDto(ran, ran ? SufficiencyReason.Sufficient : SufficiencyReason.TooFewActiveDays, 0),
                ran ? Forecast : null,
                ran ? actualCompleted : null,
                ran ? RealityCheckVerdictPolicy.Outcome(actualCompleted, Forecast) : null,
                ran
                    ? [.. Forecast.Select(level => new RealityCheckLevelOutcomeDto(
                        level.Probability, level.Value, RealityCheckVerdictPolicy.Held(actualCompleted, level.Value)))]
                    : null);
        }
    }
}
