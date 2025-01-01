using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.Update
{
    public class ForecastUpdateService : UpdateServiceBase<Project>, IForecastUpdateService
    {
        private readonly int trials;

        private readonly IRandomNumberService randomNumberService;

        public ForecastUpdateService(IRandomNumberService randomNumberService, ILogger<ForecastUpdateService> logger, IServiceScopeFactory serviceScopeFactory, IUpdateQueueService updateQueueService, int trials = 10000)
            : base(logger, serviceScopeFactory, updateQueueService, UpdateType.Forecasts)
        {
            this.trials = trials;
            this.randomNumberService = randomNumberService;
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            using (var scope = CreateServiceScope())
            {
                return GetServiceFromServiceScope<IAppSettingService>(scope).GetForecastRefreshSettings();
            }
        }

        protected override bool ShouldUpdateEntity(Project entity, RefreshSettings refreshSettings)
        {
            var minutesSinceLastUpdate = (DateTime.UtcNow - entity.ProjectUpdateTime).TotalMinutes;

            Logger.LogInformation("Last Refresh of Work Items for Project {ProjectName} was {minutesSinceLastUpdate} Minutes ago - Forecast will be rerun", entity.Name, minutesSinceLastUpdate);

            return true;
        }

        public HowManyForecast HowMany(Throughput throughput, int days)
        {
            Logger.LogInformation("Running Monte Carlo Forecast How Many for {Days} days.", days);

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

            Logger.LogInformation("Finished running Monte Carlo How Many for {Days} days.", days);

            return new HowManyForecast(simulationResults, days);
        }

        public async Task<WhenForecast> When(Team team, int remainingItems)
        {
            Logger.LogInformation("Running Monte Carlo Forecast When for Team {TeamName} and {RemainingItems} items.", team.Name, remainingItems);

            var fakeFeature = new Feature(team, remainingItems);
            await ForecastFeatures([fakeFeature]);

            Logger.LogInformation("Finished running Monte Carlo Forecast When for Team {TeamName} and {RemainingItems} items.", team.Name, remainingItems);

            return fakeFeature.Forecast;
        }

        public async Task UpdateForecastsForProject(int projectId)
        {
            using (var scope = CreateServiceScope())
            {
                await Update(projectId, CreateServiceScope().ServiceProvider);
            }
        }

        protected override async Task Update(int id, IServiceProvider serviceProvider)
        {
            var featureRepository = serviceProvider.GetRequiredService<IRepository<Feature>>();
            var featureHistoryService = serviceProvider.GetRequiredService<IFeatureHistoryService>();
            var projectRepository = serviceProvider.GetRequiredService<IRepository<Project>>();

            var project = projectRepository.GetById(id);
            if (project == null)
            {
                return;
            }

            await UpdateForecastsForProject(featureRepository, featureHistoryService, project);
        }

        private async Task UpdateForecastsForProject(IRepository<Feature> featureRepository, IFeatureHistoryService featureHistoryService, Project project)
        {
            Logger.LogInformation("Running Monte Carlo Forecast For Project {Project}", project.Name);

            await ForecastFeatures(project.Features);

            await featureRepository.Save();

            await ArchiveFeatures(featureHistoryService, project.Features);
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

        private static void UpdateFeatureForecasts(IEnumerable<Feature> features, List<SimulationResult> simulationResults)
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
                feature.SetFeatureForecasts(featureForecasts);
            }
        }

        private static WhenForecast CreateWhenForecastForSimulationResult(SimulationResult simulationResult)
        {
            return new WhenForecast(simulationResult);
        }

        private static List<SimulationResult> InitializeSimulationResults(IEnumerable<Feature> features)
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

        private static void ReduceRemainingWorkFromFeatureToUpdate(int simulatedDays, SimulationResult featureToUpdate)
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

        private static int RecalculateFeatureWIP(int featureWIP, int remainingItems)
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

        private static void AddSimulationResult(Dictionary<int, int> simulationResults, int simulationResult)
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

        private async Task ArchiveFeatures(IFeatureHistoryService featureHistoryService, IEnumerable<Feature> features)
        {
            foreach (var feature in features)
            {
                await featureHistoryService.ArchiveFeature(feature);
            }
        }
    }
}
