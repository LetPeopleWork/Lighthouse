using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/portfolios/{portfolioId}/metrics")]
    [Route("api/projects/{portfolioId}/metrics")] // Backward Compatibility
    [ApiController]
    public class PortfolioMetricsController : ControllerBase
    {
        private const string StartDateMustBeBeforeEndDateErrorMessage = "Start date must be before end date.";
        private readonly IRepository<Portfolio> projectRepository;
        private readonly IProjectMetricsService projectMetricsService;

        public PortfolioMetricsController(IRepository<Portfolio> projectRepository, IProjectMetricsService projectMetricsService)
        {
            this.projectRepository = projectRepository;
            this.projectMetricsService = projectMetricsService;
        }

        [HttpGet("throughput")]
        public ActionResult<RunChartData> GetThroughput(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, (project) =>
                projectMetricsService.GetThroughputForProject(project, startDate, endDate));
        }

        [HttpGet("started")]
        public ActionResult<RunChartData> GetStartedItems(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, (project) => projectMetricsService.GetStartedItemsForProject(project, startDate, endDate));
        }

        [HttpGet("wipOverTime")]
        public ActionResult<RunChartData> GetFeaturesInProgressOverTime(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, (project) =>
                projectMetricsService.GetFeaturesInProgressOverTimeForProject(project, startDate, endDate));
        }

        [HttpGet("currentwip")]
        public ActionResult<IEnumerable<FeatureDto>> GetInProgressFeatures(int portfolioId)
        {
            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, (project) =>
            {
                var features = projectMetricsService.GetInProgressFeaturesForProject(project);
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

            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, (project) =>
                projectMetricsService.GetCycleTimePercentilesForProject(project, startDate, endDate));
        }

        [HttpGet("cycleTimeData")]
        public ActionResult<IEnumerable<FeatureDto>> GetCycleTimeData(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, (project) =>
            {
                var features = projectMetricsService.GetCycleTimeDataForProject(project, startDate, endDate);
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

            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, (project) =>
            {
                var features = projectMetricsService.GetAllFeaturesForSizeChart(project, startDate, endDate);
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

            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, (project) =>
                projectMetricsService.GetSizePercentilesForProject(project, startDate, endDate));
        }

        [HttpGet("multiitemforecastpredictabilityscore")]
        public ActionResult<ForecastPredictabilityScore> GetMultiItemForecastPredictabilityScore(int portfolioId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, project =>
            {
                return projectMetricsService.GetMultiItemForecastPredictabilityScoreForProject(project, startDate, endDate);
            });
        }

        [HttpGet("totalWorkItemAge")]
        public ActionResult<int> GetTotalWorkItemAge(int portfolioId)
        {
            return this.GetEntityByIdAnExecuteAction(projectRepository, portfolioId, projectMetricsService.GetTotalWorkItemAge);
        }
    }
}