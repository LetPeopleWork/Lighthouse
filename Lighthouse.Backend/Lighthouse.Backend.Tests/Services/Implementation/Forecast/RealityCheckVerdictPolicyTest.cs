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
    }
}
