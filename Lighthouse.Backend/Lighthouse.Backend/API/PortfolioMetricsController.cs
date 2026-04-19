using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/portfolios/{portfolioId:int}/metrics")]
    [Route("api/projects/{portfolioId:int}/metrics")] // Backward Compatibility
    [ApiController]
    public class PortfolioMetricsController(
        IRepository<Portfolio> portfolioRepository,
        IPortfolioMetricsService portfolioMetricsService,
        IRepository<BlackoutPeriod> blackoutPeriodRepository,
        ILogger<PortfolioMetricsController> logger)
        : ControllerBase
    {
        private const string StartDateMustBeBeforeEndDateErrorMessage = "Start date must be before end date.";

        [HttpGet("throughput")]
        public ActionResult<RunChartData> GetThroughput(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var data = portfolioMetricsService.GetThroughputForPortfolio(portfolio, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("started")]
        public ActionResult<RunChartData> GetStartedItems(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var data = portfolioMetricsService.GetStartedItemsForPortfolio(portfolio, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("arrivals")]
        public ActionResult<RunChartData> GetArrivals(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var data = portfolioMetricsService.GetArrivalsForPortfolio(portfolio, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("wipOverTime")]
        public ActionResult<RunChartData> GetFeaturesInProgressOverTime(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var data = portfolioMetricsService.GetFeaturesInProgressOverTimeForPortfolio(portfolio, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("wip")]
        public ActionResult<IEnumerable<FeatureDto>> GetInProgressFeatures(int portfolioId, [FromQuery] DateTime asOfDate)
        {
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var features = portfolioMetricsService.GetInProgressFeaturesForPortfolio(portfolio, asOfDate);
                return features.Select(f => new FeatureDto(f));
            });
        }

        [HttpGet("cycleTimePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetCycleTimePercentiles(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTimePercentiles", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetCycleTimePercentilesForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("cycleTimeData")]
        public ActionResult<IEnumerable<FeatureDto>> GetCycleTimeData(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTimeData", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var features = portfolioMetricsService.GetCycleTimeDataForPortfolio(portfolio, startDate, endDate);
                return features.Select(f => new FeatureDto(f));
            });
        }

        [HttpGet("allFeaturesForSizeChart")]
        public ActionResult<IEnumerable<FeatureDto>> GetAllFeaturesForSizeChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var features = portfolioMetricsService.GetAllFeaturesForSizeChart(portfolio, startDate, endDate);
                return features.Select(f => new FeatureDto(f));
            });
        }

        [HttpGet("sizePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetSizePercentiles(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetSizePercentilesForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("multiitemforecastpredictabilityscore")]
        public ActionResult<ForecastPredictabilityScore> GetMultiItemForecastPredictabilityScore(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, portfolio => portfolioMetricsService.GetMultiItemForecastPredictabilityScoreForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("totalWorkItemAge")]
        public ActionResult<int> GetTotalWorkItemAge(int portfolioId, [FromQuery] DateTime asOfDate)
        {
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetTotalWorkItemAge(portfolio, asOfDate));
        }

        [HttpGet("throughputInfo")]
        public ActionResult<ThroughputInfoDto> GetThroughputInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetThroughputInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("arrivalsInfo")]
        public ActionResult<ArrivalsInfoDto> GetArrivalsInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetArrivalsInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("featureSizePercentilesInfo")]
        public ActionResult<FeatureSizePercentilesInfoDto> GetFeatureSizePercentilesInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetFeatureSizePercentilesInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("wipOverviewInfo")]
        public ActionResult<WipOverviewInfoDto> GetWipOverviewInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetWipOverviewInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("totalWorkItemAgeInfo")]
        public ActionResult<TotalWorkItemAgeInfoDto> GetTotalWorkItemAgeInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetTotalWorkItemAgeInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("predictabilityScoreInfo")]
        public ActionResult<PredictabilityScoreInfoDto> GetPredictabilityScoreInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetPredictabilityScoreInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("cycleTimePercentilesInfo")]
        public ActionResult<CycleTimePercentilesInfoDto> GetCycleTimePercentilesInfo(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetCycleTimePercentilesInfoForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("throughput/pbc")]
        public ActionResult<ProcessBehaviourChart> GetThroughputProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetThroughputProcessBehaviourChart(portfolio, startDate, endDate)));
        }

        [HttpGet("arrivals/pbc")]
        public ActionResult<ProcessBehaviourChart> GetArrivalsProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetArrivalsProcessBehaviourChart(portfolio, startDate, endDate)));
        }

        [HttpGet("wipOverTime/pbc")]
        public ActionResult<ProcessBehaviourChart> GetWipProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetWipProcessBehaviourChart(portfolio, startDate, endDate)));
        }

        [HttpGet("totalWorkItemAge/pbc")]
        public ActionResult<ProcessBehaviourChart> GetTotalWorkItemAgeProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetTotalWorkItemAgeProcessBehaviourChart(portfolio, startDate, endDate)));
        }

        [HttpGet("cycleTime/pbc")]
        public ActionResult<ProcessBehaviourChart> GetCycleTimeProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTime/pbc", portfolioId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetCycleTimeProcessBehaviourChart(portfolio, startDate, endDate)));
        }

        [HttpGet("featureSize/pbc")]
        public ActionResult<ProcessBehaviourChart> GetFeatureSizeProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                AnnotateBlackoutDays(portfolioMetricsService.GetFeatureSizeProcessBehaviourChart(portfolio, startDate, endDate)));
        }

        [HttpGet("estimationVsCycleTime")]
        public ActionResult<EstimationVsCycleTimeResponse> GetEstimationVsCycleTimeData(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetEstimationVsCycleTimeData(portfolio, startDate, endDate));
        }

        [HttpGet("featureSizeEstimation")]
        public ActionResult<FeatureSizeEstimationResponse> GetFeatureSizeEstimationData(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetFeatureSizeEstimationData(portfolio, startDate, endDate));
        }

        private int[] GetBlackoutDayIndicesArray(DateTime startDate, DateTime endDate)
        {
            var blackoutPeriods = blackoutPeriodRepository.GetAll();
            return blackoutPeriods.GetBlackoutDayIndices(startDate, endDate).OrderBy(i => i).ToArray();
        }

        private ProcessBehaviourChart AnnotateBlackoutDays(ProcessBehaviourChart chart)
        {
            var blackoutPeriods = blackoutPeriodRepository.GetAll();
            return blackoutPeriods.AnnotateBlackoutDays(chart);
        }

        private void LogDateBoundaries(string endpoint, int portfolioId, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Metrics request {Endpoint} for portfolio {PortfolioId}: startDate={StartDate:yyyy-MM-dd} endDate={EndDate:yyyy-MM-dd} (Kind={StartKind}/{EndKind})",
                endpoint, portfolioId, startDate, endDate, startDate.Kind, endDate.Kind);
        }
    }
}