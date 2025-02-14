using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Microsoft.VisualStudio.Services.Common;

namespace Lighthouse.Backend.Tests.Models.Forecast
{
    public class WhenForecastTest
    {
        [Test]
        [TestCase(8, 3)]
        [TestCase(30, 4)]
        [TestCase(50, 5)]
        [TestCase(70, 6)]
        [TestCase(85, 7)]
        [TestCase(95, 9)]
        public void GetPercentile_ReturnsCorrectValue(int percentile, int expectedResult)
        {
            var simulationResult = new SimulationResult(new Team(), new Feature(), 1);
            simulationResult.SimulationResults.Add(5, 3);
            simulationResult.SimulationResults.Add(4, 2);
            simulationResult.SimulationResults.Add(7, 1);
            simulationResult.SimulationResults.Add(3, 1);
            simulationResult.SimulationResults.Add(6, 2);
            simulationResult.SimulationResults.Add(9, 1);
            simulationResult.SimulationResults.Add(8, 0);

            var subject = new WhenForecast(simulationResult);

            var forecast = subject.GetProbability(percentile);

            Assert.That(forecast, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(3, 10)]
        [TestCase(4, 30)]
        [TestCase(5, 60)]
        [TestCase(6, 80)]
        [TestCase(7, 90)]
        [TestCase(12, 100)]
        public void GetLikelihood_ReturnsCorrectValue(int daysToTargetDate, int expecedLikelihood)
        {
            var simulationResult = new SimulationResult(new Team(), new Feature(), 1);
            simulationResult.SimulationResults.Add(4, 2);
            simulationResult.SimulationResults.Add(7, 1);
            simulationResult.SimulationResults.Add(5, 3);
            simulationResult.SimulationResults.Add(9, 1);
            simulationResult.SimulationResults.Add(3, 1);
            simulationResult.SimulationResults.Add(6, 2);

            var subject = new WhenForecast(simulationResult);

            var forecast = subject.GetLikelihood(daysToTargetDate);

            Assert.That(forecast, Is.EqualTo(expecedLikelihood));
        }
    }
}
