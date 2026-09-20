using System.Collections.Concurrent;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public class ForecastService(
        IRandomNumberService randomNumberService,
        ILogger<ForecastService> logger,
        ITeamMetricsService teamMetricsService,
        IRepository<Feature> featureRepository,
        IWhatTheForecastWaitsFor whatTheForecastWaitsFor,
        IDrawStreamFactory drawStreamFactory,
        ForecastSimulationLimits limits)
        : IForecastService
    {
        public HowManyForecast PredictWorkItemCreation(Team team, string[] workItemTypes, DateTime startDate, DateTime endDate, int daysToForecast)
        {
            logger.LogDebug("Predicting Work Item Creation for team {TeamName} in the next {Days} days for Work Items {WorkItems} based on the time from {Start} to {End}",
                team.Name, daysToForecast, string.Join(", ", workItemTypes), startDate, endDate);

            var createdItemsRunChart = teamMetricsService.GetCreatedItemsForTeam(team, workItemTypes, startDate, endDate);

            return HowMany(createdItemsRunChart, daysToForecast);
        }

        public HowManyForecast HowMany(RunChartData throughput, int days)
        {
            logger.LogDebug("Running Monte Carlo Forecast How Many for {Days} days.", days);

            var simulationResults = new Dictionary<int, int>();

            for (var trial = 0; trial < limits.Trials; trial++)
            {
                var simulatedThroughput = 0;
                for (var day = 0; day < days; day++)
                {
                    simulatedThroughput += GetSimulatedThroughput(throughput);
                }

                simulationResults[simulatedThroughput] = simulationResults.GetValueOrDefault(simulatedThroughput) + 1;
            }

            logger.LogDebug("Finished running Monte Carlo How Many for {Days} days.", days);

            return new HowManyForecast(simulationResults, days);
        }

        public async Task<WhenForecast> When(Team team, int remainingItems, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
        {
            logger.LogDebug("Running Monte Carlo Forecast When for Team {TeamName} and {RemainingItems} items (mode {Mode}).", team.Name, remainingItems, mode);

            var fakeFeature = new Feature(team, remainingItems);
            await ForecastFeatures([fakeFeature], mode);

            logger.LogDebug("Finished running Monte Carlo Forecast When for Team {TeamName} and {RemainingItems} items.", team.Name, remainingItems);

            return fakeFeature.Forecast;
        }

        public async Task UpdateForecastsForPortfolio(Portfolio portfolio)
        {
            await UpdateForecastsForTeams(portfolio.Teams);
        }

        private async Task UpdateForecastsForTeams(IEnumerable<Team> teams)
        {
            logger.LogInformation("Running Monte Carlo Forecast for all Features with involved of teams {Teams}", string.Join(',', teams.Select(t => t.Name)));

            var features = featureRepository.GetAll().Where(f => f.Teams.Any(teams.Contains)).ToList();

            logger.LogDebug("Features that are being forecasted: {Features}", string.Join(",", features.Select(f => f.Name)));

            await ForecastFeatures(features);

            await featureRepository.Save();
        }

        private async Task ForecastFeatures(IEnumerable<Feature> features, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
        {
            var featuresToForecast = features as IReadOnlyCollection<Feature> ?? features.ToList();
            var throughputByTeam = InitializeThroughputPerTeam(featuresToForecast, mode, out var chipStatusByTeam);

            var simulationResults = InitializeSimulationResults(featuresToForecast);
            var waits = whatTheForecastWaitsFor.Of(featuresToForecast);
            var draws = drawStreamFactory.ForOneRun();

            var startForecasts = await Task.Run(() => RunMonteCarloSimulation(simulationResults, throughputByTeam, waits, draws));

            UpdateFeatureForecasts(featuresToForecast, simulationResults, chipStatusByTeam, startForecasts);
        }

        private Dictionary<int, RunChartData> InitializeThroughputPerTeam(IEnumerable<Feature> features, ThroughputFilterMode mode, out Dictionary<int, ForecastThroughputStatus> chipStatusByTeam)
        {
            var teams = features.SelectMany(f => f.Teams).Distinct().ToList();
            var throughputByTeam = new Dictionary<int, RunChartData>();
            chipStatusByTeam = new Dictionary<int, ForecastThroughputStatus>();

            foreach (var team in teams)
            {
                var status = teamMetricsService.GetForecastThroughputStatus(team, mode);
                chipStatusByTeam[team.Id] = status;

                if (status.Throughput.Total > 0)
                {
                    throughputByTeam[team.Id] = status.Throughput;
                }
            }

            return throughputByTeam;
        }

        /// <summary>
        /// Every Team is simulated on one shared day counter, one run at a time. Each Team still draws its
        /// own delivery from its own measured history and works on its own rows; what they share is time,
        /// which is the only thing that makes "has the Feature waited on finished yet?" a question with an
        /// answer at all.
        /// </summary>
        private Dictionary<Feature, List<StartForecast>> RunMonteCarloSimulation(
            List<SimulationResult> simulationResults,
            Dictionary<int, RunChartData> throughputByTeam,
            ForecastWaits waits,
            IDrawStream draws)
        {
            var plan = ForecastRunPlan.For(simulationResults, throughputByTeam, waits);

            if (plan.RowCount == 0)
            {
                return [];
            }

            var oneRun = new SimulatedRun(plan, draws, limits.MostDaysOneSimulatedRunMayCover);
            var workersThatRanThem = new ConcurrentBag<OneWorkersShareOfTheRuns>();

            Parallel.For(
                0,
                limits.Trials,
                () => new OneWorkersShareOfTheRuns(plan),
                (trial, _, share) =>
                {
                    share.CarryOut(oneRun, trial);
                    return share;
                },
                workersThatRanThem.Add);

            var shares = workersThatRanThem.ToList();

            RecordTheDaysEachRowFinishedOn(plan, shares);

            var whatWentWrong = WhatTheRunsCouldNotFinish.AllOf(shares.Select(share => share.WhatWentWrong));

            ReportTheRunsThatCouldNotFinish(whatWentWrong);
            ReportTheRunsThatRanOutOfDays(whatWentWrong, draws);

            return TheDaysWorkBeganOn(plan, shares);
        }

        /// <summary>
        /// The start distributions, at both grains, added up once after every run is over - the same pass
        /// and the same arithmetic the finished days get.
        ///
        /// The Feature-grain row is the one the run observed rather than one computed from the per-team
        /// rows beside it. Computing it would mean combining those marginals, which can only be done by
        /// assuming the teams move independently; a Feature whose teams both wait on the same upstream
        /// work is precisely where that is false, and is also the case anyone looks at a start date for.
        /// </summary>
        private static Dictionary<Feature, List<StartForecast>> TheDaysWorkBeganOn(ForecastRunPlan plan, List<OneWorkersShareOfTheRuns> shares)
        {
            var byRow = ADayCountPer(plan.RowCount);
            var byFeature = ADayCountPer(plan.FeatureCount);

            foreach (var recorded in shares.Select(share => share.Completions))
            {
                recorded.AddRowStartsInto(byRow);
                recorded.AddFeatureStartsInto(byFeature);
            }

            var startForecasts = new Dictionary<Feature, List<StartForecast>>();

            for (var row = 0; row < plan.RowCount; row++)
            {
                if (plan.RowAt(row).Feature is { } feature)
                {
                    TheStartForecastsOf(startForecasts, feature).Add(new StartForecast(InDayOrder(byRow[row]), plan.RowAt(row).Team));
                }
            }

            for (var featureIndex = 0; featureIndex < plan.FeatureCount; featureIndex++)
            {
                var feature = plan.FeatureAt(featureIndex);
                TheStartForecastsOf(startForecasts, feature).Add(new StartForecast(InDayOrder(byFeature[featureIndex]), null));
            }

            return startForecasts;
        }

        private static List<StartForecast> TheStartForecastsOf(Dictionary<Feature, List<StartForecast>> startForecasts, Feature feature)
        {
            if (!startForecasts.TryGetValue(feature, out var ofThisFeature))
            {
                ofThisFeature = [];
                startForecasts[feature] = ofThisFeature;
            }

            return ofThisFeature;
        }

        private static Dictionary<int, int> InDayOrder(Dictionary<int, int> days)
            => days.OrderBy(day => day.Key).ToDictionary(day => day.Key, day => day.Value);

        private static Dictionary<int, int>[] ADayCountPer(int howMany)
        {
            var counts = new Dictionary<int, int>[howMany];

            for (var index = 0; index < howMany; index++)
            {
                counts[index] = [];
            }

            return counts;
        }

        /// <summary>
        /// Added up once, after every run is over. Counts add up the same whichever worker's share is taken
        /// first, and the days are written out in order, so how the work happened to be split between workers
        /// leaves no trace in what comes out.
        /// </summary>
        private static void RecordTheDaysEachRowFinishedOn(ForecastRunPlan plan, List<OneWorkersShareOfTheRuns> shares)
        {
            var total = ADayCountPer(plan.RowCount);

            foreach (var share in shares)
            {
                share.Completions.AddInto(total);
            }

            for (var row = 0; row < plan.RowCount; row++)
            {
                foreach (var day in total[row].OrderBy(finished => finished.Key))
                {
                    plan.RowAt(row).SimulationResults[day.Key] = day.Value;
                }
            }
        }

        /// <summary>
        /// A run reaches an end because the waits it was handed lead somewhere: whatever still has work has
        /// something at the front of it that is waiting for nothing. That is a property of the decision that
        /// produced them, not of this loop, and if it is ever broken this loop would otherwise spend a
        /// background thread forever with nothing anywhere saying why. So it stops, and says so.
        /// </summary>
        private void ReportTheRunsThatCouldNotFinish(WhatTheRunsCouldNotFinish whatWentWrong)
        {
            if (whatWentWrong.RunsGivenUpOn == 0)
            {
                return;
            }

            logger.LogError(
                "Abandoned {Trials} simulated runs for teams {Teams}: every Feature with work left was waiting on one that had not finished, which can only happen if the Features are waiting on each other in a circle. The dates for those Features are not a forecast",
                whatWentWrong.RunsGivenUpOn,
                string.Join(", ", whatWentWrong.TeamsCaughtInACircle));
        }

        /// <summary>
        /// Reported apart from the runs that simply had nothing left to start, and named down to the single
        /// run and the number its draws came from, because those two together set that exact run going again
        /// on its own. No data can put a run here: it means the way runs end is itself wrong.
        /// </summary>
        private void ReportTheRunsThatRanOutOfDays(WhatTheRunsCouldNotFinish whatWentWrong, IDrawStream draws)
        {
            if (whatWentWrong.RunsThatRanOutOfDays == 0)
            {
                return;
            }

            logger.LogError(
                "Gave up on {Trials} simulated runs that passed {Days} simulated days without finishing, for teams {Teams}. Set the first of them going again on its own with starting number {StartingNumber} and run {Trial}. Those teams have no dates from this forecast",
                whatWentWrong.RunsThatRanOutOfDays,
                limits.MostDaysOneSimulatedRunMayCover,
                string.Join(", ", whatWentWrong.TeamsThatRanOutOfDays),
                draws.StartingNumber,
                whatWentWrong.FirstRunThatRanOutOfDays);
        }

        private static void UpdateFeatureForecasts(
            IEnumerable<Feature> features,
            List<SimulationResult> simulationResults,
            Dictionary<int, ForecastThroughputStatus> chipStatusByTeam,
            Dictionary<Feature, List<StartForecast>> startForecasts)
        {
            foreach (var feature in features)
            {
                // Every Feature being forecast has its start distributions rewritten, including the ones
                // the run admitted no row for. Left alone they would go on reporting the last run that
                // did have them, which is a date from a world that no longer exists.
                feature.SetStartForecasts(startForecasts.TryGetValue(feature, out var ofThisFeature) ? ofThisFeature : []);

                var simulationResultsForFeature = simulationResults
                    .Where(x => x.Feature == feature).ToList();

                if (simulationResultsForFeature.Count < 1)
                {
                    var simulationResult = new SimulationResult();
                    simulationResult.SimulationResults.Add(0, 0);
                    simulationResultsForFeature.Add(simulationResult);
                }

                var featureForecasts = simulationResultsForFeature.Select(r => CreateWhenForecastForSimulationResult(r, chipStatusByTeam));
                feature.SetFeatureForecasts(featureForecasts);
            }
        }

        private static WhenForecast CreateWhenForecastForSimulationResult(SimulationResult simulationResult, Dictionary<int, ForecastThroughputStatus> chipStatusByTeam)
        {
            var forecast = new WhenForecast(simulationResult);
            if (simulationResult.Team is { } team && chipStatusByTeam.TryGetValue(team.Id, out var status))
            {
                forecast.FilterApplied = status.FilterApplied;
                forecast.ExcludedSummary = status.ExcludedSummary;
                forecast.HasSufficientData = status.HasSufficientData;
            }
            return forecast;
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

        private int GetSimulatedThroughput(RunChartData throughput)
        {
            var randomDay = randomNumberService.GetRandomNumber(throughput.History);
            return throughput.GetCountOnDay(randomDay);
        }
    }
}
