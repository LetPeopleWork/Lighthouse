using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.Update
{
    public class ForecastUpdateService : UpdateServiceBase<Project>, IForecastUpdateService
    {
        private readonly int trials;

        private readonly IRandomNumberService randomNumberService;

        public ForecastUpdateService(
            IRandomNumberService randomNumberService, ILogger<ForecastUpdateService> logger, IServiceScopeFactory serviceScopeFactory, IUpdateQueueService updateQueueService, int trials = 10_000)
            : base(logger, serviceScopeFactory, updateQueueService, UpdateType.Forecasts)
        {
            this.trials = trials;
            this.randomNumberService = randomNumberService;
        }

        protected override RefreshSettings GetRefreshSettings()
        {
            throw new NotSupportedException("Forecast Update Service does not support periodic refresh");
        }

        protected override bool ShouldUpdateEntity(Project entity, RefreshSettings refreshSettings)
        {
            throw new NotSupportedException("Forecast Update Service does not support periodic refresh");
        }

        public HowManyForecast HowMany(RunChartData throughput, int days)
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

            using (var scope = CreateServiceScope())
            {
                var teamMetricsService = CreateServiceScope().ServiceProvider.GetRequiredService<ITeamMetricsService>();
                var fakeFeature = new Feature(team, remainingItems);
                await ForecastFeatures([fakeFeature], teamMetricsService);

                Logger.LogInformation("Finished running Monte Carlo Forecast When for Team {TeamName} and {RemainingItems} items.", team.Name, remainingItems);

                return fakeFeature.Forecast;
            }
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
            var teamMetricsService = serviceProvider.GetRequiredService<ITeamMetricsService>();

            var project = projectRepository.GetById(id);
            if (project == null)
            {
                return;
            }

            await UpdateForecastsForTeams(featureRepository, featureHistoryService, teamMetricsService, project.Teams);
        }

        private async Task UpdateForecastsForTeams(IRepository<Feature> featureRepository, IFeatureHistoryService featureHistoryService, ITeamMetricsService teamMetricsService, IEnumerable<Team> teams)
        {
            Logger.LogInformation("Running Monte Carlo Forecast for all Features with involved of teams {Teams}", string.Join(',', teams.Select(t => t.Name)));

            var features = featureRepository.GetAll().Where(f => f.Teams.Any(t => teams.Contains(t))).ToList();

            Logger.LogInformation("Features with involved of those team are: {Features}", string.Join(",", features.Select(f => f.Name)));

            await ForecastFeatures(features, teamMetricsService);

            await featureRepository.Save();

            await ArchiveFeatures(featureHistoryService, features);
        }

        private async Task ForecastFeatures(IEnumerable<Feature> features, ITeamMetricsService teamMetricsService)
        {
            var throughpoutByTeam = InitializeThroughputPerTeam(features, teamMetricsService);

            var simulationResults = InitializeSimulationResults(features);
            await RunMonteCarloSimulation(simulationResults, throughpoutByTeam);
            UpdateFeatureForecasts(features, simulationResults);
        }

        private static Dictionary<int, RunChartData> InitializeThroughputPerTeam(IEnumerable<Feature> features, ITeamMetricsService teamMetricsService)
        {
            var teams = features.SelectMany(f => f.Teams).Distinct().ToList();
            var throughpoutByTeam = new Dictionary<int, RunChartData>();

            foreach (var team in teams)
            {
                var throughput = teamMetricsService.GetCurrentThroughputForTeam(team);

                if (throughput.Total > 0)
                {
                    throughpoutByTeam[team.Id] = throughput;
                }
            }

            return throughpoutByTeam;
        }

        private async Task RunMonteCarloSimulation(List<SimulationResult> simulationResults, Dictionary<int, RunChartData> throughputByTeam)
        {
            var groupedSimulationResults = simulationResults.GroupBy(s => s.Team).Where(g => throughputByTeam.ContainsKey(g.Key.Id)).ToList();

            var tasks = groupedSimulationResults.Select(simulationResultsByTeam => Task.Run(() =>
            {
                RunSimulations(() =>
                {
                    simulationResultsByTeam.ResetRemainingItems();

                    var simulatedDays = 1;

                    while (simulationResultsByTeam.GetRemainingItems() > 0)
                    {
                        SimulateIndividualDayForFeatureForecast(simulationResultsByTeam.Key, throughputByTeam[simulationResultsByTeam.Key.Id], simulationResultsByTeam.Select(x => x), simulatedDays);

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
                foreach (var featureWork in feature.FeatureWork.Where(fw => fw.RemainingWorkItems > 0))
                {
                    simulationResults.Add(new SimulationResult(featureWork.Team, feature, featureWork.RemainingWorkItems));
                }
            }

            return simulationResults;
        }

        private void SimulateIndividualDayForFeatureForecast(Team team, RunChartData throughput, IEnumerable<SimulationResult> simulationResults, int currentlySimulatedDay)
        {
            var simulatedThroughput = GetSimulatedThroughput(throughput);

            for (var closedItems = 0; closedItems < simulatedThroughput && simulationResults.GetRemainingItems() > 0; closedItems++)
            {
                var simulationResultOfFeatureToUpdate = GetSimulationResultsOfFeatureToUpdate(team, simulationResults);
                ReduceRemainingWorkFromFeatureToUpdate(currentlySimulatedDay, simulationResultOfFeatureToUpdate);
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

        private SimulationResult GetSimulationResultsOfFeatureToUpdate(Team team, IEnumerable<SimulationResult> simulationResults)
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

        private int GetSimulatedThroughput(RunChartData throughput)
        {
            var randomDay = randomNumberService.GetRandomNumber(throughput.History);
            return throughput.GetValueOnDay(randomDay);
        }

        private static async Task ArchiveFeatures(IFeatureHistoryService featureHistoryService, IEnumerable<Feature> features)
        {
            foreach (var feature in features)
            {
                await featureHistoryService.ArchiveFeature(feature);
            }
        }
    }
}
