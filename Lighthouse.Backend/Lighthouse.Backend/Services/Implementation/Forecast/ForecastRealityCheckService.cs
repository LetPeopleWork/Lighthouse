using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public class ForecastRealityCheckService(
        IForecastService forecastService,
        ITeamMetricsService teamMetricsService,
        IBlackoutPeriodService blackoutPeriodService,
        ILighthouseClock clock)
        : IForecastRealityCheckService
    {
        private static readonly int[] StandardWindowDays = [14, 30, 60, 90];

        private static readonly int[] HorizonDays = [7, 14, 28, 56];

        private static readonly int[] ConfidenceLevels = [50, 70, 85, 95];

        public RealityCheckResultDto Run(Team team, ThroughputFilterMode mode)
        {
            var anchorDate = clock.Today;
            var sampledWindowDays = WindowsToSample(team);
            var cells = HorizonDays
                .SelectMany(horizonDays => CheckOneHorizon(team, mode, anchorDate, horizonDays, sampledWindowDays))
                .ToList();
            var denominator = CountWhatWasEvaluated(cells);

            return new RealityCheckResultDto(
                team.Id,
                team.Name,
                anchorDate,
                StandardWindowDays,
                sampledWindowDays,
                HorizonDays,
                ConfidenceLevels,
                ForecastDataSufficiencyPolicy.MinimumActiveDays,
                denominator,
                RealityCheckVerdictPolicy.SoundWindows(sampledWindowDays, cells, team.ThroughputHistory, team.UseFixedDatesForThroughput),
                CoverageOfEachLevel(cells, denominator.RunsEvaluated),
                cells);
        }

        // The Team's own setting is the window its forecasts actually use, so it is always among those checked,
        // even when it is not one of the standard lengths. A Team with no rolling window of its own adds nothing.
        private static List<int> WindowsToSample(Team team)
        {
            if (RealityCheckVerdictPolicy.WhyNotTested(team.ThroughputHistory, team.UseFixedDatesForThroughput) is not null)
            {
                return [.. StandardWindowDays];
            }

            return [.. StandardWindowDays.Append(team.ThroughputHistory).Distinct().Order()];
        }

        private List<RealityCheckCellDto> CheckOneHorizon(
            Team team, ThroughputFilterMode mode, DateOnly anchorDate, int horizonDays, List<int> sampledWindowDays)
        {
            var scoredPeriodStart = anchorDate.AddDays(-horizonDays);
            var periodStart = AsDateTime(scoredPeriodStart);
            var periodEnd = AsDateTime(anchorDate);

            // The scored period depends only on the horizon, so every sampling window shares this one read.
            var actualCompleted = teamMetricsService.GetThroughputForTeam(team, periodStart, periodEnd, mode).Total;
            var forecastDays = blackoutPeriodService
                .GetEffectiveBlackoutDays(periodStart, periodEnd)
                .CountWorkingDays(periodStart, periodEnd);
            var period = new ScoredPeriod(horizonDays, scoredPeriodStart, anchorDate, forecastDays, actualCompleted);

            return [.. sampledWindowDays.Select(samplingWindowDays => CheckOneWindow(team, mode, new CheckedWindow(period, samplingWindowDays)))];
        }

        private RealityCheckCellDto CheckOneWindow(Team team, ThroughputFilterMode mode, CheckedWindow window)
        {
            var history = teamMetricsService.GetBlackoutAwareThroughputForTeam(
                team, AsDateTime(window.HistoryStart), AsDateTime(window.HistoryEnd), mode);

            if (!ForecastDataSufficiencyPolicy.HasEnoughData(history))
            {
                return window.Unevaluable(SufficiencyReason.TooFewActiveDays, history.DaysWithThroughput);
            }

            var forecast = forecastService.HowMany(history, window.Period.ForecastDays);
            var levels = ConfidenceLevels
                .Select(level => new RealityCheckForecastDto(level, forecast.GetProbability(level)))
                .ToList();

            if (!RealityCheckVerdictPolicy.HasAReadingAtEveryLevel(levels))
            {
                return window.Unevaluable(SufficiencyReason.DegenerateForecast, history.DaysWithThroughput);
            }

            return window.Evaluated(levels, history.DaysWithThroughput);
        }

        private static RealityCheckDenominatorDto CountWhatWasEvaluated(List<RealityCheckCellDto> cells)
        {
            var runsEvaluated = cells.Count(cell => cell.Sufficiency.IsSufficient);

            return new RealityCheckDenominatorDto(cells.Count, runsEvaluated, ConfidenceLevels.Length, runsEvaluated * ConfidenceLevels.Length);
        }

        private static List<RealityCheckLevelCoverageDto> CoverageOfEachLevel(List<RealityCheckCellDto> cells, int runsEvaluated)
        {
            var heldOutcomes = cells
                .Where(cell => cell.Sufficiency.IsSufficient)
                .SelectMany(cell => cell.LevelOutcomes ?? [])
                .Where(outcome => outcome.Held)
                .ToList();

            return [.. ConfidenceLevels.Select(level => RealityCheckVerdictPolicy.Coverage(
                level, heldOutcomes.Count(outcome => outcome.ConfidenceLevel == level), runsEvaluated))];
        }

        private static DateTime AsDateTime(DateOnly day) => day.ToDateTime(TimeOnly.MinValue);

        private sealed record ScoredPeriod(int Horizon, DateOnly Start, DateOnly End, int ForecastDays, int ActualCompleted);

        // A sampling window learns from the days right before the period it is scored on, so its history ends
        // where that period starts.
        private sealed record CheckedWindow(ScoredPeriod Period, int SamplingWindowDays)
        {
            public DateOnly HistoryStart => Period.Start.AddDays(-SamplingWindowDays);

            public DateOnly HistoryEnd => Period.Start;

            public RealityCheckCellDto Unevaluable(SufficiencyReason reason, int daysWithCompletedWork)
                => Cell(new RealityCheckSufficiencyDto(false, reason, daysWithCompletedWork), null, null, null, null);

            public RealityCheckCellDto Evaluated(List<RealityCheckForecastDto> forecast, int daysWithCompletedWork)
            {
                var actualCompleted = Period.ActualCompleted;
                var levelOutcomes = forecast
                    .Select(level => new RealityCheckLevelOutcomeDto(
                        level.Probability, level.Value, RealityCheckVerdictPolicy.Held(actualCompleted, level.Value)))
                    .ToList();

                return Cell(
                    new RealityCheckSufficiencyDto(true, SufficiencyReason.Sufficient, daysWithCompletedWork),
                    forecast,
                    actualCompleted,
                    RealityCheckVerdictPolicy.Outcome(actualCompleted, forecast),
                    levelOutcomes);
            }

            private RealityCheckCellDto Cell(
                RealityCheckSufficiencyDto sufficiency,
                List<RealityCheckForecastDto>? forecast,
                int? actualCompleted,
                CellOutcome? outcome,
                List<RealityCheckLevelOutcomeDto>? levelOutcomes)
                => new(
                    Period.Horizon,
                    SamplingWindowDays,
                    Period.Start,
                    Period.End,
                    HistoryStart,
                    HistoryEnd,
                    sufficiency,
                    forecast,
                    actualCompleted,
                    outcome,
                    levelOutcomes);
        }
    }
}
