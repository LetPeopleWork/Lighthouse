using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class MonteCarloService : IMonteCarloService
    {
        private readonly int trials;
        private readonly IRandomNumberService randomNumberService;
        private readonly IRepository<Feature> featureRepository;
        private readonly ILogger<MonteCarloService> logger;

        public MonteCarloService(IRandomNumberService randomNumberService, IRepository<Feature> featureRepository, ILogger<MonteCarloService> logger, int trials = 10000)
        {
            this.trials = trials;
            this.randomNumberService = randomNumberService;
            this.featureRepository = featureRepository;
            this.logger = logger;
        }

        public HowManyForecast HowMany(Throughput throughput, int days)
        {
            logger.LogInformation("Running Monte Carlo Forecast How Many for {days} days.", days);

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

            logger.LogInformation("Finished running Monte Carlo How Many for {days} days.", days);

            return new HowManyForecast(simulationResults, days);
        }

        public async Task<WhenForecast> When(Team team, int remainingItems)
        {
            logger.LogInformation("Running Monte Carlo Forecast When for Team {TeamName} and {remainingItems} items.", team.Name, remainingItems);

            var fakeFeature = new Feature(team, remainingItems);
            await ForecastFeatures([fakeFeature]);

            logger.LogInformation("Finished running Monte Carlo Forecast When for Team {TeamName} and {remainingItems} items.", team.Name, remainingItems);

            return fakeFeature.Forecast;
        }

        public async Task ForecastAllFeatures()
        {
            logger.LogInformation("Running Monte Carlo Forecast For All Fetaures");

            var allFeatures = featureRepository.GetAll();

            await ForecastFeatures(allFeatures);

            logger.LogInformation("Finished running Monte Carlo Forecast For All Fetaures");

            await featureRepository.Save();
        }

        public async Task UpdateForecastsForProject(Project project)
        {
            logger.LogInformation("Running Monte Carlo Forecast For Project {project}", project.Name);

            await ForecastFeatures(project.Features);

            await featureRepository.Save();
        }

        private async Task ForecastFeatures(IEnumerable<Feature> features)
        {
            var simulationResults = InitializeSimulationResults(features);
            await RunMonteCarloSimulation(simulationResults);
            UpdateFeatureForecasts(features, simulationResults);
        }

        private async Task RunMonteCarloSimulation(List<SimulationResult> simulationResults)
        {
            var groupedSimulationResults = simulationResults.GroupBy(s => s.Team).Where(t => t.Key.TotalThroughput > 0).ToList();

            var tasks = groupedSimulationResults.Select(simulationResultsByTeam => Task.Run(() =>
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
            })).ToList();

            await Task.WhenAll(tasks);
        }

        private void UpdateFeatureForecasts(IEnumerable<Feature> features, List<SimulationResult> simulationResults)
        {
            foreach (var feature in features)
            {
                var simulationResultsForFeature = simulationResults
                    .Where(x => x.Feature == feature).ToList();

                if (simulationResultsForFeature.Count < 1)
                {
                    var simulationResult = new SimulationResult();
                    simulationResult.SimulationResults.Add(0, 0);
                    simulationResultsForFeature.Add(simulationResult);
                }

                var featureForecasts = simulationResultsForFeature.Select(CreateWhenForecastForSimulationResult);
                feature.SetFeatureForecast(featureForecasts);
            }
        }

        private WhenForecast CreateWhenForecastForSimulationResult(SimulationResult simulationResult)
        {
            return new WhenForecast(simulationResult.SimulationResults, simulationResult.InitialRemainingItems);
        }

        private List<SimulationResult> InitializeSimulationResults(IEnumerable<Feature> features)
        {
            var simulationResults = new List<SimulationResult>();

            foreach (var feature in features)
            {
                foreach (var featureWork in feature.FeatureWork)
                {
                    simulationResults.Add(new SimulationResult(featureWork.Team, feature, featureWork.RemainingWorkItems));
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
            var randomDay = randomNumberService.GetRandomNumber(throughput.History);
            return throughput.GetThroughputOnDay(randomDay);
        }
    }
}
