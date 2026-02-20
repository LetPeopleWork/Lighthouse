using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
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
        IPortfolioMetricsService portfolioMetricsService)
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
                portfolioMetricsService.GetThroughputForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("started")]
        public ActionResult<RunChartData> GetStartedItems(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetStartedItemsForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("wipOverTime")]
        public ActionResult<RunChartData> GetFeaturesInProgressOverTime(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
                portfolioMetricsService.GetFeaturesInProgressOverTimeForPortfolio(portfolio, startDate, endDate));
        }

        [HttpGet("currentwip")]
        public ActionResult<IEnumerable<FeatureDto>> GetInProgressFeatures(int portfolioId)
        {
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) =>
            {
                var features = portfolioMetricsService.GetInProgressFeaturesForPortfolio(portfolio);
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
        public ActionResult<int> GetTotalWorkItemAge(int portfolioId)
        {
            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, portfolioMetricsService.GetTotalWorkItemAge);
        }

        [HttpGet("throughput/pbc")]
        public ActionResult<ProcessBehaviourChart> GetThroughputProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetThroughputProcessBehaviourChart(portfolio, startDate, endDate));
        }

        [HttpGet("wipOverTime/pbc")]
        public ActionResult<ProcessBehaviourChart> GetWipProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetWipProcessBehaviourChart(portfolio, startDate, endDate));
        }

        [HttpGet("totalWorkItemAge/pbc")]
        public ActionResult<ProcessBehaviourChart> GetTotalWorkItemAgeProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetTotalWorkItemAgeProcessBehaviourChart(portfolio, startDate, endDate));
        }

        [HttpGet("cycleTime/pbc")]
        public ActionResult<ProcessBehaviourChart> GetCycleTimeProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetCycleTimeProcessBehaviourChart(portfolio, startDate, endDate));
        }

        [HttpGet("featureSize/pbc")]
        public ActionResult<ProcessBehaviourChart> GetFeatureSizeProcessBehaviourChart(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(portfolioRepository, portfolioId, (portfolio) => portfolioMetricsService.GetFeatureSizeProcessBehaviourChart(portfolio, startDate, endDate));
        }
    }
}