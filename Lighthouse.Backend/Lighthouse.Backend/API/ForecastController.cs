using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Services.Common;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    public class ForecastController(
        IForecastUpdater forecastUpdater,
        IForecastService forecastService,
        IRepository<Team> teamRepository,
        IPortfolioRepository portfolioRepository,
        ITeamMetricsService teamMetricsService,
        IBlackoutPeriodService blackoutPeriodService,
        ILighthouseClock clock)
        : ControllerBase
    {
        /// <summary>
        /// Accepted rather than OK, and only for a portfolio that is really there. A forecast runs in the
        /// background, so the most this can honestly say is that one is now queued and nothing will hold it
        /// back - saying it is done would be a claim about a simulation that has not started, and saying it
        /// about a portfolio that does not exist would be a claim about a run that will never happen.
        /// </summary>
        [HttpPost("update/{id:int}")]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "id")]
        public ActionResult UpdateForecastForProject(int id)
        {
            if (!portfolioRepository.Exists(id))
            {
                return NotFound();
            }

            forecastUpdater.TriggerImmediateUpdate(id);

            return Accepted();
        }

        /// <summary>
        /// Answers with the number of forecasts queued rather than a bare success, because a team can work
        /// on nothing that is forecast at all, and telling that operator it worked leaves them watching for
        /// a change that was never going to come.
        /// </summary>
        [HttpPost("update-portfolios-for-team/{teamId:int}")]
        [RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "teamId")]
        public ActionResult<int> UpdateForecastsForTeamPortfolios(int teamId)
        {
            if (!teamRepository.Exists(teamId))
            {
                return NotFound();
            }

            var portfolioIds = portfolioRepository.GetPortfolioIdsForTeam(teamId);

            foreach (var portfolioId in portfolioIds)
            {
                forecastUpdater.TriggerImmediateUpdate(portfolioId);
            }

            return Accepted(portfolioIds.Count);
        }

        [HttpPost("itemprediction/{id:int}")]
        [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "id")]
        public ActionResult<ManualForecastDto> RunItemCreationPrediction(int id, [FromBody] ItemCreationPredictionInputDto input)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, id, team =>
            {
                var itemCreationPrediction = new ManualForecastDto(0, input.TargetDate);
                var timeToTargetDate = (input.TargetDate - clock.TodayAsUtcMidnight).Days;

                var howManyForecast = forecastService.PredictWorkItemCreation(team, input.WorkItemTypes, input.StartDate, input.EndDate, timeToTargetDate);

                // "X or less" forecast: invert the percentiles (50/30/15/5) and the probability so high probabilities map to low item counts.
                var forecastDtos = howManyForecast.CreateForecastDtos(50, 30, 15, 5);
                forecastDtos.ForEach(f => f.Probability = 100 - f.Probability);

                itemCreationPrediction.HowManyForecasts.AddRange(forecastDtos.OrderByDescending(x => x.Probability));

                return itemCreationPrediction;
            });
        }

        [HttpPost("manual/{id:int}")]
        [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "id")]
        public async Task<ActionResult<ManualForecastDto>> RunManualForecastAsync(int id, [FromBody] ManualForecastInputDto input)
        {
            return await this.GetEntityByIdAnExecuteAction(teamRepository, id, async team =>
            {
                var manualForecast = new ManualForecastDto(input.RemainingItems ?? 0, input.TargetDate);
                var mode = MapOverrideToFilterMode(input.ApplyFilterOverride);
                var forecastWindowStart = clock.TodayAsUtcMidnight;

                var timeToTargetDate = input.TargetDate.HasValue
                    ? blackoutPeriodService
                        .GetEffectiveBlackoutDays(forecastWindowStart, input.TargetDate.Value)
                        .CountWorkingDays(forecastWindowStart, input.TargetDate.Value)
                    : 0;

                if (input.RemainingItems is > 0)
                {
                    var whenForecast = await forecastService.When(team, input.RemainingItems.Value, mode);

                    var effectiveBlackoutDays = blackoutPeriodService.GetEffectiveBlackoutDays(
                        forecastWindowStart,
                        EstimateForecastWindowEnd(whenForecast, forecastWindowStart));

                    manualForecast.WhenForecasts.AddRange(whenForecast.CreateForecastDtos(clock.Today, effectiveBlackoutDays, 50, 70, 85, 95));
                    manualForecast.FilterApplied = whenForecast.FilterApplied;
                    manualForecast.ExcludedSummary = whenForecast.ExcludedSummary;
                    manualForecast.HasSufficientData = whenForecast.HasSufficientData;

                    if (timeToTargetDate > 0 && whenForecast.TotalTrials > 0)
                    {
                        manualForecast.Likelihood = whenForecast.GetLikelihood(timeToTargetDate);
                    }
                }

                if (timeToTargetDate <= 0)
                {
                    return manualForecast;
                }

                var status = teamMetricsService.GetForecastThroughputStatus(team, mode);
                var howManyForecast = forecastService.HowMany(status.Throughput, timeToTargetDate);

                manualForecast.HowManyForecasts.AddRange(howManyForecast.CreateForecastDtos(50, 70, 85, 95));
                manualForecast.FilterApplied = status.FilterApplied;
                manualForecast.ExcludedSummary = status.ExcludedSummary;
                manualForecast.HasSufficientData = status.HasSufficientData;

                return manualForecast;
            });
        }

        private static DateTime EstimateForecastWindowEnd(WhenForecast whenForecast, DateTime windowStart)
        {
            const int CalendarHeadroomDays = 14;
            const int BlackoutDensityMultiplier = 2;

            var worstCaseWorkingDays = whenForecast.GetProbability(95);
            var calendarSpan = (worstCaseWorkingDays * BlackoutDensityMultiplier) + CalendarHeadroomDays;

            return windowStart.AddDays(calendarSpan);
        }

        private static ThroughputFilterMode MapOverrideToFilterMode(bool? applyFilterOverride)
        {
            return applyFilterOverride switch
            {
                true => ThroughputFilterMode.ApplyFilter,
                false => ThroughputFilterMode.SkipFilter,
                null => ThroughputFilterMode.RespectTeamSetting,
            };
        }

        private static string? ValidateBacktestInput(BacktestInputDto input, DateOnly today)
        {
            var minStartDate = today.AddDays(-14);
            if (input.StartDate > minStartDate)
            {
                return "StartDate must be at least 14 days in the past.";
            }

            if (input.EndDate.DayNumber - input.StartDate.DayNumber < 14)
            {
                return "EndDate must be at least 14 days after StartDate.";
            }

            if (input.HistoricalEndDate > input.StartDate)
            {
                return "HistoricalEndDate must not be after StartDate.";
            }

            if (input.HistoricalStartDate >= input.HistoricalEndDate)
            {
                return "HistoricalStartDate must be before HistoricalEndDate.";
            }

            return null;
        }

        [HttpPost("backtest/{teamId:int}")]
        [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
        public ActionResult<BacktestResultDto> RunBacktest(int teamId, [FromBody] BacktestInputDto input)
        {
            var validationError = ValidateBacktestInput(input, clock.Today);
            if (validationError != null)
            {
                return BadRequest(validationError);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
            {
                var mode = MapOverrideToFilterMode(input.ApplyFilterOverride);
                var historyStart = input.HistoricalStartDate.ToDateTime(TimeOnly.MinValue);
                var historyEnd = input.HistoricalEndDate.ToDateTime(TimeOnly.MinValue);
                var historicalThroughput = teamMetricsService.GetBlackoutAwareThroughputForTeam(team, historyStart, historyEnd, mode);

                var periodStart = input.StartDate.ToDateTime(TimeOnly.MinValue);
                var periodEnd = input.EndDate.ToDateTime(TimeOnly.MinValue);
                var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(periodStart, periodEnd);
                var forecastDays = blackoutPeriods.CountWorkingDays(periodStart, periodEnd);

                var howManyForecast = forecastService.HowMany(historicalThroughput, forecastDays);

                var actualThroughput = teamMetricsService.GetThroughputForTeam(team, periodStart, periodEnd, mode);

                var status = teamMetricsService.GetForecastThroughputStatus(team, mode);

                var result = new BacktestResultDto(input.StartDate, input.EndDate, input.HistoricalStartDate, input.HistoricalEndDate)
                {
                    ActualThroughput = actualThroughput.Total,
                    FilterApplied = status.FilterApplied,
                    ExcludedSummary = status.ExcludedSummary,
                };

                result.Percentiles.AddRange(howManyForecast.CreateForecastDtos(50, 70, 85, 95));

                return result;
            });
        }

        public class ManualForecastInputDto
        {
            public int? RemainingItems { get; set; }

            public DateTime? TargetDate { get; set; }

            public bool? ApplyFilterOverride { get; set; }
        }

        public class ItemCreationPredictionInputDto
        {
            [JsonRequired]
            public DateTime StartDate { get; set; }

            [JsonRequired]
            public DateTime EndDate { get; set; }

            [JsonRequired]
            public DateTime TargetDate { get; set; }

            [JsonRequired]
            public string[] WorkItemTypes { get; set; } = [];
        }
    }
}
