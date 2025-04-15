using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/projects/{projectId}/metrics")]
    [ApiController]
    public class ProjectMetricsController : ControllerBase
    {
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
                return BadRequest("Start date must be before end date.");
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) => 
                projectMetricsService.GetThroughputForProject(project, startDate, endDate));
        }

        [HttpGet("featuresInProgressOverTime")]
        public ActionResult<RunChartData> GetFeaturesInProgressOverTime(int projectId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest("Start date must be before end date.");
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) => 
                projectMetricsService.GetFeaturesInProgressOverTimeForProject(project, startDate, endDate));
        }

        [HttpGet("inProgressFeatures")]
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
                return BadRequest("Start date must be before end date.");
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) => 
                projectMetricsService.GetCycleTimePercentilesForProject(project, startDate, endDate));
        }

        [HttpGet("cycleTimeData")]
        public ActionResult<IEnumerable<FeatureDto>> GetCycleTimeData(int projectId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest("Start date must be before end date.");
            }

            return this.GetEntityByIdAnExecuteAction(projectRepository, projectId, (project) =>
            {
                var features = projectMetricsService.GetCycleTimeDataForProject(project, startDate, endDate);
                return features.Select(f => new FeatureDto(f));
            });
        }
    }
}