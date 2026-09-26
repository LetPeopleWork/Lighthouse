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

            return [.. sampledWindowDays.Select(samplingWindowDays =>
                CheckOneWindow(team, mode, new ScoredPeriod(horizonDays, scoredPeriodStart, anchorDate, forecastDays, actualCompleted), samplingWindowDays))];
        }

        private RealityCheckCellDto CheckOneWindow(Team team, ThroughputFilterMode mode, ScoredPeriod period, int samplingWindowDays)
        {
            var historyWindowStart = period.Start.AddDays(-samplingWindowDays);
            var history = teamMetricsService.GetBlackoutAwareThroughputForTeam(
                team, AsDateTime(historyWindowStart), AsDateTime(period.Start), mode);

            if (!ForecastDataSufficiencyPolicy.HasEnoughData(history))
            {
                return Unevaluable(period, samplingWindowDays, historyWindowStart, SufficiencyReason.TooFewActiveDays, history.DaysWithThroughput);
            }

            var forecast = forecastService.HowMany(history, period.ForecastDays);
            var levels = ConfidenceLevels
                .Select(level => new RealityCheckForecastDto(level, forecast.GetProbability(level)))
                .ToList();

            if (!RealityCheckVerdictPolicy.HasAReadingAtEveryLevel(levels))
            {
                return Unevaluable(period, samplingWindowDays, historyWindowStart, SufficiencyReason.DegenerateForecast, history.DaysWithThroughput);
            }

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
                RealityCheckVerdictPolicy.Outcome(period.ActualCompleted, levels),
                levelOutcomes);
        }

        private static RealityCheckCellDto Unevaluable(
            ScoredPeriod period, int samplingWindowDays, DateOnly historyWindowStart, SufficiencyReason reason, int daysWithCompletedWork)
            => new(
                period.Horizon,
                samplingWindowDays,
                period.Start,
                period.End,
                historyWindowStart,
                period.Start,
                new RealityCheckSufficiencyDto(false, reason, daysWithCompletedWork),
                null,
                null,
                null,
                null);

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
    }
}
