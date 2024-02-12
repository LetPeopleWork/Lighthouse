
using CMFTAspNet.Models;
using CMFTAspNet.Models.Forecast;

namespace CMFTAspNet.Services
{
    public class MonteCarloService
    {
        private readonly int trials;
        private readonly IRandomNumberService randomNumberService;

        public MonteCarloService(IRandomNumberService randomNumberService, int trials = 10000)
        {
            this.trials = trials;
            this.randomNumberService = randomNumberService;
        }

        public HowManyForecast HowMany(Throughput throughput, int days)
        {
            var simulationResults = RunSimulations(() =>
            {
                var simulatedThroughput = 0;
                for (var day = 0; day < days; day++)
                {
                    simulatedThroughput += GetRandomThroughput(throughput);
                }

                return simulatedThroughput;
            });

            return new HowManyForecast(simulationResults);
        }

        public WhenForecast When(Throughput throughput, int remainingItems)
        {
            var simulationResult = RunSimulations(() =>
            {
                var simulatedDays = 0;
                var simulatedRemainingItems = remainingItems;

                while (simulatedRemainingItems > 0)
                {
                    simulatedRemainingItems -= GetRandomThroughput(throughput);
                    simulatedDays++;
                }

                return simulatedDays;
            });

            return new WhenForecast(simulationResult);
        }

        private Dictionary<int, int> RunSimulations(Func<int> individualForecastFunction)
        {
            var simulationResults = new Dictionary<int, int>();

            for (var trial = 0; trial < trials; trial++)
            {
                var simulationResult = individualForecastFunction();

                AddSimulationResult(simulationResults, simulationResult);
            }

            return simulationResults;
        }

        private void AddSimulationResult(Dictionary<int, int> simulationResults, int simulationResult)
        {
            if (!simulationResults.ContainsKey(simulationResult))
            {
                simulationResults[simulationResult] = 0;
            }

            simulationResults[simulationResult]++;
        }

        private int GetRandomThroughput(Throughput throughput)
        {
            var randomDay = randomNumberService.GetRandomNumber(throughput.History - 1);
            return throughput.GetThroughputOnDay(randomDay);
        }
    }
}
