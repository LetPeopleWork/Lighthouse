using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/teams/{teamId:int}/metrics")]
    [ApiController]
    public class TeamMetricsController : ControllerBase
    {
        private const string StartDateMustBeBeforeEndDateErrorMessage = "Start date must be before end date.";
        private readonly IRepository<Team> teamRepository;
        private readonly ITeamMetricsService teamMetricsService;
        private readonly IRepository<BlackoutPeriod> blackoutPeriodRepository;
        private readonly ILogger<TeamMetricsController> logger;

        public TeamMetricsController(IRepository<Team> teamRepository, ITeamMetricsService teamMetricsService, IRepository<BlackoutPeriod> blackoutPeriodRepository, ILogger<TeamMetricsController> logger)
        {
            this.teamRepository = teamRepository;
            this.teamMetricsService = teamMetricsService;
            this.blackoutPeriodRepository = blackoutPeriodRepository;
            this.logger = logger;
        }

        [HttpGet("throughput")]
        public ActionResult<RunChartData> GetThroughput(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var data = teamMetricsService.GetThroughputForTeam(team, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("started")]
        public ActionResult<RunChartData> GetStartedItems(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var data = teamMetricsService.GetStartedItemsForTeam(team, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("arrivals")]
        public ActionResult<RunChartData> GetArrivals(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var data = teamMetricsService.GetArrivalsForTeam(team, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("wipOverTime")]
        public ActionResult<RunChartData> GetWorkInProgressOverTime(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var data = teamMetricsService.GetWorkInProgressOverTimeForTeam(team, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return data;
            });
        }

        [HttpGet("featuresInProgress")]
        public ActionResult<IEnumerable<FeatureDto>> GetFeaturesInProgress(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var features = teamMetricsService.GetCurrentFeaturesInProgressForTeam(team);

                return features.Select(f => new FeatureDto(f));
            });
        }

        [HttpGet("currentwip")]
        public ActionResult<IEnumerable<WorkItemDto>> GetCurrentWipForTeam(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var workItems = teamMetricsService.GetCurrentWipForTeam(team);
                return workItems.Select(w => new WorkItemDto(w));
            });
        }

        [HttpGet("forecastInputCandidates")]
        public ActionResult<ForecastInputCandidatesDto> GetForecastInputCandidates(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetForecastInputCandidates(team));
        }

        [HttpGet("cycleTimePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetCycleTimePercentilesForTeam(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTimePercentiles", teamId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) => teamMetricsService.GetCycleTimePercentilesForTeam(team, startDate, endDate));
        }

        [HttpGet("cycleTimeData")]
        public ActionResult<IEnumerable<WorkItemDto>> GetCycleTimeDataForTeam(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTimeData", teamId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var workItems = teamMetricsService.GetClosedItemsForTeam(team, startDate, endDate);
                return workItems.Select(w => new WorkItemDto(w));
            });
        }

        [HttpGet("multiitemforecastpredictabilityscore")]
        public ActionResult<ForecastPredictabilityScore> GetMultiItemForecastPredictabilityScore(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
            {
                return teamMetricsService.GetMultiItemForecastPredictabilityScoreForTeam(team, startDate, endDate);
            });
        }

        [HttpGet("totalWorkItemAge")]
        public ActionResult<int> GetTotalWorkItemAge(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, teamMetricsService.GetTotalWorkItemAge);
        }

        [HttpGet("throughput/pbc")]
        public ActionResult<ProcessBehaviourChart> GetThroughputProcessBehaviourChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                AnnotateBlackoutDays(teamMetricsService.GetThroughputProcessBehaviourChart(team, startDate, endDate)));
        }

        [HttpGet("arrivals/pbc")]
        public ActionResult<ProcessBehaviourChart> GetArrivalsProcessBehaviourChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                AnnotateBlackoutDays(teamMetricsService.GetArrivalsProcessBehaviourChart(team, startDate, endDate)));
        }

        [HttpGet("wipOverTime/pbc")]
        public ActionResult<ProcessBehaviourChart> GetWipProcessBehaviourChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                AnnotateBlackoutDays(teamMetricsService.GetWipProcessBehaviourChart(team, startDate, endDate)));
        }

        [HttpGet("totalWorkItemAge/pbc")]
        public ActionResult<ProcessBehaviourChart> GetTotalWorkItemAgeProcessBehaviourChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                AnnotateBlackoutDays(teamMetricsService.GetTotalWorkItemAgeProcessBehaviourChart(team, startDate, endDate)));
        }

        [HttpGet("cycleTime/pbc")]
        public ActionResult<ProcessBehaviourChart> GetCycleTimeProcessBehaviourChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTime/pbc", teamId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                AnnotateBlackoutDays(teamMetricsService.GetCycleTimeProcessBehaviourChart(team, startDate, endDate)));
        }

        [HttpGet("estimationVsCycleTime")]
        public ActionResult<EstimationVsCycleTimeResponse> GetEstimationVsCycleTimeData(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) => teamMetricsService.GetEstimationVsCycleTimeData(team, startDate, endDate));
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

        private void LogDateBoundaries(string endpoint, int teamId, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Metrics request {Endpoint} for team {TeamId}: startDate={StartDate:yyyy-MM-dd} endDate={EndDate:yyyy-MM-dd} (Kind={StartKind}/{EndKind})",
                endpoint, teamId, startDate, endDate, startDate.Kind, endDate.Kind);
        }
    }
}