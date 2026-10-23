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
#pragma warning disable S107 // The run marks the Portfolio's recorded inputs right beside saving its forecasts, so the Portfolio repository joins. Every other collaborator is used on its own, and grouping any of them would invent a concept.
    public class ForecastService(
        IRandomNumberService randomNumberService,
        ILogger<ForecastService> logger,
        ITeamMetricsService teamMetricsService,
        IRepository<Feature> featureRepository,
        IPortfolioRepository portfolioRepository,
        IWhatTheForecastWaitsFor whatTheForecastWaitsFor,
        ForecastSimulation simulation,
        TimeProvider timeProvider)
        : IForecastService
#pragma warning restore S107
    {
        private readonly IDrawStreamFactory drawStreamFactory = simulation.Draws;

        private readonly ForecastSimulationLimits limits = simulation.Limits;

        private DateTime CreationInstant() => timeProvider.GetUtcNow().UtcDateTime;

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

            return new HowManyForecast(simulationResults, days, CreationInstant());
        }

        public async Task<WhenForecast> When(Team team, int remainingItems, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
        {
            logger.LogDebug("Running Monte Carlo Forecast When for Team {TeamName} and {RemainingItems} items (mode {Mode}).", team.Name, remainingItems, mode);

            var fakeFeature = new Feature(team, remainingItems);
            await Simulate(Gather([fakeFeature], mode), CancellationToken.None);

            logger.LogDebug("Finished running Monte Carlo Forecast When for Team {TeamName} and {RemainingItems} items.", team.Name, remainingItems);

            return fakeFeature.Forecast;
        }

        /// <summary>
        /// A stop asked for while the runs are going ends the forecast between two runs: the cancellation
        /// leaves before anything is saved, so the Features keep the forecast they already had rather than
        /// one built from part of the runs.
        ///
        /// When everything the simulation would be handed matches what the Portfolio recorded after its last
        /// computed forecast, the forecasts it already has are the ones this run would produce, so nothing runs
        /// and nothing is written.
        ///
        /// Otherwise the recorded inputs are swapped for a mark of this run before the new forecasts are saved,
        /// so that until the caller records what they were computed from, no round can take them for current.
        /// </summary>
        public async Task<ForecastRunOutcome> UpdateForecastsForPortfolio(Portfolio portfolio, DateOnly instanceDay, CancellationToken cancellationToken = default)
        {
            var recorded = portfolio.ForecastInputFingerprint;
            var teams = portfolio.Teams;

            logger.LogInformation("Running Monte Carlo Forecast for all Features with involved of teams {Teams}", string.Join(',', teams.Select(t => t.Name)));

            var features = TheFeaturesWorkedOnBy(teams);

            logger.LogDebug("Features that are being forecasted: {Features}", string.Join(",", features.Select(f => f.Name)));

            var gathered = Gather(features, ThroughputFilterMode.RespectTeamSetting);
            var fresh = FingerprintOf(gathered, instanceDay, portfolio);

            if (IsUnchangedSinceRecorded(fresh, recorded))
            {
                logger.LogInformation("Forecast inputs of Portfolio {Portfolio} are unchanged since its last forecast, so its forecasts stand", portfolio.Name);
                return new ForecastRunOutcome.ConfirmedUnchanged(features.Select(f => f.Id).ToList());
            }

            await Simulate(gathered, cancellationToken);

            var runMark = await portfolioRepository.TryMarkForecastRunning(portfolio.Id, recorded, cancellationToken);
            await featureRepository.Save();

            return new ForecastRunOutcome.Computed(runMark, fresh);
        }

        private static bool IsUnchangedSinceRecorded(string? fresh, string? recorded)
            => fresh is not null && string.Equals(fresh, recorded, StringComparison.Ordinal);

        private List<Feature> TheFeaturesWorkedOnBy(List<Team> teams)
        {
            var teamIds = teams.Select(team => team.Id).ToList();

            return featureRepository
                .GetAllByPredicate(feature => feature.FeatureWork.Any(work => teamIds.Contains(work.TeamId)))
                .ToList();
        }

        /// <summary>
        /// A fingerprint is only ever a reason to skip work, so one that cannot be taken means the forecast
        /// computes, with nothing to record afterwards; the forecast itself must not fail over it.
        /// </summary>
        private string? FingerprintOf(GatheredInputs gathered, DateOnly instanceDay, Portfolio portfolio)
        {
            try
            {
                return ForecastInputFingerprint.Of(new ForecastInputs(instanceDay, limits, gathered.Features, gathered.ChipStatusByTeam, gathered.Waits));
            }
            // Whatever went wrong, the cost is only a forecast computed in full, and the forecast must not fail over it.
#pragma warning disable CA1031
            catch (Exception e)
#pragma warning restore CA1031
            {
                logger.LogWarning(e, "Could not fingerprint the forecast inputs of Portfolio {Portfolio}, so it is forecast in full", portfolio.Name);
                return null;
            }
        }

        private GatheredInputs Gather(List<Feature> features, ThroughputFilterMode mode)
        {
            var throughputByTeam = InitializeThroughputPerTeam(features, mode, out var chipStatusByTeam);

            return new GatheredInputs(features, throughputByTeam, chipStatusByTeam, whatTheForecastWaitsFor.Of(features));
        }

        private async Task Simulate(GatheredInputs gathered, CancellationToken cancellationToken)
        {
            var simulationResults = InitializeSimulationResults(gathered.Features);
            var draws = drawStreamFactory.ForOneRun();

            var startForecasts = await Task.Run(
                () => RunMonteCarloSimulation(simulationResults, gathered.ThroughputByTeam, gathered.Waits, draws, cancellationToken), cancellationToken);

            UpdateFeatureForecasts(gathered.Features, simulationResults, gathered.ChipStatusByTeam, startForecasts, CreationInstant());
        }

        private sealed record GatheredInputs(
            List<Feature> Features,
            Dictionary<int, RunChartData> ThroughputByTeam,
            Dictionary<int, ForecastThroughputStatus> ChipStatusByTeam,
            ForecastWaits Waits);

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
            IDrawStream draws,
            CancellationToken cancellationToken)
        {
            var plan = ForecastRunPlan.For(simulationResults, throughputByTeam, waits);

            if (plan.RowCount == 0)
            {
                return [];
            }

            var oneRun = new SimulatedRun(plan, draws, limits.MostDaysOneSimulatedRunMayCover);
            var workersThatRanThem = new ConcurrentBag<OneWorkersShareOfTheRuns>();

            var howTheRunsAreSpread = new ParallelOptions
            {
                MaxDegreeOfParallelism = limits.MostRunsAtOnce,
                CancellationToken = cancellationToken,
            };

            Parallel.For(
                0,
                limits.Trials,
                howTheRunsAreSpread,
                () => new OneWorkersShareOfTheRuns(plan),
                (trial, _, share) =>
                {
                    share.CarryOut(oneRun, trial);
                    return share;
                },
                workersThatRanThem.Add);

            var shares = workersThatRanThem.ToList();

            RecordTheDaysEachRowFinishedOn(plan, shares, limits.Trials);

            var whatWentWrong = WhatTheRunsCouldNotFinish.AllOf(shares.Select(share => share.WhatWentWrong));

            ReportTheRunsThatCouldNotFinish(whatWentWrong);
            ReportTheRunsThatReachedTheHorizon(whatWentWrong);

            return TheDaysWorkBeganOn(plan, shares, limits.Trials, CreationInstant());
        }

        /// <summary>
        /// The start distributions, at both grains, added up once after every run is over - the same pass
        /// and the same arithmetic the finished days get.
        ///
        /// The Feature-grain row is the one the run observed rather than one computed from the per-team
        /// rows beside it. Computing it would mean combining those marginals, which can only be done by
        /// assuming the teams move independently; a Feature whose teams both wait on the same upstream
        /// work is precisely where that is false, and is also the case anyone looks at a start date for.
        ///
        /// Every run counts towards a start, including the ones in which work never began, so a Feature
        /// stuck behind one that never finishes reads as starting beyond the horizon in those runs.
        /// </summary>
        private static Dictionary<Feature, List<StartForecast>> TheDaysWorkBeganOn(ForecastRunPlan plan, List<OneWorkersShareOfTheRuns> shares, int runs, DateTime creationTime)
        {
            var byRow = DayCounts.EmptyPer(plan.RowCount);
            var byFeature = DayCounts.EmptyPer(plan.FeatureCount);

            foreach (var recorded in shares.Select(share => share.Recordings))
            {
                recorded.AddRowStartsInto(byRow);
                recorded.AddFeatureStartsInto(byFeature);
            }

            var startForecasts = new Dictionary<Feature, List<StartForecast>>();

            for (var row = 0; row < plan.RowCount; row++)
            {
                if (plan.RowAt(row).Feature is { } feature)
                {
                    TheStartForecastsOf(startForecasts, feature).Add(new StartForecast(InDayOrder(byRow[row]), plan.RowAt(row).Team, runs, creationTime));
                }
            }

            for (var featureIndex = 0; featureIndex < plan.FeatureCount; featureIndex++)
            {
                var feature = plan.FeatureAt(featureIndex);
                TheStartForecastsOf(startForecasts, feature).Add(new StartForecast(InDayOrder(byFeature[featureIndex]), null, runs, creationTime));
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

        /// <summary>
        /// Added up once, after every run is over. Counts add up the same whichever worker's share is taken
        /// first, and the days are written out in order, so how the work happened to be split between workers
        /// leaves no trace in what comes out. Every run covers every row, so each row was simulated as many
        /// times as there were runs, whether or not those runs finished it.
        /// </summary>
        private static void RecordTheDaysEachRowFinishedOn(ForecastRunPlan plan, List<OneWorkersShareOfTheRuns> shares, int runs)
        {
            var total = DayCounts.EmptyPer(plan.RowCount);

            foreach (var share in shares)
            {
                share.Recordings.AddInto(total);
            }

            for (var row = 0; row < plan.RowCount; row++)
            {
                plan.RowAt(row).RunsSimulated = runs;

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
                "Abandoned {Trials} simulated runs for teams {Teams}: every Feature with work left was waiting on one that had not finished, which can only happen if the Features are waiting on each other in a circle. Those runs count as beyond the horizon for those Features",
                whatWentWrong.RunsGivenUpOn,
                string.Join(", ", whatWentWrong.TeamsCaughtInACircle));
        }

        /// <summary>
        /// Work that would take longer than the horizon is an ordinary answer, not a fault: those runs count
        /// towards every percentile and read as beyond the horizon. So the count is only worth a Debug line.
        /// </summary>
        private void ReportTheRunsThatReachedTheHorizon(WhatTheRunsCouldNotFinish whatWentWrong)
        {
            if (whatWentWrong.RunsThatReachedTheHorizon == 0)
            {
                return;
            }

            logger.LogDebug(
                "{Trials} simulated runs reached the horizon of {Days} simulated days without finishing and count as beyond the horizon",
                whatWentWrong.RunsThatReachedTheHorizon,
                limits.MostDaysOneSimulatedRunMayCover);
        }

        private static void UpdateFeatureForecasts(
            IEnumerable<Feature> features,
            List<SimulationResult> simulationResults,
            Dictionary<int, ForecastThroughputStatus> chipStatusByTeam,
            Dictionary<Feature, List<StartForecast>> startForecasts,
            DateTime creationTime)
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

                var featureForecasts = simulationResultsForFeature.Select(r => CreateWhenForecastForSimulationResult(r, chipStatusByTeam, creationTime));
                feature.SetFeatureForecasts(featureForecasts);
            }
        }

        private static WhenForecast CreateWhenForecastForSimulationResult(SimulationResult simulationResult, Dictionary<int, ForecastThroughputStatus> chipStatusByTeam, DateTime creationTime)
        {
            var forecast = new WhenForecast(simulationResult, creationTime);
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
