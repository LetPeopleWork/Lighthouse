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
    }
}
