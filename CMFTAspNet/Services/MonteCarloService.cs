
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
            var simulationResults = new Dictionary<int, int>();

            RunSimulations(() =>
            {
                var simulatedThroughput = 0;
                for (var day = 0; day < days; day++)
                {
                    simulatedThroughput += GetRandomThroughput(throughput);
                }

                AddSimulationResult(simulationResults, simulatedThroughput);
            });


            return new HowManyForecast(simulationResults);
        }

        public WhenForecast When(Throughput throughput, int remainingItems)
        {
            var fakeFeature = new Feature(new Team(1, throughput), remainingItems);
            ForecastFeatures(fakeFeature);

            return fakeFeature.Forecast;
        }

        public void ForecastFeatures(params Feature[] features)
        {
            var simulationResults = InitializeSimulationResults(features);
            RunMonteCarloSimulation(simulationResults);
            SetFeatureForecasts(features, simulationResults);
        }

        private void RunMonteCarloSimulation(List<SimulationResult> simulationResults)
        {
            foreach (var simulationResultsByTeam in simulationResults.GroupBy(s => s.Team))
            {
                RunSimulations(() =>
                {
                    simulationResultsByTeam.ResetRemainingItems();
                    var simulatedDays = 1;

                    while (simulationResultsByTeam.GetRemainingItems() > 0)
                    {
                        SimulateIndividualDayForFeatureForecast(simulationResultsByTeam.Key, simulationResultsByTeam.Select(x => x), simulatedDays);

                        simulatedDays++;
                    }
                });
            }
        }

        private void SetFeatureForecasts(Feature[] features, List<SimulationResult> simulationResults)
        {
            foreach (var feature in features)
            {
                var simulationResultForFeature = simulationResults
                    .Where(x => x.Feature.Id == feature.Id);

                var featureForecasts = simulationResultForFeature.Select(result => new WhenForecast(result.SimulationResults));
                feature.SetFeatureForecast(featureForecasts);
            }
        }

        private List<SimulationResult> InitializeSimulationResults(Feature[] features)
        {
            var simulationResults = new List<SimulationResult>();

            foreach (var feature in features)
            {
                foreach (var remainingWorkKey in feature.RemainingWork.Keys)
                {
                    simulationResults.Add(new SimulationResult(remainingWorkKey, feature, feature.RemainingWork[remainingWorkKey]));
                }
            }

            return simulationResults;
        }

        private void SimulateIndividualDayForFeatureForecast(Team team, IEnumerable<SimulationResult> simulationResults, int simulatedDays)
        {
            var remainingItems = simulationResults.Sum(x => x.RemainingItems);
            var simulatedThroughput = GetRandomThroughput(team.Throughput);

            for (var closedItems = 0; closedItems < simulatedThroughput; closedItems++)
            {
                var featuresRemaining = simulationResults.Where(x => x.HasWorkRemaining);
                var featureWorkedOnIndex = RecalculateFeatureWIP(team.FeatureWIP, featuresRemaining.Count());
                var featureWorkedOn = randomNumberService.GetRandomNumber(featureWorkedOnIndex);

                var itemToUpdate = featuresRemaining.ElementAt(featureWorkedOn);
                itemToUpdate.RemainingItems -= 1;

                if (itemToUpdate.RemainingItems == 0)
                {                    
                    AddSimulationResult(itemToUpdate.SimulationResults, simulatedDays);

                    if (simulationResults.GetRemainingItems() == 0)
                    {
                        break;
                    }
                }
            }
        }

        private int RecalculateFeatureWIP(int featureWIP, int remainingItems)
        {
            var featureWorkedOnIndex = featureWIP;
            if (remainingItems < featureWIP)
            {
                featureWorkedOnIndex = remainingItems;
            }

            return featureWorkedOnIndex;
        }

        private void RunSimulations(Action individualSimulation)
        {
            for (var trial = 0; trial < trials; trial++)
            {
                individualSimulation();
            }
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
