
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
            var fakeFeature = new Feature(throughput, remainingItems);
            ForecastFeatures(1, fakeFeature);

            return fakeFeature.Forecast;
        }

        public void ForecastFeatures(int featureWIP, params Feature[] features)
        {
            var simulationResults = InitializeSimulationResults(features);
            var throughput = features.First().Throughput;

            RunSimulations(() =>
            {
                var simulatedDays = 1;
                var remainingItems = InitializeRemainingItems(features);

                while (remainingItems.Sum(x => x.Value) > 0)
                {
                    SimulateIndividualDayForFeatureForecast(featureWIP, simulationResults, throughput, simulatedDays, remainingItems);

                    simulatedDays++;
                }

            });

            foreach (var feature in features)
            {
                var featureForecast = simulationResults[feature.Id];
                feature.SetFeatureForecast(new WhenForecast(featureForecast));
            }
        }

        private void SimulateIndividualDayForFeatureForecast(int featureWIP, Dictionary<Guid, Dictionary<int, int>> simulationResults, Throughput throughput, int simulatedDays, Dictionary<Guid, int> remainingItems)
        {
            var simulatedThroughput = GetRandomThroughput(throughput);

            for (var closedItems = 0; closedItems < simulatedThroughput; closedItems++)
            {
                var featureWorkedOnIndex = RecalculateFeatureWIP(featureWIP, remainingItems.Count);
                var featureWorkedOn = randomNumberService.GetRandomNumber(featureWorkedOnIndex);

                var featureToUpdateId = remainingItems.ElementAt(featureWorkedOn).Key;
                remainingItems[featureToUpdateId] -= 1;

                if (remainingItems[featureToUpdateId] == 0)
                {
                    var featureSpecificSimulationResult = simulationResults[featureToUpdateId];
                    AddSimulationResult(featureSpecificSimulationResult, simulatedDays);
                    remainingItems.Remove(featureToUpdateId);

                    if (remainingItems.Count == 0)
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

        private Dictionary<Guid, int> InitializeRemainingItems(Feature[] features)
        {
            var remainingItems = new Dictionary<Guid, int>();
            foreach (var feature in features)
            {
                remainingItems.Add(feature.Id, feature.RemainingItems);
            }

            return remainingItems;
        }

        private Dictionary<Guid, Dictionary<int, int>> InitializeSimulationResults(Feature[] features)
        {
            var simulationResults = new Dictionary<Guid, Dictionary<int, int>>();

            foreach (var feature in features)
            {
                simulationResults.Add(feature.Id, new Dictionary<int, int>());
            }

            return simulationResults;
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
