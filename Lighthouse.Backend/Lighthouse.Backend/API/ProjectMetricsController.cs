using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/projects/{projectId}/metrics")]
    [ApiController]
    public class ProjectMetricsController : ControllerBase
    {
        private const string StartDateMustBeBeforeEndDateErrorMessage = "Start date must be before end date.";
        private readonly IRepository<Project> projectRepository;
        private readonly IProjectMetricsService projectMetricsService;

        public ProjectMetricsController(IRepository<Project> projectRepository, IProjectMetricsService projectMetricsService)
        {
            this.projectRepository = projectRepository;
            this.projectMetricsService = projectMetricsService;
        }

        [HttpGet("throughput")]
        public ActionResult<RunChartData> GetThroughput(int projectId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) =>
                projectMetricsService.GetThroughputForProject(project, startDate, endDate));
        }

        [HttpGet("started")]
        public ActionResult<RunChartData> GetStartedItems(int projectId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) => projectMetricsService.GetStartedItemsForProject(project, startDate, endDate));
        }

        [HttpGet("wipOverTime")]
        public ActionResult<RunChartData> GetFeaturesInProgressOverTime(int projectId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) =>
                projectMetricsService.GetFeaturesInProgressOverTimeForProject(project, startDate, endDate));
        }

        [HttpGet("currentwip")]
        public ActionResult<IEnumerable<FeatureDto>> GetInProgressFeatures(int projectId)
        {
            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) =>
            {
                var features = projectMetricsService.GetInProgressFeaturesForProject(project);
                return features.Select(f => new FeatureDto(f));
            });
        }

        [HttpGet("cycleTimePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetCycleTimePercentiles(int projectId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) =>
                projectMetricsService.GetCycleTimePercentilesForProject(project, startDate, endDate));
        }

        [HttpGet("cycleTimeData")]
        public ActionResult<IEnumerable<FeatureDto>> GetCycleTimeData(int projectId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) =>
            {
                var features = projectMetricsService.GetCycleTimeDataForProject(project, startDate, endDate);
                return features.Select(f => new FeatureDto(f));
            });
        }

        [HttpGet("sizePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetSizePercentiles(int projectId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) =>
                projectMetricsService.GetSizePercentilesForProject(project, startDate, endDate));
        }

        [HttpGet("multiitemforecastpredictabilityscore")]
        public ActionResult<ForecastPredictabilityScore> GetMultiItemForecastPredictabilityScore(int projectId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, project =>
            {
                return projectMetricsService.GetMultiItemForecastPredictabilityScoreForProject(project, startDate, endDate);
            });
        }
    }
}