
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
            var workByTeamAndFeature = new Dictionary<Team, List<(Guid featureId, int remainingWork)>>();
            var simulationResults = new Dictionary<Team, Dictionary<Guid, Dictionary<int, int>>>();

            foreach (var feature in features)
            {
                foreach (var remainingWorkKey in feature.RemainingWork.Keys)
                {
                    if (!workByTeamAndFeature.ContainsKey(remainingWorkKey))
                    {
                        workByTeamAndFeature.Add(remainingWorkKey, new List<(Guid featureId, int remainingWork)>());
                        simulationResults.Add(remainingWorkKey, new Dictionary<Guid, Dictionary<int, int>>());
                    }

                    workByTeamAndFeature[remainingWorkKey].Add((feature.Id, feature.RemainingWork[remainingWorkKey]));
                    simulationResults[remainingWorkKey].Add(feature.Id, new Dictionary<int, int>());
                }
            }

            foreach (var featureByTeam in workByTeamAndFeature)
            {
                var team = featureByTeam.Key;
                var remainingWorkByTeam = featureByTeam.Value;

                RunSimulations(() =>
                {
                    var simulatedDays = 1;
                    var remainingItems = InitializeRemainingItems(remainingWorkByTeam);

                    while (remainingItems.Sum(x => x.Value) > 0)
                    {
                        SimulateIndividualDayForFeatureForecast(team, simulationResults[team], simulatedDays, remainingItems);

                        simulatedDays++;
                    }

                });
            }

            foreach (var feature in features)
            {
                var simulationResultForFeature = simulationResults
                    .SelectMany(teamDict => teamDict.Value)
                    .Where(guidDict => guidDict.Key == feature.Id)
                    .Select(pair => pair.Value);

                var featureForecasts = simulationResultForFeature.Select(result => new WhenForecast(result));
                feature.SetFeatureForecast(featureForecasts);
            }
        }

        private void SimulateIndividualDayForFeatureForecast(Team team, Dictionary<Guid, Dictionary<int, int>> simulationResults, int simulatedDays, Dictionary<Guid, int> remainingItems)
        {
            var simulatedThroughput = GetRandomThroughput(team.Throughput);

            for (var closedItems = 0; closedItems < simulatedThroughput; closedItems++)
            {
                var featureWorkedOnIndex = RecalculateFeatureWIP(team.FeatureWIP, remainingItems.Count);
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

        private Dictionary<Guid, int> InitializeRemainingItems(IEnumerable<(Guid featureId, int remainingWork)> featureForTeam)
        {
            var remainingItems = new Dictionary<Guid, int>();
            foreach (var feature in featureForTeam)
            {
                remainingItems.Add(feature.featureId, feature.remainingWork);
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
