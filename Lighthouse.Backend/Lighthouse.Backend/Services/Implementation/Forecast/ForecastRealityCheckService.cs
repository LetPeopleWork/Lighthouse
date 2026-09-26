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
            var cells = HorizonDays.SelectMany(horizonDays => CheckOneHorizon(team, mode, anchorDate, horizonDays)).ToList();

            return new RealityCheckResultDto(
                team.Id,
                team.Name,
                anchorDate,
                StandardWindowDays,
                StandardWindowDays,
                HorizonDays,
                ConfidenceLevels,
                ForecastDataSufficiencyPolicy.MinimumActiveDays,
                CountWhatWasEvaluated(cells),
                new RealityCheckSoundWindowDto(
                    [],
                    [],
                    Determination.NotEnoughEvidence,
                    team.ThroughputHistory,
                    true,
                    CurrentSettingStanding.NotDetermined,
                    null),
                [.. ConfidenceLevels.Select(level => new RealityCheckLevelCoverageDto(level, 0, 0, LevelReading.NotEvaluated))],
                cells);
        }

        private List<RealityCheckCellDto> CheckOneHorizon(Team team, ThroughputFilterMode mode, DateOnly anchorDate, int horizonDays)
        {
            var scoredPeriodStart = anchorDate.AddDays(-horizonDays);
            var periodStart = AsDateTime(scoredPeriodStart);
            var periodEnd = AsDateTime(anchorDate);

            // The scored period depends only on the horizon, so every sampling window shares this one read.
            var actualCompleted = teamMetricsService.GetThroughputForTeam(team, periodStart, periodEnd, mode).Total;
            var forecastDays = blackoutPeriodService
                .GetEffectiveBlackoutDays(periodStart, periodEnd)
                .CountWorkingDays(periodStart, periodEnd);

            return [.. StandardWindowDays.Select(samplingWindowDays =>
                CheckOneWindow(team, mode, new ScoredPeriod(horizonDays, scoredPeriodStart, anchorDate, forecastDays, actualCompleted), samplingWindowDays))];
        }

        private RealityCheckCellDto CheckOneWindow(Team team, ThroughputFilterMode mode, ScoredPeriod period, int samplingWindowDays)
        {
            var historyWindowStart = period.Start.AddDays(-samplingWindowDays);
            var history = teamMetricsService.GetBlackoutAwareThroughputForTeam(
                team, AsDateTime(historyWindowStart), AsDateTime(period.Start), mode);
            var forecast = forecastService.HowMany(history, period.ForecastDays);

            var levels = ConfidenceLevels
                .Select(level => new RealityCheckForecastDto(level, forecast.GetProbability(level)))
                .ToList();
            var levelOutcomes = levels
                .Select(level => new RealityCheckLevelOutcomeDto(
                    level.Probability, level.Value, RealityCheckVerdictPolicy.Held(period.ActualCompleted, level.Value)))
                .ToList();

            return new RealityCheckCellDto(
                period.Horizon,
                samplingWindowDays,
                period.Start,
                period.End,
                historyWindowStart,
                period.Start,
                new RealityCheckSufficiencyDto(true, SufficiencyReason.Sufficient, history.DaysWithThroughput),
                levels,
                period.ActualCompleted,
                null,
                levelOutcomes);
        }

        private static RealityCheckDenominatorDto CountWhatWasEvaluated(List<RealityCheckCellDto> cells)
        {
            var runsEvaluated = cells.Count(cell => cell.Sufficiency.IsSufficient);

            return new RealityCheckDenominatorDto(cells.Count, runsEvaluated, ConfidenceLevels.Length, runsEvaluated * ConfidenceLevels.Length);
        }

        private static DateTime AsDateTime(DateOnly day) => day.ToDateTime(TimeOnly.MinValue);

        private sealed record ScoredPeriod(int Horizon, DateOnly Start, DateOnly End, int ForecastDays, int ActualCompleted);
    }
}
