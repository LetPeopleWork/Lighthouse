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

        private static readonly int[] OnlyThirty = [30];

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

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 30, usesFixedDates: false);

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

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 30, usesFixedDates: false);

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

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 14, usesFixedDates: false);

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

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 60, usesFixedDates: false);

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

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 14, usesFixedDates: false);

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

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 14, usesFixedDates: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays, Is.Empty);
                Assert.That(soundWindow.UnevaluatedWindowDays, Is.EqualTo(Ladder));
                Assert.That(soundWindow.Determination, Is.EqualTo(Determination.NotEnoughEvidence));
            }
        }

        [TestCase(2, 0, true)]
        [TestCase(2, 1, false)]
        [TestCase(1, 0, true)]
        [TestCase(1, 1, false)]
        public void SoundWindows_AWindowOnlySomeOfWhoseChecksRanIsJudgedOnTheOnesThatDid(int checksThatRan, int ofThemFellShort, bool holdsUp)
        {
            Check[] checks =
            [
                .. Enumerable.Repeat(Check.FellShort, ofThemFellShort),
                .. Enumerable.Repeat(Check.InsideTheBand, checksThatRan - ofThemFellShort),
                .. Enumerable.Repeat(Check.CouldNotRun, ChecksPerWindow - checksThatRan),
            ];
            var cells = ChecksOf(90, checks).Concat(EveryOtherWindow(90, Check.InsideTheBand)).ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 90, usesFixedDates: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays.Contains(90), Is.EqualTo(holdsUp));
                Assert.That(soundWindow.UnevaluatedWindowDays, Is.Empty);
                Assert.That(soundWindow.Determination, Is.EqualTo(holdsUp ? Determination.AllWindowsAlike : Determination.SomeWindowsSound));
            }
        }

        [TestCase(14)]
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(90)]
        public void SoundWindows_AWindowNoneOfWhoseChecksRanIsAGapNeverAPass(int windowDays)
        {
            var cells = ChecksOf(windowDays, [.. Enumerable.Repeat(Check.CouldNotRun, ChecksPerWindow)])
                .Concat(EveryOtherWindow(windowDays, Check.InsideTheBand))
                .ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, windowDays, usesFixedDates: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays, Is.EqualTo(Ladder.Where(days => days != windowDays)));
                Assert.That(soundWindow.UnevaluatedWindowDays, Is.EqualTo(new[] { windowDays }));
                Assert.That(soundWindow.Determination, Is.EqualTo(Determination.SomeWindowsSound));
            }
        }

        [TestCase(14, true)]
        [TestCase(30, false)]
        [TestCase(60, true)]
        [TestCase(90, false)]
        public void SoundWindows_WhenTheTeamsOwnWindowCouldNotBeEvaluatedItsStandingIsNotDeterminedAndItWasStillTested(int currentSettingDays, bool everyOtherWindowHoldsUp)
        {
            var cells = ChecksOf(currentSettingDays, [.. Enumerable.Repeat(Check.CouldNotRun, ChecksPerWindow)])
                .Concat(EveryOtherWindow(currentSettingDays, everyOtherWindowHoldsUp ? Check.InsideTheBand : Check.FellShort))
                .ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, currentSettingDays, usesFixedDates: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.CurrentSettingStanding, Is.EqualTo(CurrentSettingStanding.NotDetermined));
                Assert.That(soundWindow.CurrentSettingWasTested, Is.True);
                Assert.That(soundWindow.CurrentSettingNotTestedReason, Is.Null);
            }
        }

        [TestCase(14)]
        [TestCase(90)]
        public void SoundWindows_WhenNoCheckCouldRunAnywhereTheTeamsOwnWindowIsNotDetermined(int currentSettingDays)
        {
            var cells = Ladder.SelectMany(windowDays => ChecksOf(windowDays, [.. Enumerable.Repeat(Check.CouldNotRun, ChecksPerWindow)])).ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, currentSettingDays, usesFixedDates: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.CurrentSettingStanding, Is.EqualTo(CurrentSettingStanding.NotDetermined));
                Assert.That(soundWindow.CurrentSettingWasTested, Is.True);
                Assert.That(soundWindow.CurrentSettingNotTestedReason, Is.Null);
            }
        }

        [TestCase(true, CurrentSettingStanding.Inside)]
        [TestCase(false, CurrentSettingStanding.Outside)]
        public void SoundWindows_AnotherWindowThatCouldNotBeEvaluatedLeavesTheTeamsOwnStandingDetermined(bool ownWindowHoldsUp, CurrentSettingStanding expected)
        {
            var cells = ChecksOf(30, [.. Enumerable.Repeat(ownWindowHoldsUp ? Check.InsideTheBand : Check.FellShort, ChecksPerWindow)])
                .Concat(ChecksOf(60, [.. Enumerable.Repeat(Check.CouldNotRun, ChecksPerWindow)]))
                .Concat(Ladder.Where(days => days != 30 && days != 60).SelectMany(days => ChecksOf(days, [.. Enumerable.Repeat(Check.InsideTheBand, ChecksPerWindow)])))
                .ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 30, usesFixedDates: false);

            Assert.That(soundWindow.CurrentSettingStanding, Is.EqualTo(expected));
        }

        [Test]
        public void SoundWindows_AWindowThatCouldNotBeCheckedBesideWindowsThatFellShortIsNoWindowSound()
        {
            var cells = ChecksOf(30, [.. Enumerable.Repeat(Check.CouldNotRun, ChecksPerWindow)])
                .Concat(EveryOtherWindow(30, Check.FellShort))
                .ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 30, usesFixedDates: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays, Is.Empty);
                Assert.That(soundWindow.UnevaluatedWindowDays, Is.EqualTo(OnlyThirty));
                Assert.That(soundWindow.Determination, Is.EqualTo(Determination.NoWindowSound));
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void SoundWindows_AWindowWithACheckThatFellShortWasEvaluatedHoweverManyOfItsChecksCouldNotRun(int timesShort)
        {
            var cells = ChecksOf(60, [.. Enumerable.Repeat(Check.FellShort, timesShort), .. Enumerable.Repeat(Check.CouldNotRun, ChecksPerWindow - timesShort)])
                .Concat(EveryOtherWindow(60, Check.InsideTheBand))
                .ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 60, usesFixedDates: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.SoundWindowDays, Is.EqualTo(LadderWithoutSixty));
                Assert.That(soundWindow.UnevaluatedWindowDays, Is.Empty);
                Assert.That(soundWindow.Determination, Is.EqualTo(Determination.SomeWindowsSound));
            }
        }

        [Test]
        public void SoundWindows_EveryMixOfWindowStatesIsDeterminedInOrderAndNoWindowIsBothSoundAndUnevaluated()
        {
            WindowFate[] fates = [WindowFate.HoldsUp, WindowFate.FallsShort, WindowFate.CouldNotRun];
            var everyMix = Ladder.Aggregate(
                new[] { new Dictionary<int, WindowFate>() }.AsEnumerable(),
                (mixes, windowDays) => mixes.SelectMany(mix => fates.Select(fate => new Dictionary<int, WindowFate>(mix) { [windowDays] = fate })));

            using (Assert.EnterMultipleScope())
            {
                foreach (var mix in everyMix)
                {
                    var cells = Ladder.SelectMany(windowDays => ChecksOf(windowDays, [.. Enumerable.Repeat(CheckFor(mix[windowDays]), ChecksPerWindow)])).ToList();

                    var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, 14, usesFixedDates: false);

                    Assert.That(soundWindow.SoundWindowDays, Is.EqualTo(Ladder.Where(days => mix[days] == WindowFate.HoldsUp)));
                    Assert.That(soundWindow.UnevaluatedWindowDays, Is.EqualTo(Ladder.Where(days => mix[days] == WindowFate.CouldNotRun)));
                    Assert.That(soundWindow.SoundWindowDays.Intersect(soundWindow.UnevaluatedWindowDays), Is.Empty);
                    Assert.That(soundWindow.Determination, Is.EqualTo(ExpectedDetermination(mix.Values)));
                }
            }
        }

        [TestCase(45, true, NotTestedReason.UsesFixedDates)]
        [TestCase(30, true, NotTestedReason.UsesFixedDates)]
        [TestCase(0, true, NotTestedReason.UsesFixedDates)]
        [TestCase(-7, true, NotTestedReason.UsesFixedDates)]
        [TestCase(0, false, NotTestedReason.NotAPositiveLength)]
        [TestCase(-7, false, NotTestedReason.NotAPositiveLength)]
        public void SoundWindows_ASettingThatIsNotARollingWindowIsNotTestedAndSaysWhyWhateverTheWindowsShow(int currentSettingDays, bool usesFixedDates, NotTestedReason expectedReason)
        {
            Check[] everyOutcome = [Check.InsideTheBand, Check.FellShort, Check.CouldNotRun];

            using (Assert.EnterMultipleScope())
            {
                foreach (var outcome in everyOutcome)
                {
                    var cells = Ladder.SelectMany(windowDays => ChecksOf(windowDays, [.. Enumerable.Repeat(outcome, ChecksPerWindow)])).ToList();

                    var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, currentSettingDays, usesFixedDates);

                    Assert.That(soundWindow.CurrentSettingWasTested, Is.False);
                    Assert.That(soundWindow.CurrentSettingStanding, Is.EqualTo(CurrentSettingStanding.NotTested));
                    Assert.That(soundWindow.CurrentSettingNotTestedReason, Is.EqualTo(expectedReason));
                    Assert.That(soundWindow.CurrentSettingDays, Is.EqualTo(currentSettingDays));
                }
            }
        }

        [TestCase(1)]
        [TestCase(14)]
        [TestCase(45)]
        public void SoundWindows_ARollingWindowOfAnyPositiveLengthIsTestedAndGivesNoReason(int currentSettingDays)
        {
            var cells = Ladder.SelectMany(windowDays => ChecksOf(windowDays, [.. Enumerable.Repeat(Check.InsideTheBand, ChecksPerWindow)])).ToList();

            var soundWindow = RealityCheckVerdictPolicy.SoundWindows(Ladder, cells, currentSettingDays, usesFixedDates: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(soundWindow.CurrentSettingWasTested, Is.True);
                Assert.That(soundWindow.CurrentSettingStanding, Is.Not.EqualTo(CurrentSettingStanding.NotTested));
                Assert.That(soundWindow.CurrentSettingNotTestedReason, Is.Null);
            }
        }

        private enum WindowFate
        {
            HoldsUp,
            FallsShort,
            CouldNotRun,
        }

        private static Check CheckFor(WindowFate fate) => fate switch
        {
            WindowFate.HoldsUp => Check.InsideTheBand,
            WindowFate.FallsShort => Check.FellShort,
            _ => Check.CouldNotRun,
        };

        /// <summary>
        /// The answer written out from how many windows held up and how many could not be checked at all,
        /// rather than worked out the way the policy works it out.
        /// </summary>
        private static Determination ExpectedDetermination(ICollection<WindowFate> fates)
        {
            var windows = fates.Count;
            var heldUp = fates.Count(fate => fate == WindowFate.HoldsUp);
            var couldNotBeChecked = fates.Count(fate => fate == WindowFate.CouldNotRun);

            return (heldUp, couldNotBeChecked) switch
            {
                (0, var notChecked) when notChecked == windows => Determination.NotEnoughEvidence,
                (var held, 0) when held == windows => Determination.AllWindowsAlike,
                (0, _) => Determination.NoWindowSound,
                _ => Determination.SomeWindowsSound,
            };
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
