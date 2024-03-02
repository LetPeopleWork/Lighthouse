using CMFTAspNet.Models;
using CMFTAspNet.Models.Forecast;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation
{
    public class MonteCarloService : IMonteCarloService
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
                    simulatedThroughput += GetSimulatedThroughput(throughput);
                }

                AddSimulationResult(simulationResults, simulatedThroughput);
            });


            return new HowManyForecast(simulationResults, days);
        }

        public WhenForecast When(Team team, int remainingItems)
        {
            var fakeFeature = new Feature(team, remainingItems);
            ForecastFeatures([fakeFeature]);

            return fakeFeature.Forecast;
        }

        public void ForecastFeatures(IEnumerable<Feature> features)
        {
            var simulationResults = InitializeSimulationResults(features.OrderBy(f => f.Order));
            RunMonteCarloSimulation(simulationResults);
            UpdateFeatureForecasts(features, simulationResults);
        }

        private void RunMonteCarloSimulation(List<SimulationResult> simulationResults)
        {
            foreach (var simulationResultsByTeam in simulationResults.GroupBy(s => s.Team))
            {
                if (simulationResultsByTeam.Key.TotalThroughput <= 0)
                {
                    continue;
                }

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

        private void UpdateFeatureForecasts(IEnumerable<Feature> features, List<SimulationResult> simulationResults)
        {
            foreach (var feature in features)
            {
                var simulationResultForFeature = simulationResults
                    .Where(x => x.Feature == feature);

                var featureForecasts = simulationResultForFeature.Select(sr => CreateWhenForecastForSimulationResult(sr));
                feature.SetFeatureForecast(featureForecasts);
            }
        }

        private WhenForecast CreateWhenForecastForSimulationResult(SimulationResult simulationResult)
        {
            if (simulationResult.HasWorkRemaining)
            {
                return new WhenForecast(new Dictionary<int, int>(), 0);
            }

            return new WhenForecast(simulationResult.SimulationResults, simulationResult.InitialRemainingItems);
        }

        private List<SimulationResult> InitializeSimulationResults(IEnumerable<Feature> features)
        {
            var simulationResults = new List<SimulationResult>();

            foreach (var feature in features)
            {
                foreach (var remainingWork in feature.RemainingWork)
                {
                    simulationResults.Add(new SimulationResult(remainingWork.Team, feature, remainingWork.RemainingWorkItems));
                }
            }

            return simulationResults;
        }

        private void SimulateIndividualDayForFeatureForecast(Team team, IEnumerable<SimulationResult> simulationResults, int currentlySimulatedDay)
        {
            var simulatedThroughput = GetSimulatedThroughput(team.Throughput);

            for (var closedItems = 0; closedItems < simulatedThroughput && simulationResults.GetRemainingItems() > 0; closedItems++)
            {
                var featureToUpdate = GetFeatureToUpdate(team, simulationResults);
                ReduceRemainingWorkFromFeatureToUpdate(currentlySimulatedDay, featureToUpdate);
            }
        }

        private void ReduceRemainingWorkFromFeatureToUpdate(int simulatedDays, SimulationResult featureToUpdate)
        {
            featureToUpdate.RemainingItems -= 1;

            if (!featureToUpdate.HasWorkRemaining)
            {
                AddSimulationResult(featureToUpdate.SimulationResults, simulatedDays);
            }
        }

        private SimulationResult GetFeatureToUpdate(Team team, IEnumerable<SimulationResult> simulationResults)
        {
            var featuresRemaining = simulationResults.Where(x => x.HasWorkRemaining);
            var featureWorkedOnIndex = RecalculateFeatureWIP(team.FeatureWIP, featuresRemaining.Count());
            var featureWorkedOn = randomNumberService.GetRandomNumber(featureWorkedOnIndex);

            var itemToUpdate = featuresRemaining.ElementAt(featureWorkedOn);
            return itemToUpdate;
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

        private int GetSimulatedThroughput(Throughput throughput)
        {
            var randomDay = randomNumberService.GetRandomNumber(throughput.History - 1);
            return throughput.GetThroughputOnDay(randomDay);
        }
    }
}
