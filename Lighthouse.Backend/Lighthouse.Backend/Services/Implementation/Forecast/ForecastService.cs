using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public class ForecastService(
        IRandomNumberService randomNumberService,
        ILogger<ForecastService> logger,
        ITeamMetricsService teamMetricsService,
        IRepository<Feature> featureRepository,
        IWhatTheForecastWaitsFor whatTheForecastWaitsFor)
        : IForecastService
    {
        private const int Trials = 10_000;

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

            RunSimulations(() =>
            {
                var simulatedThroughput = 0;
                for (var day = 0; day < days; day++)
                {
                    simulatedThroughput += GetSimulatedThroughput(throughput);
                }

                AddSimulationResult(simulationResults, simulatedThroughput);
            });

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
            await RunMonteCarloSimulation(simulationResults, throughputByTeam, whatTheForecastWaitsFor.Of(featuresToForecast));
            UpdateFeatureForecasts(featuresToForecast, simulationResults, chipStatusByTeam);
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

        private async Task RunMonteCarloSimulation(
            List<SimulationResult> simulationResults,
            Dictionary<int, RunChartData> throughputByTeam,
            ForecastWaits waits)
        {
            var groupedSimulationResults = simulationResults.GroupBy(s => s.Team).Where(g => throughputByTeam.ContainsKey(g.Key.Id)).ToList();

            var teamsThatCouldNotFinish = new ConcurrentDictionary<string, int>();

            var tasks = groupedSimulationResults.Select(simulationResultsByTeam => Task.Run(() =>
            {
                var whatIsWaitingForWhat = new WhatEachFeatureIsWaitingFor(simulationResultsByTeam, waits);
                var trialsAbandoned = 0;

                RunSimulations(() =>
                {
                    simulationResultsByTeam.ResetRemainingItems();

                    var simulatedDays = 1;

                    while (simulationResultsByTeam.GetRemainingItems() > 0)
                    {
                        var anythingCouldBeStarted = SimulateIndividualDayForFeatureForecast(simulationResultsByTeam.Key, throughputByTeam[simulationResultsByTeam.Key.Id], simulationResultsByTeam.Select(x => x), simulatedDays, whatIsWaitingForWhat);

                        if (!anythingCouldBeStarted)
                        {
                            trialsAbandoned++;
                            break;
                        }

                        simulatedDays++;
                    }
                });

                if (trialsAbandoned > 0)
                {
                    teamsThatCouldNotFinish[simulationResultsByTeam.Key.Name] = trialsAbandoned;
                }
            })).ToList();

            await Task.WhenAll(tasks);

            ReportTheRunsThatCouldNotFinish(teamsThatCouldNotFinish);
        }

        /// <summary>
        /// A run reaches an end because the waits it was handed lead somewhere: whatever still has work has
        /// something at the front of it that is waiting for nothing. That is a property of the decision that
        /// produced them, not of this loop, and if it is ever broken this loop would otherwise spend a
        /// background thread forever with nothing anywhere saying why. So it stops, and says so.
        /// </summary>
        private void ReportTheRunsThatCouldNotFinish(ConcurrentDictionary<string, int> teamsThatCouldNotFinish)
        {
            if (teamsThatCouldNotFinish.IsEmpty)
            {
                return;
            }

            logger.LogError(
                "Abandoned {Trials} simulated runs for teams {Teams}: every Feature with work left was waiting on one that had not finished, which can only happen if the Features are waiting on each other in a circle. The dates for those Features are not a forecast",
                teamsThatCouldNotFinish.Values.Sum(),
                string.Join(", ", teamsThatCouldNotFinish.Keys.Order(StringComparer.Ordinal)));
        }

        /// <summary>
        /// Which rows in one Team's run have to reach zero before another of them may be worked on, worked
        /// out once per run and read on every draw. It is built from the rows themselves rather than from
        /// names, so "has the Feature waited on finished" is answered by the remaining counts the trial
        /// already resets - there is no per-trial state here to keep in step with anything.
        ///
        /// A Feature waited on that has no row in this Team's run has already finished, or is not part of
        /// this run at all. Either way there is nothing here to wait for, and it holds nobody up.
        /// </summary>
        private sealed class WhatEachFeatureIsWaitingFor
        {
            private static readonly SimulationResult[] NothingToWaitFor = [];

            private readonly Dictionary<SimulationResult, SimulationResult[]> mustFinishFirst;

            public WhatEachFeatureIsWaitingFor(IEnumerable<SimulationResult> rowsInThisRun, ForecastWaits waits)
            {
                if (waits.NobodyWaitsForAnything)
                {
                    mustFinishFirst = [];
                    return;
                }

                var rowByFeature = rowsInThisRun
                    .Where(row => row.Feature is not null)
                    .GroupBy(row => row.Feature.ReferenceId, StringComparer.Ordinal)
                    .ToDictionary(byReferenceId => byReferenceId.Key, byReferenceId => byReferenceId.First(), StringComparer.Ordinal);

                mustFinishFirst = rowByFeature.Values
                    .Select(row => (row, blockers: RowsFor(waits.Of(row.Feature.ReferenceId), rowByFeature)))
                    .Where(pair => pair.blockers.Length > 0)
                    .ToDictionary(pair => pair.row, pair => pair.blockers);
            }

            public bool ReadyToBeWorkedOn(SimulationResult row)
            {
                if (mustFinishFirst.Count == 0)
                {
                    return true;
                }

                foreach (var blocker in mustFinishFirst.GetValueOrDefault(row, NothingToWaitFor))
                {
                    if (blocker.HasWorkRemaining)
                    {
                        return false;
                    }
                }

                return true;
            }

            private static SimulationResult[] RowsFor(
                IReadOnlyList<string> blockerReferenceIds, Dictionary<string, SimulationResult> rowByFeature)
            {
                return blockerReferenceIds
                    .Select(referenceId => rowByFeature.GetValueOrDefault(referenceId))
                    .Where(row => row is not null)
                    .Select(row => row!)
                    .ToArray();
            }
        }

        private static void UpdateFeatureForecasts(IEnumerable<Feature> features, List<SimulationResult> simulationResults, Dictionary<int, ForecastThroughputStatus> chipStatusByTeam)
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

        /// <returns>
        /// False when everything with work left was waiting on something unfinished. Nothing a later day
        /// brings can change that - what a Feature waits on only clears when somebody finishes it - so this
        /// is the end of the run rather than an idle day, and the caller stops rather than counting days
        /// forever. Delivery drawn for such a day is discarded either way: a team that could not start
        /// anything did not bank the day, and carrying it forward would give the wait back the time it cost.
        /// </returns>
        private bool SimulateIndividualDayForFeatureForecast(Team team, RunChartData throughput, IEnumerable<SimulationResult> simulationResults, int currentlySimulatedDay, WhatEachFeatureIsWaitingFor whatIsWaitingForWhat)
        {
            var simulatedThroughput = GetSimulatedThroughput(throughput);

            for (var closedItems = 0; closedItems < simulatedThroughput && simulationResults.GetRemainingItems() > 0; closedItems++)
            {
                var simulationResultOfFeatureToUpdate = GetSimulationResultsOfFeatureToUpdate(team, simulationResults, whatIsWaitingForWhat);

                if (simulationResultOfFeatureToUpdate is null)
                {
                    return false;
                }

                ReduceRemainingWorkFromFeatureToUpdate(currentlySimulatedDay, simulationResultOfFeatureToUpdate);
            }

            return true;
        }

        private static void ReduceRemainingWorkFromFeatureToUpdate(int simulatedDays, SimulationResult featureToUpdate)
        {
            featureToUpdate.RemainingItems -= 1;

            if (!featureToUpdate.HasWorkRemaining)
            {
                AddSimulationResult(featureToUpdate.SimulationResults, simulatedDays);
            }
        }

        /// <summary>
        /// The one place a Feature's turn is decided, and the one predicate that says what a dependency does
        /// to a date. Leaving a Feature that cannot start out of this list is not a postponement applied to
        /// it afterwards: the work-in-progress window closes up over the gap, so the Features below it come
        /// into range and take the capacity it could not use.
        /// </summary>
        /// <returns>Nothing when every Feature with work left is waiting on one that has not finished.</returns>
        private SimulationResult? GetSimulationResultsOfFeatureToUpdate(Team team, IEnumerable<SimulationResult> simulationResults, WhatEachFeatureIsWaitingFor whatIsWaitingForWhat)
        {
            var featuresRemaining = simulationResults
                .Where(x => x.HasWorkRemaining && whatIsWaitingForWhat.ReadyToBeWorkedOn(x))
                .ToList();

            if (featuresRemaining.Count == 0)
            {
                return null;
            }

            var featureWorkedOnIndex = RecalculateFeatureWIP(team.FeatureWIP > 0 ? team.FeatureWIP : 1, featuresRemaining.Count);
            var featureWorkedOn = randomNumberService.GetRandomNumber(featureWorkedOnIndex);

            var itemToUpdate = featuresRemaining[featureWorkedOn];
            return itemToUpdate;
        }

        private static int RecalculateFeatureWIP(int featureWIP, int remainingItems)
        {
            return Math.Min(featureWIP, remainingItems);
        }

        private static void RunSimulations(Action individualSimulation)
        {
            for (var trial = 0; trial < Trials; trial++)
            {
                individualSimulation();
            }
        }

        private static void AddSimulationResult(Dictionary<int, int> simulationResults, int simulationResult)
        {
            simulationResults[simulationResult] = simulationResults.GetValueOrDefault(simulationResult) + 1;
        }

        private int GetSimulatedThroughput(RunChartData throughput)
        {
            var randomDay = randomNumberService.GetRandomNumber(throughput.History);
            return throughput.GetCountOnDay(randomDay);
        }
    }
}
