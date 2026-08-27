using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/teams/{teamId:int}/metrics")]
    [Route("api/latest/teams/{teamId:int}/metrics")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
    public class TeamMetricsController : ControllerBase
    {
        private const string StartDateMustBeBeforeEndDateErrorMessage = "Start date must be before end date.";
        private const string StateMustNotBeEmptyErrorMessage = "State must not be empty.";
        private const string UnsupportedProcessBehaviorMetricTypeErrorMessage = "Unsupported process behavior metric type.";
        private readonly IRepository<Team> teamRepository;
        private readonly ITeamMetricsService teamMetricsService;
        private readonly IBlackoutPeriodService blackoutPeriodService;
        private readonly IBlockedItemService blockedItemService;
        private readonly IBlockedCountSnapshotRepository blockedCountSnapshotRepository;
        private readonly IWorkItemRepository workItemRepository;
        private readonly IWorkItemBlockedTransitionRepository workItemBlockedTransitionRepository;
        private readonly IPercentilesOverTimeSeriesQuery percentilesOverTimeSeriesQuery;
        private readonly IProcessBehaviorSeriesQuery processBehaviorSeriesQuery;
        private readonly ILighthouseClock clock;
        private readonly ILogger<TeamMetricsController> logger;

#pragma warning disable S107 // The blocked-drill-through endpoint (slice-08) genuinely needs the snapshot repo, work-item repo and blocked-transition repo alongside the existing metrics collaborators; grouping them into an aggregate purely to dodge the 7-param threshold would add indirection without a domain rationale (same rationale as OAuthService).
        public TeamMetricsController(IRepository<Team> teamRepository, ITeamMetricsService teamMetricsService, IBlackoutPeriodService blackoutPeriodService, IBlockedItemService blockedItemService, IBlockedCountSnapshotRepository blockedCountSnapshotRepository, IWorkItemRepository workItemRepository, IWorkItemBlockedTransitionRepository workItemBlockedTransitionRepository, IPercentilesOverTimeSeriesQuery percentilesOverTimeSeriesQuery, IProcessBehaviorSeriesQuery processBehaviorSeriesQuery, ILighthouseClock clock, ILogger<TeamMetricsController> logger)
#pragma warning restore S107
        {
            this.teamRepository = teamRepository;
            this.teamMetricsService = teamMetricsService;
            this.blackoutPeriodService = blackoutPeriodService;
            this.blockedItemService = blockedItemService;
            this.blockedCountSnapshotRepository = blockedCountSnapshotRepository;
            this.workItemRepository = workItemRepository;
            this.workItemBlockedTransitionRepository = workItemBlockedTransitionRepository;
            this.percentilesOverTimeSeriesQuery = percentilesOverTimeSeriesQuery;
            this.processBehaviorSeriesQuery = processBehaviorSeriesQuery;
            this.clock = clock;
            this.logger = logger;
        }

        [HttpGet("throughput")]
        public ActionResult<RunChartDataDto> GetThroughput(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string? view = null)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var data = GetThroughputForView(team, startDate, endDate, view);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return ToRunChartDataDto(data, team);
            });
        }

        private RunChartData GetThroughputForView(Team team, DateTime startDate, DateTime endDate, string? view)
        {
            if (string.Equals(view, "filtered", StringComparison.OrdinalIgnoreCase))
            {
                return teamMetricsService.GetThroughputForTeam(team, startDate, endDate, ThroughputFilterMode.ApplyFilter);
            }

            return teamMetricsService.GetThroughputForTeam(team, startDate, endDate);
        }

        [HttpGet("arrivals")]
        public ActionResult<RunChartDataDto> GetArrivals(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var data = teamMetricsService.GetStartedItemsForTeam(team, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);
                return ToRunChartDataDto(data, team);
            });
        }

        [HttpGet("wipOverTime")]
        public ActionResult<RunChartDataDto> GetWorkInProgressOverTime(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var data = teamMetricsService.GetWorkInProgressOverTimeForTeam(team, startDate, endDate);
                data.BlackoutDayIndices = GetBlackoutDayIndicesArray(startDate, endDate);

                // Every item in this chart is in progress across the range, so its age belongs to the
                // end of the range the caller asked about — the same anchor /wip and the aging chart use.
                return ToRunChartDataDto(data, team, endDate);
            });
        }

        private RunChartDataDto ToRunChartDataDto(RunChartData data, Team team, DateTime? asOf = null)
        {
            // The run chart is typed to the base entity, but a blocked rule set belongs to a team and
            // is evaluated against that team's own work items.
            return RunChartDataDto.From(data, clock, item => item is WorkItem workItem && blockedItemService.IsBlocked(workItem, team), asOf);
        }

        [HttpGet("featuresInProgress")]
        public ActionResult<IEnumerable<FeatureDto>> GetFeaturesInProgress(int teamId, [FromQuery] DateTime asOfDate)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var features = teamMetricsService.GetCurrentFeaturesInProgressForTeam(team, asOfDate).ToList();
                var forecastWindowStart = clock.TodayAsUtcMidnight;
                var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                    forecastWindowStart, FeatureForecastWindow.EndFor(forecastWindowStart, features));

                return features.Select(f => new FeatureDto(f, clock, blackoutPeriods, f.Portfolios.Any(p => blockedItemService.IsBlocked(f, p)), null));
            });
        }

        [HttpGet("wip")]
        public ActionResult<IEnumerable<WorkItemDto>> GetCurrentWipForTeam(int teamId, [FromQuery] DateTime asOfDate)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var workItems = teamMetricsService.GetWipSnapshotForTeam(team, asOfDate).ToList();

                // UPSTREAM-7: for a past range, "is it blocked" cannot be answered by evaluating the
                // blocked rule against today's item — the tags/state it matches on have moved on since.
                // The blocked-transition history is the record of what was true back then, so a historic
                // read prefers it. An item with no history at all predates blocked capture, and there the
                // live rule is still the only answer available — the same one today's read gives, so the
                // fallback cannot regress an item that was already reading correctly.
                var isHistoricRange = DateOnly.FromDateTime(asOfDate) < clock.Today;
                var workItemIds = workItems.Select(w => w.Id).ToList();

                // Indexed once rather than scanned per item: both lookups sit inside the projection
                // below, so a linear Contains/FirstOrDefault makes a board of n items cost O(n²).
                var openSpellByItem = (isHistoricRange
                        ? workItemBlockedTransitionRepository.GetBlockedTransitionsAt(DateOnly.FromDateTime(asOfDate))
                        : [])
                    .GroupBy(transition => transition.WorkItemId)
                    .ToDictionary(group => group.Key, group => group.First());
                var idsWithBlockedHistory = (isHistoricRange
                        ? workItemBlockedTransitionRepository.GetWorkItemIdsWithBlockedHistory(workItemIds)
                        : [])
                    .ToHashSet();

                return workItems.Select(w =>
                {
                    var answerFromHistory = isHistoricRange && idsWithBlockedHistory.Contains(w.Id);
                    var blockedSpell = openSpellByItem.GetValueOrDefault(w.Id);

                    var isBlocked = answerFromHistory ? blockedSpell != null : blockedItemService.IsBlocked(w, team);
                    var liveBlockedSince = isBlocked ? w.CurrentStateEnteredAt : null;
                    var blockedSince = answerFromHistory ? blockedSpell?.EnteredAt : liveBlockedSince;

                    // D16: the endpoint already receives asOfDate and used to discard it, leaving the
                    // aging chart's dot heights today-anchored. Pass it so they honour the range.
                    return new WorkItemDto(w, clock, isBlocked, [], blockedSince, asOfDate);
                });
            });
        }

        [HttpGet("forecastInputCandidates")]
        public ActionResult<ForecastInputCandidatesDto> GetForecastInputCandidates(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetForecastInputCandidates(team));
        }

        [HttpGet("cycleTimePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetCycleTimePercentilesForTeam(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int? definitionId = null)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cycleTimePercentiles", teamId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                IsNamedRequest(definitionId)
                    ? teamMetricsService.GetNamedCycleTimePercentilesForTeam(team, startDate, endDate, definitionId!.Value)
                    : teamMetricsService.GetCycleTimePercentilesForTeam(team, startDate, endDate));
        }

        [HttpGet("workItemAgePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetWorkItemAgePercentilesForTeam(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("workItemAgePercentiles", teamId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetWorkItemAgePercentilesForTeam(team, endDate));
        }

        [HttpGet("ageInStatePercentiles")]
        public ActionResult<IEnumerable<AgeInStatePercentilesDto>> GetAgeInStatePercentilesForTeam(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("ageInStatePercentiles", teamId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) => teamMetricsService.GetAgeInStatePercentilesForTeam(team, startDate, endDate));
        }

        [HttpGet("cumulativeStateTime")]
        public ActionResult<CumulativeStateTimeDto> GetCumulativeStateTime(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int[]? itemIds = null, [FromQuery] int? definitionId = null)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cumulativeStateTime", teamId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetCumulativeStateTimeForTeam(team, startDate, endDate, itemIds, definitionId));
        }

        [HttpGet("cumulativeStateTime/items")]
        public ActionResult<CumulativeStateTimeItemsDto> GetCumulativeStateTimeItems(int teamId, [FromQuery] string? state, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int[]? itemIds = null)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return BadRequest(StateMustNotBeEmptyErrorMessage);
            }

            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cumulativeStateTime/items", teamId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetCumulativeStateTimeItemsForTeam(team, state, startDate, endDate, itemIds));
        }

        [HttpGet("cumulativeStateTime/candidates")]
        public ActionResult<CumulativeStateTimeCandidatesDto> GetCumulativeStateTimeCandidates(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            LogDateBoundaries("cumulativeStateTime/candidates", teamId, startDate, endDate);
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetCumulativeStateTimeCandidatesForTeam(team, startDate, endDate));
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
                teamMetricsService.GetCycleTimeDataForTeam(team, startDate, endDate)
                    .Select(entry => new WorkItemDto(entry.WorkItem, clock, blockedItemService.IsBlocked(entry.WorkItem, team), entry.NamedCycleTimes)));
        }

        private static bool IsNamedRequest(int? definitionId) => definitionId is > 0;

        [HttpGet("multiitemforecastpredictabilityscore")]
        public ActionResult<ForecastPredictabilityScore> GetMultiItemForecastPredictabilityScore(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string? view = null)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
            {
                return GetPredictabilityScoreForView(team, startDate, endDate, view);
            });
        }

        private ForecastPredictabilityScore GetPredictabilityScoreForView(Team team, DateTime startDate, DateTime endDate, string? view)
        {
            if (string.Equals(view, "filtered", StringComparison.OrdinalIgnoreCase))
            {
                return teamMetricsService.GetMultiItemForecastPredictabilityScoreForTeam(team, startDate, endDate, ThroughputFilterMode.ApplyFilter);
            }

            return teamMetricsService.GetMultiItemForecastPredictabilityScoreForTeam(team, startDate, endDate, ThroughputFilterMode.SkipFilter);
        }

        [HttpGet("totalWorkItemAge")]
        public ActionResult<int> GetTotalWorkItemAge(int teamId, [FromQuery] DateTime asOfDate)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) => teamMetricsService.GetTotalWorkItemAge(team, asOfDate));
        }

        [HttpGet("throughputInfo")]
        public ActionResult<ThroughputInfoDto> GetThroughputInfo(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetThroughputInfoForTeam(team, startDate, endDate));
        }

        [HttpGet("arrivalsInfo")]
        public ActionResult<ArrivalsInfoDto> GetArrivalsInfo(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetArrivalsInfoForTeam(team, startDate, endDate));
        }

        [HttpGet("wipOverviewInfo")]
        public ActionResult<WipOverviewInfoDto> GetWipOverviewInfo(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetWipOverviewInfoForTeam(team, startDate, endDate));
        }

        [HttpGet("flowEfficiencyInfo")]
        public ActionResult<FlowEfficiencyInfoDto> GetFlowEfficiencyInfo(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetFlowEfficiencyInfoForTeam(team, startDate, endDate));
        }

        [HttpGet("featuresWorkedOnInfo")]
        public ActionResult<FeaturesWorkedOnInfoDto> GetFeaturesWorkedOnInfo(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetFeaturesWorkedOnInfoForTeam(team, startDate, endDate));
        }

        [HttpGet("totalWorkItemAgeInfo")]
        public ActionResult<TotalWorkItemAgeInfoDto> GetTotalWorkItemAgeInfo(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetTotalWorkItemAgeInfoForTeam(team, startDate, endDate));
        }

        [HttpGet("predictabilityScoreInfo")]
        public ActionResult<PredictabilityScoreInfoDto> GetPredictabilityScoreInfo(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetPredictabilityScoreInfoForTeam(team, startDate, endDate));
        }

        [HttpGet("cycleTimePercentilesInfo")]
        public ActionResult<CycleTimePercentilesInfoDto> GetCycleTimePercentilesInfo(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int? definitionId = null)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                teamMetricsService.GetCycleTimePercentilesInfoForTeam(team, startDate, endDate, definitionId));
        }

        [HttpGet("throughput/pbc")]
        public ActionResult<ProcessBehaviourChart> GetThroughputProcessBehaviourChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string? view = null)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                AnnotateBlackoutDays(GetPbcForView(team, startDate, endDate, view), startDate, endDate));
        }

        private ProcessBehaviourChart GetPbcForView(Team team, DateTime startDate, DateTime endDate, string? view)
        {
            if (string.Equals(view, "filtered", StringComparison.OrdinalIgnoreCase))
            {
                return teamMetricsService.GetThroughputProcessBehaviourChart(team, startDate, endDate, ThroughputFilterMode.ApplyFilter);
            }

            return teamMetricsService.GetThroughputProcessBehaviourChart(team, startDate, endDate);
        }

        [HttpGet("arrivals/pbc")]
        public ActionResult<ProcessBehaviourChart> GetArrivalsProcessBehaviourChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                AnnotateBlackoutDays(teamMetricsService.GetArrivalsProcessBehaviourChart(team, startDate, endDate), startDate, endDate));
        }

        [HttpGet("wipOverTime/pbc")]
        public ActionResult<ProcessBehaviourChart> GetWipProcessBehaviourChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                AnnotateBlackoutDays(teamMetricsService.GetWipProcessBehaviourChart(team, startDate, endDate), startDate, endDate));
        }

        [HttpGet("totalWorkItemAge/pbc")]
        public ActionResult<ProcessBehaviourChart> GetTotalWorkItemAgeProcessBehaviourChart(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                AnnotateBlackoutDays(teamMetricsService.GetTotalWorkItemAgeProcessBehaviourChart(team, startDate, endDate), startDate, endDate));
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
                AnnotateBlackoutDays(teamMetricsService.GetCycleTimeProcessBehaviourChart(team, startDate, endDate), startDate, endDate));
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

        [HttpGet("blockedCountHistory")]
        public ActionResult<IEnumerable<BlockedCountSnapshotDto>> GetBlockedCountHistory(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var start = DateOnly.FromDateTime(startDate.Date);
                var end = DateOnly.FromDateTime(endDate.Date);
                return BlockedCountSeriesBuilder
                    .BuildDailySeries(blockedCountSnapshotRepository, teamId, OwnerType.Team, start, end)
                    .Select(s => new BlockedCountSnapshotDto
                    {
                        RecordedAt = s.RecordedAt.ToString("yyyy-MM-dd"),
                        BlockedCount = s.BlockedCount,
                    });
            });
        }

        /// <summary>
        /// Serves the persisted percentiles trend for one metric family. <paramref name="metricType"/> is
        /// additive and defaults to cycle time, so requests written against the original contract are
        /// unchanged on the wire. Work Item Age has no horizon dimension — a horizon sent alongside it is
        /// ignored in favour of the horizon-less series (see PercentilesOverTimeSeriesQuery).
        /// <paramref name="startDate"/> and <paramref name="endDate"/> are an optional window on the
        /// recorded day, inclusive at both ends; either may be omitted on its own and omitting both
        /// returns the full history, so the shipped request shape keeps its shipped meaning. They are
        /// nullable because a non-nullable [FromQuery] parameter is required by the model binder.
        /// </summary>
        [HttpGet("percentiles-over-time")]
        public ActionResult<IEnumerable<PercentilesOverTimeSnapshotDto>> GetPercentilesOverTime(int teamId, [FromQuery] int? horizon, [FromQuery] MetricType metricType = MetricType.CycleTime, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            if (IsInvertedWindow(startDate, endDate))
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                percentilesOverTimeSeriesQuery
                    .GetSeries(teamId, OwnerType.Team, metricType, horizon, AsRecordedDay(startDate), AsRecordedDay(endDate))
                    .Select(snapshot => new PercentilesOverTimeSnapshotDto(snapshot)));
        }

        /// <summary>
        /// Serves the persisted process-behaviour (natural process limits) trend for one metric family.
        /// Read-only: the widget re-plots the days the recording pipeline judged honest, it never triggers
        /// a recompute. An unknown <paramref name="type"/> is rejected with 400 rather than answered with
        /// an empty 200 — an empty array is indistinguishable from "nothing recorded yet" and would make
        /// the widget lie about why it is empty. An inverted <paramref name="startDate"/>/
        /// <paramref name="endDate"/> window is rejected for the same reason: it would come back empty and
        /// the widget would report it as an honest "nothing in the selected range".
        /// </summary>
        [HttpGet("process-behavior-over-time")]
        public ActionResult<IEnumerable<ProcessBehaviorSnapshotDto>> GetProcessBehaviorOverTime(int teamId, [FromQuery] ProcessBehaviorMetricType type = ProcessBehaviorMetricType.Throughput, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            if (!Enum.IsDefined(type))
            {
                return BadRequest(UnsupportedProcessBehaviorMetricTypeErrorMessage);
            }

            if (IsInvertedWindow(startDate, endDate))
            {
                return BadRequest(StartDateMustBeBeforeEndDateErrorMessage);
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
                processBehaviorSeriesQuery
                    .GetSeries(teamId, OwnerType.Team, type, AsRecordedDay(startDate), AsRecordedDay(endDate))
                    .Select(snapshot => new ProcessBehaviorSnapshotDto(snapshot)));
        }

        /// <summary>
        /// A window is only invertible when BOTH bounds are supplied — a lone bound has nothing to be
        /// inverted against and stays a legal open-ended request.
        /// </summary>
        private static bool IsInvertedWindow(DateTime? startDate, DateTime? endDate)
            => startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date;

        private static DateOnly? AsRecordedDay(DateTime? bound)
            => bound.HasValue ? DateOnly.FromDateTime(bound.Value.Date) : null;

        [HttpGet("blockedItemsAtDate")]
        public ActionResult<IEnumerable<WorkItemDto>> GetBlockedItemsAtDate(int teamId, [FromQuery] DateTime date)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var targetDate = DateOnly.FromDateTime(date.Date);
                var today = clock.Today;

                if (targetDate >= today)
                {
                    return teamMetricsService
                        .GetBlockedEligibleItemsForTeam(team)
                        .Where(w => blockedItemService.IsBlocked(w, team))
                        .Select(w => new WorkItemDto(w, clock, isBlocked: true, [], w.CurrentStateEnteredAt));
                }

                var teamWorkItems = workItemRepository
                    .GetAllByPredicate(w => w.TeamId == teamId)
                    .AsEnumerable()
                    .ToList();
                var blockedIds = workItemBlockedTransitionRepository.GetBlockedWorkItemIdsAt(targetDate);
                var reconstructed = teamWorkItems
                    .Where(w => blockedIds.Contains(w.Id))
                    .Select(w => new WorkItemDto(w, clock, isBlocked: true, [], null))
                    .ToList();

                ReconcileReconstructedCountWithSnapshot(teamId, OwnerType.Team, targetDate, reconstructed.Count);

                return reconstructed;
            });
        }

        // Reconciliation guard (ADR-099): where a captured BlockedCountSnapshot exists for the requested date,
        // compare it against the interval-reconstructed membership count. A divergence signals a
        // transition-capture gap and must never be silent — surface it as a structured warning.
        private void ReconcileReconstructedCountWithSnapshot(int ownerId, OwnerType ownerType, DateOnly targetDate, int reconstructedCount)
        {
            var snapshot = blockedCountSnapshotRepository.GetByPredicate(
                s => s.OwnerId == ownerId && s.OwnerType == ownerType && s.RecordedAt == targetDate);

            if (snapshot is null || snapshot.BlockedCount == reconstructedCount)
            {
                return;
            }

            // Stryker disable once all: diagnostic log text is not behaviour (the guard condition above is what matters)
            logger.LogWarning(
                "Blocked-membership reconstruction for {OwnerType} {OwnerId} at {Date:yyyy-MM-dd} diverged from the captured snapshot (reconstructed {ReconstructedCount}, snapshot {SnapshotCount}); a transition-capture gap is likely.",
                ownerType, ownerId, targetDate, reconstructedCount, snapshot.BlockedCount);
        }

        private int[] GetBlackoutDayIndicesArray(DateTime startDate, DateTime endDate)
        {
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(startDate, endDate);
            return blackoutPeriods.GetBlackoutDayIndices(startDate, endDate).OrderBy(i => i).ToArray();
        }

        private ProcessBehaviourChart AnnotateBlackoutDays(ProcessBehaviourChart chart, DateTime startDate, DateTime endDate)
        {
            var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(startDate, endDate);
            return blackoutPeriods.AnnotateBlackoutDays(chart);
        }

        private void LogDateBoundaries(string endpoint, int teamId, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Metrics request {Endpoint} for team {TeamId}: startDate={StartDate:yyyy-MM-dd} endDate={EndDate:yyyy-MM-dd} (Kind={StartKind}/{EndKind})",
                endpoint, teamId, startDate, endDate, startDate.Kind, endDate.Kind);
        }
    }
}